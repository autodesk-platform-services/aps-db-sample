using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using forge_viewer_db_properties.Hubs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace forge_viewer_db_properties
{
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
}
