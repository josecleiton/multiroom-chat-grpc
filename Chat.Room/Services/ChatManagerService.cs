using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Channels;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Chat.Room.Services {
  public class ChatManagerService : Grpc.ChatManager.ChatManagerBase {
    private readonly ILogger<ChatManagerService> _logger;
    private readonly Channel<Grpc.User> _userCh;
    private readonly IList<(Grpc.User, Channel<Grpc.Message>)> _userList;
    private readonly Grpc.Room _room;

    public ChatManagerService(ILogger<ChatManagerService> logger,
    Channel<Grpc.User> userCh,
    IList<(Grpc.User, Channel<Grpc.Message>)> userList,
    RoomManagerClientService managerClient) {
      _logger = logger;
      _userCh = userCh;
      _userList = userList;
      _room = managerClient.Room;
    }

    public override async Task ReceiveMessage(Grpc.User request, IServerStreamWriter<Grpc.Message> responseStream, ServerCallContext context) {
      var (_, ch) = await JoinUser(request, context.CancellationToken);

      try {
        while (!context.CancellationToken.IsCancellationRequested) {
          var message = await ch.Reader.ReadAsync(context.CancellationToken);
          await responseStream.WriteAsync(message);
        }

      } catch (OperationCanceledException) {
        // se o cliente fechar a conexão abruptamente
        if (_userList.Any((tuple) => tuple.Item1.Id == request.Id)) {
          await ExitRoom(request, context);
        }
      }
    }

    private async Task<(Grpc.User, Channel<Grpc.Message>)> JoinUser(Grpc.User user, CancellationToken cancellationToken) {
      var userTuple = (user, Channel.CreateUnbounded<Grpc.Message>());
      _userList.Add(userTuple);

      await Task.WhenAll(
        Task.Run(async () => await _userCh.Writer.WriteAsync(user)),
        WriteMessageOnUserChannel(new Grpc.Message {
          Room = _room,
          User = RoomUser(),
          Message_ = $"User {user.Name} joined."
        }, cancellationToken)
      );

      return userTuple;
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

      foreach (var (user, ch) in _userList) {
        tasks.Add(Task.Run(async () => await ch.Writer.WriteAsync(message)));
      }

      await Task.WhenAll(tasks);
    }

    public override async Task ListUsers(Empty request, IServerStreamWriter<Grpc.ListUser> responseStream, ServerCallContext context) {
      do {
        var result = new Grpc.ListUser();
        result.Users.AddRange(_userList.Select(item => item.Item1));

        await responseStream.WriteAsync(result);

        try {
          await _userCh.Reader.ReadAsync(context.CancellationToken);
        } catch (OperationCanceledException) {
        }

      } while (!context.CancellationToken.IsCancellationRequested);
    }

    public override async Task<Empty> ExitRoom(Grpc.User request, ServerCallContext context) {
      for (int i = 0; i < _userList.Count; i++) {
        var (user, ch) = _userList[i];

        if (user.Id == request.Id) {
          _userList.RemoveAt(i);
          ch.Writer.Complete();
          break;
        }
      }

      await Task.WhenAll(
        Task.Run(() => _userCh.Writer.WriteAsync(request)),
        WriteMessageOnUserChannel(new Grpc.Message {
          Message_ = $"User {request.Name} exited",
          Room = _room,
          User = RoomUser(),
        }, context.CancellationToken)
      );

      return new Empty();
    }
  }
}
