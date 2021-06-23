using System.Threading.Channels;

namespace Chat.Room.Entities {
  public class RoomUser {
    public Grpc.User User { get; private set; }
    public Channel<Grpc.Message> MessageCh { get; private set; }
    public Channel<bool> UserChangedCh { get; private set; }

    public RoomUser(Grpc.User user) {
      User = user;
      MessageCh = Channel.CreateUnbounded<Grpc.Message>(new UnboundedChannelOptions {
        SingleReader = true
      });
      UserChangedCh = Channel.CreateUnbounded<bool>(new UnboundedChannelOptions {
        SingleReader = true
      });
    }
  }
}
