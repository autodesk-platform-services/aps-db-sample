using aps_viewer_db_properties.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;

[ApiController]
[Route("api/[controller]")]
public class DBController : ControllerBase
{
  public readonly IHubContext<DBHub> _dbHub;
  private readonly ILogger<DBController> _logger;
  private readonly DBService _dbService;

  public DBController(ILogger<DBController> logger, DBService dbService, IHubContext<DBHub> dbHub)
  {
    _dbHub = dbHub;
    GC.KeepAlive(_dbHub);
    _logger = logger;
    _dbService = dbService;
  }

  [HttpPost("dbconnector")]
  public object PostDBData([FromBody] DBUpdate dBUpdate)
  {

    switch (dBUpdate.dbProvider.ToLower())
    {
      case "mongo":
        _dbService.UpdateDataFromMongoDB(dBUpdate.connectionId, dBUpdate.property, dBUpdate.selecteddbId, dBUpdate.itemId, _dbHub);
        break;
      default:
        break;
    }
    return new { Success = true };
  }

  [HttpGet("dbconnector")]
  public object GetDBData()
  {
    string connectionId = base.Request.Query["connectionId"];
    //string externalId = base.Request.Query["externalId"];
    string selecteddbId = base.Request.Query["selecteddbId"];
    string dbProvider = base.Request.Query["dbProvider"];
    //string projectId = base.Request.Query["projectId"];
    string itemId = base.Request.Query["itemId"];


    switch (dbProvider.ToLower())
    {
      case "mongo":
        _dbService.ExtractDataFromMongoDB(connectionId, selecteddbId, itemId, _dbHub);
        break;
      default:
        break;
    }

    return new { Success = true };
  }

}

public class DBUpdate
{
  public Property property { get; set; }
  public string connectionId { get; set; }
  public string dbProvider { get; set; }
  public string selecteddbId { get; set; }
  public string itemId { get; set; }
}

public class Property
{
  public string category { get; set; }
  public string name { get; set; }
  public string value { get; set; }
}
