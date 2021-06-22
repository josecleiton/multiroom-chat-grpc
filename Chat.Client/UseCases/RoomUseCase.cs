using System;
using Grpc.Net.Client;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using System.Threading;
using Newtonsoft.Json;
using Grpc.Core;
using static Chat.Grpc.Message.Types;

namespace Chat.Client.UseCases {
  public class RoomUseCase : IDisposable {
    private readonly GrpcChannel _roomChannel;
    private readonly CancellationToken _cancelToken;
    private readonly Grpc.Message _message;

    public RoomUseCase(Uri address, User user, Grpc.Room room, CancellationToken token) {
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
      using var streamingCall = CreateClient().ReceiveMessage(new Empty(), cancellationToken: _cancelToken);

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
      }
    }
  }
}
