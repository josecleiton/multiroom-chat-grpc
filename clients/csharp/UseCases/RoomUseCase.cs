using System;
using Grpc.Net.Client;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using Grpc.Core;

namespace Chat.Client.UseCases {
  public class RoomUseCase : IDisposable {
    private readonly GrpcChannel _roomChannel;
    private readonly CancellationToken _cancelToken;
    private readonly Grpc.Message _message;

    public RoomUseCase(Uri address, Grpc.User user, Grpc.Room room, CancellationToken token) {
      _roomChannel = GrpcChannel.ForAddress(address);
      _cancelToken = token;
      _message = new Grpc.Message {
        Room = room,
        User = user,
      };
    }

    public void Dispose() {
      _roomChannel.Dispose();
    }

    Grpc.ChatManager.ChatManagerClient CreateClient() {
      return new Grpc.ChatManager.ChatManagerClient(_roomChannel);
    }

    public async Task SendMessage(string message) {

      await CreateClient().SendMessageAsync(new Grpc.Message(_message) {
        Message_ = message,
      });
    }

    public async Task ReceiveMessages() {
      using var streamingCall = CreateClient().ReceiveMessage(_message.User, cancellationToken: _cancelToken);

      try {
        while (await streamingCall.ResponseStream.MoveNext(cancellationToken: _cancelToken)) {
          var messageReceived = streamingCall.ResponseStream.Current;
          Console.WriteLine(JsonConvert.SerializeObject(messageReceived));
          if (_message.User.Id == messageReceived.User.Id) {
            Console.WriteLine("Filter same user message");
          }
        }
        Console.ReadLine();
      } catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled) {
        Console.WriteLine("Stream cancelled");
      } catch (OperationCanceledException) {

      }
    }

    public async Task ListUsers() {
      using var streamingCall = CreateClient().ListUsers(_message.User, cancellationToken: _cancelToken);

      try {
        while (await streamingCall.ResponseStream.MoveNext(cancellationToken: _cancelToken)) {
          var usersReceived = streamingCall.ResponseStream.Current;
          Console.WriteLine($"Users connected: {usersReceived.Users.Count}");
        }
      } catch (RpcException) {
        Console.WriteLine("Stream cancelled");
      }
    }

    public async Task ExitRoom() {
      await CreateClient().ExitRoomAsync(_message.User);
    }
  }
}
