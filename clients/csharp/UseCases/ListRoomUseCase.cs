using Grpc.Net.Client;
using Google.Protobuf.WellKnownTypes;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace Chat.Client.UseCases {
  public class ListRoomUseCase : IDisposable {
    private static ListRoomUseCase? _instance;

    public static ListRoomUseCase CreateUseCase() {
      return _instance ??= new ListRoomUseCase();
    }

    private readonly GrpcChannel _managerChannel;

    ListRoomUseCase() {
      _managerChannel = GrpcChannel.ForAddress("http://localhost:5001");
    }

    public void Dispose() {
      _managerChannel.Dispose();
    }

    public async Task<IList<Grpc.Room>> Execute() {
      var client = new Grpc.RoomManager.RoomManagerClient(_managerChannel);

      var result = await client.ListRoomAsync(new Empty());

      return result.Rooms;
    }
  }
}
