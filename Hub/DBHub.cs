using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;

namespace aps_viewer_db_properties.Hubs
{
  public class DBHub : Microsoft.AspNetCore.SignalR.Hub
  {
    public async static Task SendData(IHubContext<DBHub> hub, string connectionId, string selecteddbId, Dictionary<string, dynamic> properties)
    {
      await hub.Clients.Client(connectionId).SendAsync("ReceiveProperties", selecteddbId, properties);
    }

    public async static Task SendUpdate(IHubContext<DBHub> hub, string connectionId, string selecteddbId, bool updateResult, string message, Dictionary<string, dynamic> properties, string urn)
    {
      await hub.Clients.Client(connectionId).SendAsync("ReceiveUpdate", selecteddbId, updateResult, message);
      if (updateResult)
      {
        await hub.Clients.AllExcept(connectionId).SendAsync("ReceiveModification", selecteddbId, properties, urn);
      }
    }
  }
}