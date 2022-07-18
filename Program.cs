using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using forge_viewer_db_properties.Hubs;
using Microsoft.AspNetCore.SignalR;

public class Program
{
  public static void Main(string[] args)
  {
		var host = CreateHostBuilder(args).Build();
		var hubContext = host.Services.GetService(typeof(IHubContext<DBHub>));
		host.Run();
	}

  public static IHostBuilder CreateHostBuilder(string[] args) =>
      Host.CreateDefaultBuilder(args)
          .ConfigureWebHostDefaults(webBuilder =>
          {
            webBuilder.UseStartup<Startup>();
          });
}