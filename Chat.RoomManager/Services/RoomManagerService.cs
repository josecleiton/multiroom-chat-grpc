using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Chat.Grpc;

namespace Chat.RoomManager.Services {
  public class RoomManagerService : Grpc.RoomManager.RoomManagerBase {
    private readonly ILogger<RoomManagerService> _logger;
    private readonly IList<Room> _rooms;

    public RoomManagerService(ILogger<RoomManagerService> logger) {
      _logger = logger;
      _rooms = new List<Room>();
    }

    public override Task<Room> AcknowledgeRoom(AcknowledgeRoomRequest request, ServerCallContext context) {
      _rooms.Add(new Room {
        Id = Guid.NewGuid().ToString(),
        Name = request.Name,
        Address = request.Address,
      });

      return Task.FromResult(_rooms.Last());
    }

    public override Task<ListRoomResponse> ListRoom(Empty request, ServerCallContext context) {
      var response = new ListRoomResponse();

      response.Rooms.AddRange(_rooms);

      return Task.FromResult(response);
    }

    public override Task<Empty> CloseRoom(Room request, ServerCallContext context) {
      for (int i = 0; i < _rooms.Count; i++) {
        var room = _rooms[i];

        if (room.Id == request.Id) {
          _rooms.RemoveAt(i);
          break;
        }
      }

      return Task.FromResult(new Empty());
    }
  }
}