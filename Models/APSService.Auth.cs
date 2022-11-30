﻿using System;
using System.Threading.Tasks;
using Autodesk.Forge;

public record Token(string AccessToken, DateTime ExpiresAt);

public partial class APSService
{
  private Token _internalTokenCache;
  private Token _publicTokenCache;

  public string GetAuthorizationURL()
  {
    return new ThreeLeggedApi().Authorize(_clientId, "code", _callbackUri, InternalTokenScopes);
  }

  public async Task<Tokens> GenerateTokens(string code)
  {
    dynamic internalAuth = await new ThreeLeggedApi().GettokenAsync(_clientId, _clientSecret, "authorization_code", code, _callbackUri);
    dynamic publicAuth = await new ThreeLeggedApi().RefreshtokenAsync(_clientId, _clientSecret, "refresh_token", internalAuth.refresh_token, PublicTokenScopes);
    return new Tokens
    {
      PublicToken = publicAuth.access_token,
      InternalToken = internalAuth.access_token,
      RefreshToken = publicAuth.refresh_token,
      ExpiresAt = DateTime.Now.ToUniversalTime().AddSeconds(internalAuth.expires_in)
    };
  }

  public async Task<Tokens> RefreshTokens(Tokens tokens)
  {
    dynamic internalAuth = await new ThreeLeggedApi().RefreshtokenAsync(_clientId, _clientSecret, "refresh_token", tokens.RefreshToken, InternalTokenScopes);
    dynamic publicAuth = await new ThreeLeggedApi().RefreshtokenAsync(_clientId, _clientSecret, "refresh_token", internalAuth.refresh_token, PublicTokenScopes);
    return new Tokens
    {
      PublicToken = publicAuth.access_token,
      InternalToken = internalAuth.access_token,
      RefreshToken = publicAuth.refresh_token,
      ExpiresAt = DateTime.Now.ToUniversalTime().AddSeconds(internalAuth.expires_in)
    };
  }

  public async Task<dynamic> GetUserProfile(Tokens tokens)
  {
    var api = new UserProfileApi();
    api.Configuration.AccessToken = tokens.InternalToken;
    dynamic profile = await api.GetUserProfileAsync();
    return profile;
  }

  private async Task<Token> GetToken(Scope[] scopes)
  {
    dynamic auth = await new TwoLeggedApi().AuthenticateAsync(_clientId, _clientSecret, "client_credentials", scopes);
    return new Token(auth.access_token, DateTime.UtcNow.AddSeconds(auth.expires_in));
  }

  private async Task<Token> GetInternalToken()
  {
    if (_internalTokenCache == null || _internalTokenCache.ExpiresAt < DateTime.UtcNow)
      _internalTokenCache = await GetToken(new Scope[] { Scope.BucketCreate, Scope.BucketRead, Scope.DataRead, Scope.DataWrite, Scope.DataCreate });
    return _internalTokenCache;
  }

  public async Task<Token> GetPublicToken()
  {
    if (_publicTokenCache == null || _publicTokenCache.ExpiresAt < DateTime.UtcNow)
      _publicTokenCache = await GetToken(new Scope[] { Scope.ViewablesRead });
    return _publicTokenCache;
  }
}