using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.ModelDerivative;
using Autodesk.ModelDerivative.Model;

public record TranslationStatus(string Status, string Progress, IEnumerable<string>? Messages);

public partial class APSService
{
  public static string Base64Encode(string plainText)
  {
    var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
    return System.Convert.ToBase64String(plainTextBytes).TrimEnd('=');
  }

  public async Task<TranslationStatus> GetTranslationStatus(string urn)
  {
    var token = await GetInternalToken();
    Manifest manifest = await _modelDerivativeClient.GetManifestAsync(urn, Autodesk.ModelDerivative.Model.Region.US, accessToken: token.InternalToken);
    var messages = new List<string>();
    return new TranslationStatus(manifest.Status, manifest.Status, messages);
  }
}