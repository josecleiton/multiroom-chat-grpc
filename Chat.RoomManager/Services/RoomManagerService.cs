using System;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Chat.Grpc;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Linq;

namespace Chat.RoomManager.Services {
  public class RoomManagerService : Grpc.RoomManager.RoomManagerBase {
    private readonly ILogger<RoomManagerService> _logger;
    private readonly ConcurrentDictionary<string, Room> _roomDict;

    public RoomManagerService(ILogger<RoomManagerService> logger, ConcurrentDictionary<string, Room> roomDict) {
      _logger = logger;
      _roomDict = roomDict;
    }

    public override Task<Room> AcknowledgeRoom(AcknowledgeRoomRequest request, ServerCallContext context) {
      var room = new Room {
        Id = Guid.NewGuid().ToString(),
        Name = request.Name,
        Address = request.Address,
      };

      if (!_roomDict.TryAdd(room.Id, room)) {
        throw new RpcException(new Status(StatusCode.Internal, "add room to dict operation failed"));
      }

      Console.WriteLine(JsonConvert.SerializeObject(room));

      return Task.FromResult(room);
    }

    public override async Task RoomHeartbeat(RoomHeartbeatRequest request, IServerStreamWriter<RoomHeartbeatResponse> responseStream, ServerCallContext context) {
      try {
        while (!context.CancellationToken.IsCancellationRequested) {

          if (!_roomDict.TryGetValue(request.Id, out Room? receivedValue)) {
            throw new RpcException(new Status(StatusCode.NotFound, "room was closed"));
          }

          await responseStream.WriteAsync(new RoomHeartbeatResponse {
            Id = receivedValue.Id,
            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
          });
          await Task.Delay(2000, context.CancellationToken);
        }
      } catch (Exception) {
        _logger.LogWarning($"Room {request.Id} closed");
      } finally {
        _roomDict.TryRemove(request.Id, out Room? popReturn);
      }
    }

    public override Task<ListRoomResponse> ListRoom(Empty request, ServerCallContext context) {
      var response = new ListRoomResponse();

      response.Rooms.AddRange(_roomDict.Values.OrderBy(
        room => room.Name, StringComparer.OrdinalIgnoreCase
      ));

      return Task.FromResult(response);
    }

    public override Task<Empty> CloseRoom(Room request, ServerCallContext context) {
      if (_roomDict.TryRemove(request.Id, out Room? receivedValue)) {
        _logger.LogInformation($"Room {receivedValue.Id} - {receivedValue.Name} closed");
      }

      return Task.FromResult(new Empty());
    }
  }
}
