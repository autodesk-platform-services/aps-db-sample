using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Autodesk.Oss;
using Autodesk.Oss.Model;

public partial class APSService
{
  private async Task EnsureBucketExists(string bucketKey)
  {
    const string region = "US";
    var token = await GetInternalToken();
    try
    {
      await _ossClient.GetBucketDetailsAsync(bucketKey, accessToken: token.InternalToken);
    }
    catch (OssApiException e)
    {
      if (e.StatusCode == HttpStatusCode.NotFound)
      {
        var payload = new CreateBucketsPayload
        {
          BucketKey = bucketKey,
          PolicyKey = "persistent"
        };
        await _ossClient.CreateBucketAsync(region, payload, token.InternalToken);
      }
      else
      {
        throw e;
      }
    }
  }

  public async Task<IEnumerable<ObjectDetails>> GetObjects()
  {
    const int PageSize = 64;
    await EnsureBucketExists(_bucket);
    var token = await GetInternalToken();
    var results = new List<ObjectDetails>();
    var response = await _ossClient.GetObjectsAsync(_bucket, PageSize, accessToken: token.InternalToken);
    results.AddRange(response.Items);
    while (!string.IsNullOrEmpty(response.Next))
    {
      var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(response.Next).Query);
      response = await _ossClient.GetObjectsAsync(_bucket, PageSize, null, queryParams["startAt"], accessToken: token.InternalToken);
      results.AddRange(response.Items);
    }
    return results;
  }

  public async Task<string> GetBucketKey()
  {
    await EnsureBucketExists(_bucket);
    return _bucket;
  }
}