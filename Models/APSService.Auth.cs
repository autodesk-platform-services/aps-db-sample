using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Authentication.Model;

public partial class APSService
{
  private Tokens _internalTokenCache;
  private Tokens _publicTokenCache;

  public string GetAuthorizationURL()
  {
    return _authClient.Authorize(_clientId, ResponseType.Code, _callbackUri, InternalTokenScopes);
  }

  public async Task<Tokens> GenerateTokens(string code)
  {
    ThreeLeggedToken internalAuth = await _authClient.GetThreeLeggedTokenAsync(_clientId, _clientSecret, code, _callbackUri);
    RefreshToken publicAuth = await _authClient.GetRefreshTokenAsync(_clientId, _clientSecret, internalAuth.RefreshToken, PublicTokenScopes);
    return new Tokens
    {
      PublicToken = publicAuth.AccessToken,
      InternalToken = internalAuth.AccessToken,
      RefreshToken = publicAuth._RefreshToken,
      ExpiresAt = DateTime.Now.ToUniversalTime().AddSeconds((double)internalAuth.ExpiresIn)
    };
  }

  public async Task<Tokens> RefreshTokens(Tokens tokens)
  {
    RefreshToken internalAuth = await _authClient.GetRefreshTokenAsync(_clientId, _clientSecret, tokens.RefreshToken, InternalTokenScopes);
    RefreshToken publicAuth = await _authClient.GetRefreshTokenAsync(_clientId, _clientSecret, internalAuth._RefreshToken, PublicTokenScopes);
    return new Tokens
    {
      PublicToken = publicAuth.AccessToken,
      InternalToken = internalAuth.AccessToken,
      RefreshToken = publicAuth._RefreshToken,
      ExpiresAt = DateTime.Now.ToUniversalTime().AddSeconds((double)internalAuth.ExpiresIn).AddSeconds(-1700)
    };
  }

  public async Task<UserInfo> GetUserProfile(Tokens tokens)
  {
    var userInfo = await _authClient.GetUserInfoAsync(tokens.InternalToken);
    return userInfo;
  }

  private async Task<Tokens> GetToken(List<Scopes> scopes)
  {
    TwoLeggedToken auth = await _authClient.GetTwoLeggedTokenAsync(_clientId, _clientSecret, scopes);
    return new Tokens
    {
      PublicToken = auth.AccessToken,
      InternalToken = auth.AccessToken,
      RefreshToken = null,
      ExpiresAt = DateTime.UtcNow.AddSeconds((double)auth.ExpiresIn)
    };
  }

  private async Task<Tokens> GetInternalToken()
  {
    if (_internalTokenCache == null || _internalTokenCache.ExpiresAt < DateTime.UtcNow)
      _internalTokenCache = await GetToken(new List<Scopes> { Scopes.BucketCreate, Scopes.BucketRead, Scopes.DataRead, Scopes.DataWrite, Scopes.DataCreate });
    return _internalTokenCache;
  }

  public async Task<Tokens> GetPublicToken()
  {
    if (_publicTokenCache == null || _publicTokenCache.ExpiresAt < DateTime.UtcNow)
      _publicTokenCache = await GetToken(new List<Scopes> { Scopes.ViewablesRead });
    return _publicTokenCache;
  }
}