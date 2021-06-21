using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Chat.Grpc;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace Chat.Room.Services {
  public class ChatManagerService : Grpc.ChatManager.ChatManagerBase {
    private readonly ILogger<ChatManagerService> _logger;
    private readonly Channel<Message> _ch;

    public ChatManagerService(ILogger<ChatManagerService> logger) {
      _logger = logger;
      _ch = Channel.CreateUnbounded<Message>();
    }


    public override async Task ReceiveMessage(Empty request, IServerStreamWriter<Message> responseStream, ServerCallContext context) {
      while (!context.CancellationToken.IsCancellationRequested) {
        var message = await _ch.Reader.ReadAsync();
        await responseStream.WriteAsync(message);
      }
    }

    public override async Task<Empty> SendMessage(Message request, ServerCallContext context) {
      await _ch.Writer.WriteAsync(request);

      return new Empty();
    }
  }
}