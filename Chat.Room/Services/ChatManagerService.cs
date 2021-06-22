using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using System.Collections.Generic;
using System.Linq;

namespace Chat.Room.Services {
  public class ChatManagerService : Grpc.ChatManager.ChatManagerBase {
    private readonly ILogger<ChatManagerService> _logger;
    private readonly Channel<Grpc.Message> _messageCh;
    private readonly Channel<Grpc.User> _userCh;
    private readonly IList<Grpc.User> _userList;
    private readonly Grpc.Room _room;

    public ChatManagerService(ILogger<ChatManagerService> logger,
    Channel<Grpc.Message> messageCh,
    Channel<Grpc.User> userCh,
    IList<Grpc.User> userList, RoomManagerClientService managerClient) {
      _logger = logger;
      _messageCh = messageCh;
      _userCh = userCh;
      _userList = userList;
      _room = managerClient.Room;
    }

    public override async Task ReceiveMessage(Grpc.User request, IServerStreamWriter<Grpc.Message> responseStream, ServerCallContext context) {
      await JoinUser(request);

      while (!context.CancellationToken.IsCancellationRequested) {
        var message = await _messageCh.Reader.ReadAsync();
        await responseStream.WriteAsync(message);
      }

      // se o cliente fechar a conexão abruptamente
      if (_userList.Any((user) => user.Id == request.Id)) {
        await ExitRoom(request, context);
      }
    }

    private async Task JoinUser(Grpc.User user) {
      _userList.Add(user);

      var tasks = new List<Task>{
        Task.Run(async () => await _userCh.Writer.WriteAsync(user)),
        Task.Run(async () => await _messageCh.Writer.WriteAsync(new Grpc.Message {
          Room = _room,
          User = RoomUser(),
          Message_ = $"User {user.Name} joined."
        })),
      };

      await Task.WhenAll(tasks);
    }

    private Grpc.User RoomUser() => new Grpc.User {
      Id = _room.Id,
      Name = "Room",
    };

    public override async Task<Empty> SendMessage(Grpc.Message request, ServerCallContext context) {
      await _messageCh.Writer.WriteAsync(request);

      return new Empty();
    }

    public override async Task ListUsers(Empty request, IServerStreamWriter<Grpc.ListUser> responseStream, ServerCallContext context) {
      do {
        var result = new Grpc.ListUser();
        result.Users.AddRange(_userList);

        await responseStream.WriteAsync(result);
        await _userCh.Reader.ReadAsync();
      } while (!context.CancellationToken.IsCancellationRequested);
    }

    public override async Task<Empty> ExitRoom(Grpc.User request, ServerCallContext context) {
      for (int i = 0; i < _userList.Count; i++) {
        if (_userList[i].Id == request.Id) {
          _userList.RemoveAt(i);
          break;
        }
      }

      var tasks = new List<Task>{
        Task.Run(() => _userCh.Writer.WriteAsync(request)),
        Task.Run(() => _messageCh.Writer.WriteAsync(new Grpc.Message {
          Message_ = $"User {request.Name} exited",
          Room = _room,
          User = RoomUser(),
        })),
      };

      await Task.WhenAll(tasks);

      return new Empty();
    }
  }
}
