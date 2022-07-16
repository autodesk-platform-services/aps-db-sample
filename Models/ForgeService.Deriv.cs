using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Forge;

public record TranslationStatus(string Status, string Progress, IEnumerable<string>? Messages);

public partial class ForgeService
{
	public static string Base64Encode(string plainText)
	{
		var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
		return System.Convert.ToBase64String(plainTextBytes).TrimEnd('=');
	}

	public async Task<TranslationStatus> GetTranslationStatus(string urn)
	{
		var token = await GetInternalToken();
		var api = new DerivativesApi();
		api.Configuration.AccessToken = token.AccessToken;
		var json = (await api.GetManifestAsync(urn)).ToJson();
		var messages = new List<string>();
		foreach (var message in json.SelectTokens("$.derivatives[*].messages[?(@.type == 'error')].message"))
		{
			if (message.Type == Newtonsoft.Json.Linq.JTokenType.String)
				messages.Add((string)message);
		}
		foreach (var message in json.SelectTokens("$.derivatives[*].children[*].messages[?(@.type == 'error')].message"))
		{
			if (message.Type == Newtonsoft.Json.Linq.JTokenType.String)
				messages.Add((string)message);
		}
		return new TranslationStatus((string)json["status"], (string)json["progress"], messages);
	}
}