using System;
using System.Threading;
using System.Threading.Tasks;
using Chat.Client.UseCases;
using static Chat.Grpc.Message.Types;

namespace Chat.Client {
  class Program {
    static async Task Main(string[] args) {
      var cancelRegistration = new CancellationTokenRegistration();

      var listRoomUseCase = ListRoomUseCase.Instance();

      var rooms = await listRoomUseCase.Execute();

      var room = rooms[0];

      var user = new User { Id = Guid.NewGuid().ToString(), Name = "Opa" };

      var roomUseCase = new RoomUseCase(
        new Uri(room.Address),
        user,
        room,
        cancelRegistration.Token
      );

      await roomUseCase.SendMessage("oi");

      await roomUseCase.ReceiveMessages();
    }
  }
}
