using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Chat.Grpc;
using Newtonsoft.Json;

namespace Chat.RoomManager.Services {
  public class RoomManagerService : Grpc.RoomManager.RoomManagerBase {
    private readonly ILogger<RoomManagerService> _logger;
    private readonly IList<Room> _rooms;

    public RoomManagerService(ILogger<RoomManagerService> logger, IList<Room> rooms) {
      _logger = logger;
      _rooms = rooms;
    }

    public override Task<Room> AcknowledgeRoom(AcknowledgeRoomRequest request, ServerCallContext context) {
      _rooms.Add(new Room {
        Id = Guid.NewGuid().ToString(),
        Name = request.Name,
        Address = request.Address,
      });

      Console.WriteLine(JsonConvert.SerializeObject(_rooms.Last()));

      return Task.FromResult(_rooms.Last());
    }

    public override Task<ListRoomResponse> ListRoom(Empty request, ServerCallContext context) {
      var response = new ListRoomResponse();

      response.Rooms.Add(_rooms);

      Console.WriteLine(JsonConvert.SerializeObject(_rooms));

      return Task.FromResult(response);
    }

    public override Task<Empty> CloseRoom(Room request, ServerCallContext context) {
      for (int i = 0; i < _rooms.Count; i++) {
        var room = _rooms[i];

        if (room.Id == request.Id) {
          _rooms.RemoveAt(i);
          _logger.LogInformation($"Room {room.Id} - {room.Name} closed");
          break;
        }
      }

      return Task.FromResult(new Empty());
    }
  }
}
