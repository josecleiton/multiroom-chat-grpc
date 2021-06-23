using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;

namespace Chat.Room.Services {
  public class RoomManagerClientService : IDisposable {
    public Grpc.Room Room { get; private set; }
    private readonly GrpcChannel _channel;
    private readonly ILogger<RoomManagerClientService> _logger;
    private readonly CancellationToken _cancellationToken;

    public RoomManagerClientService(ILogger<RoomManagerClientService> logger) {
      _logger = logger;
      _cancellationToken = new CancellationTokenRegistration().Token;
      var managerAddress = Environment.GetEnvironmentVariable("HOST_MANAGER") ?? "http://localhost:5001";
      _channel = GrpcChannel.ForAddress(managerAddress);

      var client = new Grpc.RoomManager.RoomManagerClient(_channel);
      Room = client.AcknowledgeRoom(new Grpc.AcknowledgeRoomRequest {
        Name = Environment.GetEnvironmentVariable("NAME"),
        Address = $"http://localhost:{Environment.GetEnvironmentVariable("PORT")}",
      });
      _logger.LogInformation("Room successfully registered");

      Task.Run(Heartbeat);
    }

    private async Task Heartbeat() {
      var client = new Grpc.RoomManager.RoomManagerClient(_channel);
      var streamingCall = client.RoomHeartbeat(new Grpc.RoomHeartbeatRequest {
        Id = Room.Id,
      }, cancellationToken: _cancellationToken);

      while (await streamingCall.ResponseStream.MoveNext(_cancellationToken)) {
      }
    }

    public void Dispose() {
      var client = new Grpc.RoomManager.RoomManagerClient(_channel);
      Task.Run(async () => await client.CloseRoomAsync(Room));

      _channel.Dispose();
    }
  }
}
