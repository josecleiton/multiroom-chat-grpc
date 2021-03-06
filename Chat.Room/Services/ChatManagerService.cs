using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Collections.Generic;
using System;
using System.Collections.Concurrent;
using Chat.Room.Entities;
using System.Linq;
using System.Threading.Channels;
using Newtonsoft.Json;

namespace Chat.Room.Services {
  public class ChatManagerService : Grpc.ChatManager.ChatManagerBase {
    private readonly ILogger<ChatManagerService> _logger;
    private readonly ConcurrentDictionary<string, RoomUser> _userDict;
    private readonly Grpc.Room _room;

    public ChatManagerService(ILogger<ChatManagerService> logger,
    ConcurrentDictionary<string, RoomUser> userDict,
    RoomManagerClientService managerClient) {
      _logger = logger;
      _userDict = userDict;
      _room = managerClient.Room;
    }
    public override async Task<Empty> JoinRoom(Grpc.User request, ServerCallContext context) {
      var roomUser = await JoinUser(request, context.CancellationToken);

      _logger.LogInformation($"JoinRoom - {JsonConvert.SerializeObject(request)}");

      return new Empty();
    }

    // public override Task<Empty> UpdateName(Grpc.UpdateNameRequest request, ServerCallContext context) {
    //   try {
    //     var roomUser = _userDict[request.UserId];
    //     roomUser.User.Name = request.Name;

    //     return Task.FromResult(new Empty());
    //   } catch (KeyNotFoundException) {
    //     throw new RpcException(new Status(StatusCode.NotFound, "user not found"));
    //   }
    // }

    public override async Task ReceiveMessage(Grpc.User request, IServerStreamWriter<Grpc.Message> responseStream, ServerCallContext context) {
      var roomUser = GetUser(request);

      try {
        while (!context.CancellationToken.IsCancellationRequested) {
          var message = await roomUser.MessageCh.Reader.ReadAsync(context.CancellationToken);
          await responseStream.WriteAsync(message);
        }

      } catch (Exception ex) when (
          ex is OperationCanceledException ||
          ex is ChannelClosedException ||
          ex is TaskCanceledException
        ) {
        // se o cliente fechar a conex??o abruptamente
        if (_userDict.ContainsKey(roomUser.User.Id)) {
          await PopUser(roomUser.User.Id);
        }
      }
    }

    private async Task<RoomUser> JoinUser(Grpc.User user, CancellationToken cancellationToken) {
      var roomUser = new RoomUser(user);

      if (!_userDict.TryAdd(user.Id, roomUser)) {
        throw new RpcException(new Status(StatusCode.Internal, "failed to add user to list"));
      }

      await Task.WhenAll(
        UpdateUserList(cancellationToken),
        WriteMessageOnUserChannel(new Grpc.Message {
          Room = _room,
          User = RoomUser(),
          Message_ = $"User {user.Name} joined."
        }, cancellationToken)
      );

      return roomUser;
    }

    private Grpc.User RoomUser() => new Grpc.User {
      Id = _room.Id,
      Name = "Room",
    };

    public override async Task<Empty> SendMessage(Grpc.Message request, ServerCallContext context) {
      await WriteMessageOnUserChannel(request, context.CancellationToken);

      return new Empty();
    }

    private async Task WriteMessageOnUserChannel(Grpc.Message message, CancellationToken cancelToken = default) {
      var tasks = new List<Task>();

      foreach (var (_, user) in _userDict) {
        tasks.Add(Task.Run(async () =>
          await user.MessageCh.Writer.WriteAsync(message, cancelToken),
          cancelToken
        ));
      }

      await Task.WhenAll(tasks);
    }

    private async Task UpdateUserList(CancellationToken cancelToken) {
      var tasks = new List<Task>();

      foreach (var (_, user) in _userDict) {
        tasks.Add(Task.Run(async () =>
          await user.UserChangedCh.Writer.WriteAsync(true, cancelToken),
          cancelToken
        ));
      }

      await Task.WhenAll(tasks);
    }

    private RoomUser GetUser(Grpc.User request) {
      if (!_userDict.TryGetValue(request.Id, out RoomUser? user)) {
        throw new RpcException(new Status(StatusCode.NotFound, "user not found"));
      }

      return user;
    }

    public override async Task ListUsers(Grpc.User request, IServerStreamWriter<Grpc.ListUser> responseStream, ServerCallContext context) {
      var user = GetUser(request);

      try {
        while (!context.CancellationToken.IsCancellationRequested) {
          var result = new Grpc.ListUser();
          result.Users.AddRange(_userDict.Values.Select((roomUser) => roomUser.User));

          await responseStream.WriteAsync(result);
          await user.UserChangedCh.Reader.ReadAsync(context.CancellationToken);
        }
      } catch (Exception ex) when (
          ex is OperationCanceledException ||
          ex is ChannelClosedException ||
          ex is TaskCanceledException
        ) {
        throw new RpcException(new Status(StatusCode.Cancelled, "cancelled"));
      }
    }

    private async Task PopUser(String userId, CancellationToken? cancellationToken = null) {
      if (!_userDict.TryRemove(userId, out RoomUser? roomUser)) {
        return;
      }

      var source = new CancellationTokenSource();
      CancellationToken token = cancellationToken ?? source.Token;

      await Task.WhenAll(
        UpdateUserList(token),
        WriteMessageOnUserChannel(new Grpc.Message {
          Message_ = $"User {roomUser.User.Name} exited",
          Room = _room,
          User = RoomUser(),
        }, token)
      );
    }

    public override async Task<Empty> ExitRoom(Grpc.User request, ServerCallContext context) {
      await PopUser(request.Id, context.CancellationToken);

      return new Empty();
    }
  }
}
