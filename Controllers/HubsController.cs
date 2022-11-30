using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

[ApiController]
[Route("api/[controller]")]
public class HubsController : ControllerBase
{
  private readonly ILogger<HubsController> _logger;
  private readonly APSService _APSService;

  public HubsController(ILogger<HubsController> logger, APSService APSService)
  {
    _logger = logger;
    _APSService = APSService;
  }

  [HttpGet()]
  public async Task<ActionResult<string>> ListHubs()
  {
    var tokens = await AuthController.PrepareTokens(Request, Response, _APSService);
    if (tokens == null)
    {
      return Unauthorized();
    }
    var hubs = await _APSService.GetHubs(tokens);
    return JsonConvert.SerializeObject(hubs);
  }

  [HttpGet("{hub}/projects")]
  public async Task<ActionResult<string>> ListProjects(string hub)
  {
    var tokens = await AuthController.PrepareTokens(Request, Response, _APSService);
    if (tokens == null)
    {
      return Unauthorized();
    }
    var projects = await _APSService.GetProjects(hub, tokens);
    return JsonConvert.SerializeObject(projects);
  }

  [HttpGet("{hub}/projects/{project}/contents")]
  public async Task<ActionResult<string>> ListItems(string hub, string project, [FromQuery] string? folder_id)
  {
    var tokens = await AuthController.PrepareTokens(Request, Response, _APSService);
    if (tokens == null)
    {
      return Unauthorized();
    }
    var contents = await _APSService.GetContents(hub, project, folder_id, tokens);
    return JsonConvert.SerializeObject(contents);
  }

  [HttpGet("{hub}/projects/{project}/contents/{item}/versions")]
  public async Task<ActionResult<string>> ListVersions(string hub, string project, string item)
  {
    var tokens = await AuthController.PrepareTokens(Request, Response, _APSService);
    if (tokens == null)
    {
      return Unauthorized();
    }
    var versions = await _APSService.GetVersions(hub, project, item, tokens);
    return JsonConvert.SerializeObject(versions);
  }
}