using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;

namespace Chat.RoomManager {
  public class Program {
    public static void Main(string[] args) {
      CreateHostBuilder(args).Build().Run();
    }

    // Additional configuration is required to successfully run gRPC on macOS.
    // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder => {
              webBuilder.ConfigureKestrel(options => {
                options.ListenAnyIP(5001, o => o.Protocols = HttpProtocols.Http2);
                // options.ListenAnyIP(Config.Port ?? 5008, o => o.Protocols = HttpProtocols.Http1AndHttp2);
              });

              webBuilder.UseStartup<Startup>();
            });
  }
}
