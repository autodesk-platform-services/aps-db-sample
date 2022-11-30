using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
  public record AccessToken(string access_token, long expires_in);

  private readonly ILogger<AuthController> _logger;
  private readonly APSService _APSService;

  public AuthController(ILogger<AuthController> logger, APSService APSService)
  {
    _logger = logger;
    _APSService = APSService;
  }

  public static async Task<Tokens> PrepareTokens(HttpRequest request, HttpResponse response, APSService APSService)
  {
    if (!request.Cookies.ContainsKey("internal_token"))
    {
      return null;
    }
    var tokens = new Tokens
    {
      PublicToken = request.Cookies["public_token"],
      InternalToken = request.Cookies["internal_token"],
      RefreshToken = request.Cookies["refresh_token"],
      ExpiresAt = DateTime.Parse(request.Cookies["expires_at"])
    };
    if (tokens.ExpiresAt < DateTime.Now.ToUniversalTime())
    {
      tokens = await APSService.RefreshTokens(tokens);
      response.Cookies.Append("public_token", tokens.PublicToken);
      response.Cookies.Append("internal_token", tokens.InternalToken);
      response.Cookies.Append("refresh_token", tokens.RefreshToken);
      response.Cookies.Append("expires_at", tokens.ExpiresAt.ToString());
    }
    return tokens;
  }

  [HttpGet("login")]
  public ActionResult Login()
  {
    var redirectUri = _APSService.GetAuthorizationURL();
    return Redirect(redirectUri);
  }

  [HttpGet("logout")]
  public ActionResult Logout()
  {
    Response.Cookies.Delete("public_token");
    Response.Cookies.Delete("internal_token");
    Response.Cookies.Delete("refresh_token");
    Response.Cookies.Delete("expires_at");
    return Redirect("/");
  }

  [HttpGet("callback")]
  public async Task<ActionResult> Callback(string code)
  {
    var tokens = await _APSService.GenerateTokens(code);
    Response.Cookies.Append("public_token", tokens.PublicToken);
    Response.Cookies.Append("internal_token", tokens.InternalToken);
    Response.Cookies.Append("refresh_token", tokens.RefreshToken);
    Response.Cookies.Append("expires_at", tokens.ExpiresAt.ToString());
    return Redirect("/");
  }

  [HttpGet("profile")]
  public async Task<dynamic> GetProfile(string? code)
  {
    var tokens = await PrepareTokens(Request, Response, _APSService);
    if (tokens == null)
    {
      return Unauthorized();
    }
    dynamic profile = await _APSService.GetUserProfile(tokens);
    return new
    {
      name = string.Format("{0} {1}", profile.firstName, profile.lastName)
    };
  }

  [HttpGet("token")]
  public async Task<dynamic> GetPublicToken(string? code)
  {
    var token = await _APSService.GetPublicToken();
    return new AccessToken(
        token.AccessToken,
        (long)Math.Round((token.ExpiresAt - DateTime.UtcNow).TotalSeconds)
    );
  }
}