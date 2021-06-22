using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Chat.Grpc;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using Grpc.Net.Client;
using System;

namespace Chat.Room.Services {
  public class ChatManagerService : Grpc.ChatManager.ChatManagerBase {
    private readonly ILogger<ChatManagerService> _logger;
    private readonly Channel<Message> _ch;
    private readonly Grpc.Room _room;

    public ChatManagerService(ILogger<ChatManagerService> logger, RoomManagerClientService roomManager, Channel<Message> ch) {
      _logger = logger;
      _ch = ch;
      _room = roomManager.Room;
    }

    // public override async Task ReceiveMessage(Empty request, IServerStreamWriter<Message> responseStream, ServerCallContext context) {
    // }

    public override async Task ReceiveMessage(Empty request, IServerStreamWriter<Message> responseStream, ServerCallContext context) {
      while (!context.CancellationToken.IsCancellationRequested) {
        var message = await _ch.Reader.ReadAsync();
        Console.WriteLine($"MESSAGE: {message.Message_}");
        await responseStream.WriteAsync(message);
      }
    }

    public override async Task<Empty> SendMessage(Message request, ServerCallContext context) {
      await _ch.Writer.WriteAsync(request);

      return new Empty();
    }
  }
}