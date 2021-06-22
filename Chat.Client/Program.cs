using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Chat.Client.UseCases;

namespace Chat.Client {
  class Program {
    static async Task Main(string[] args) {
      var cancelRegistration = new CancellationTokenRegistration();

      var listRoomUseCase = ListRoomUseCase.Instance();

      var rooms = await listRoomUseCase.Execute();

      var room = rooms[0];

      var user = new Grpc.User { Id = Guid.NewGuid().ToString(), Name = "Opa" };

      var roomUseCase = new RoomUseCase(
        new Uri(room.Address),
        user,
        room,
        cancelRegistration.Token
      );

      var tasks = new List<Task> {
        roomUseCase.ReceiveMessages(),
        roomUseCase.ListUsers(),
        Task.Run(async () => {
          await Task.Delay(2500);
          await roomUseCase.SendMessage("oi");
          await Task.Delay(10000);
          await roomUseCase.ExitRoom();
        })
      };

      await Task.WhenAll(tasks);
    }
  }
}
