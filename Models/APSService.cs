using System;
using System.Collections.Generic;
using Autodesk.Authentication;
using Autodesk.Authentication.Model;
using Autodesk.DataManagement;
using Autodesk.ModelDerivative;
using Autodesk.Oss;
using Autodesk.SDKManager;

public class Tokens
{
  public string InternalToken;
  public string PublicToken;
  public string RefreshToken;
  public DateTime ExpiresAt;
}

public partial class APSService
{
  private readonly string _clientId;
  private readonly string _clientSecret;
  private readonly string _callbackUri;
  private readonly string _bucket;
  private readonly AuthenticationClient _authClient;
  private readonly DataManagementClient _dataManagementClient;
  private readonly ModelDerivativeClient _modelDerivativeClient;
  private readonly OssClient _ossClient;
  private readonly List<Scopes> InternalTokenScopes = new List<Scopes> { Scopes.DataRead, Scopes.ViewablesRead };
  private readonly List<Scopes> PublicTokenScopes = new List<Scopes> { Scopes.DataRead, Scopes.ViewablesRead };

  public APSService(string clientId, string clientSecret, string callbackUri, string bucket = null)
  {
    _clientId = clientId;
    _clientSecret = clientSecret;
    _callbackUri = callbackUri;
    _bucket = string.IsNullOrEmpty(bucket) ? string.Format("{0}-basic-app", _clientId.ToLower()) : bucket;
    SDKManager sdkManager = SdkManagerBuilder
      .Create() // Creates SDK Manager Builder itself.
      .Build();
    _authClient = new AuthenticationClient(sdkManager);
    _dataManagementClient = new DataManagementClient(sdkManager);
    _modelDerivativeClient = new ModelDerivativeClient(sdkManager);
    _ossClient = new OssClient(sdkManager);
  }
}