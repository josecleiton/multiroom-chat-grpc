using System;
using System.Threading;
using System.Threading.Tasks;
using Chat.Client.UseCases;

namespace Chat.Client {
  class Program {
    static async Task Main(string[] args) {
      var tkSource = new CancellationTokenSource();

      // ListRoom Singleton
      var listRoomUseCase = ListRoomUseCase.Instance();

      var rooms = await listRoomUseCase.Execute();

      // Arbitrariamente seleciona a primeira sala
      var room = rooms[0];

      // Cria as informações de usuário
      var user = new Grpc.User { Id = Guid.NewGuid().ToString(), Name = "Opa" };

      // Prepara o RoomUseCase
      var roomUseCase = new RoomUseCase(
        new Uri(room.Address),
        user,
        room,
        tkSource.Token
      );

      // Lista de tarefas assincronas:
      // 1. Escuta novas mensagens
      // 2. Escuta quais usuários estão online na sala
      // 3. Manda uma mensage e sai da sala
      await Task.WhenAll(
        roomUseCase.ReceiveMessages(),
        roomUseCase.ListUsers(),
        Task.Run(async () => {
          await Task.Delay(2500, tkSource.Token);
          await roomUseCase.SendMessage("oi");
          await Task.Delay(10000, tkSource.Token);
          await roomUseCase.ExitRoom();
          tkSource.Cancel();
        }, tkSource.Token)
      );
    }
  }
}
