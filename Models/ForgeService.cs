using System;
using Autodesk.Forge;

public class Tokens
{
	public string InternalToken;
	public string PublicToken;
	public string RefreshToken;
	public DateTime ExpiresAt;
}

public partial class ForgeService
{
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _callbackUri;
	private readonly string _bucket;
	private readonly Scope[] InternalTokenScopes = new Scope[] { Scope.DataRead, Scope.ViewablesRead };
	private readonly Scope[] PublicTokenScopes = new Scope[] { Scope.ViewablesRead };

	public ForgeService(string clientId, string clientSecret, string callbackUri, string bucket = null)
	{
		_clientId = clientId;
		_clientSecret = clientSecret;
		_callbackUri = callbackUri;
		_bucket = string.IsNullOrEmpty(bucket) ? string.Format("{0}-basic-app", _clientId.ToLower()) : bucket;
	}
}