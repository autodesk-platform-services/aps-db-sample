using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;

namespace forge_viewer_db_properties.Hubs
{
	public class DBHub : Microsoft.AspNetCore.SignalR.Hub
	{
		public async static Task SendData(IHubContext<DBHub> hub,string connectionId, string externalId, Dictionary<string, dynamic> properties)
		{
			await hub.Clients.Client(connectionId).SendAsync("ReceiveProperties", externalId, properties);
		}

		public async static Task SendUpdate(IHubContext<DBHub> hub, string connectionId, string externalId, UpdateResult updateResult)
		{
			await hub.Clients.Client(connectionId).SendAsync("ReceiveUpdate", externalId);
		}
	}
}
