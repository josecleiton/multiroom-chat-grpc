using Grpc.Net.Client;
using Chat.Grpc;
using Google.Protobuf.WellKnownTypes;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace Chat.Client.UseCases {
  public class ListRoomUseCase : IDisposable {
    private readonly GrpcChannel _managerChannel;
    static private ListRoomUseCase? _instance;
    ListRoomUseCase() {
      _managerChannel = GrpcChannel.ForAddress("http://localhost:5001");
    }

    public static ListRoomUseCase Instance() {
      if (_instance is null) {
        _instance = new ListRoomUseCase();
      }

      return _instance;
    }

    public void Dispose() {
      _managerChannel.Dispose();
    }

    public async Task<IList<Room>> Execute() {
      var client = new Grpc.RoomManager.RoomManagerClient(_managerChannel);

      var result = await client.ListRoomAsync(new Empty());

      return result.Rooms;
    }
  }
}