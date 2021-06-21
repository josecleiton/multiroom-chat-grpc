using System;
using Chat.Grpc;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Chat.Room.Services {
  public class RoomManagerClientService {
    public Grpc.Room Room { get; private set; }
    private readonly ILogger<RoomManagerClientService> _logger;

    public RoomManagerClientService(ILogger<RoomManagerClientService> logger) {
      _logger = logger;
      _logger.LogInformation("Connecting with Manager");


      var channel = GrpcChannel.ForAddress("http://localhost:5001");
      var client = new Grpc.RoomManager.RoomManagerClient(channel);

      Room = client.AcknowledgeRoom(new AcknowledgeRoomRequest {
        Name = Environment.GetEnvironmentVariable("NAME"),
        Address = $"http://localhost:{Environment.GetEnvironmentVariable("PORT")}",
      });


      _logger.LogInformation(JsonConvert.SerializeObject(Room));

      channel.Dispose();
    }
  }
}