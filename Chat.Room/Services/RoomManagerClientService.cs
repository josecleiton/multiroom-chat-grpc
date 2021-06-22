using System;
using System.Threading.Tasks;
using Grpc.Net.Client;

namespace Chat.Room.Services {
  public class RoomManagerClientService : IDisposable {
    public Grpc.Room Room { get; private set; }
    private readonly GrpcChannel _channel;

    public RoomManagerClientService() {


      _channel = GrpcChannel.ForAddress("http://localhost:5001");

      var client = new Grpc.RoomManager.RoomManagerClient(_channel);
      Room = client.AcknowledgeRoom(new Grpc.AcknowledgeRoomRequest {
        Name = Environment.GetEnvironmentVariable("NAME"),
        Address = $"http://localhost:{Environment.GetEnvironmentVariable("PORT")}",
      });

    }

    public void Dispose() {
      var client = new Grpc.RoomManager.RoomManagerClient(_channel);
      Task.Run(() => client.CloseRoomAsync(Room));

      _channel.Dispose();
    }
  }
}
