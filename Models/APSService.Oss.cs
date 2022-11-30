using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Forge;
using Autodesk.Forge.Client;
using Autodesk.Forge.Model;

public partial class APSService
{
  private async Task EnsureBucketExists(string bucketKey)
  {
    var token = await GetInternalToken();
    var api = new BucketsApi();
    api.Configuration.AccessToken = token.AccessToken;
    try
    {
      await api.GetBucketDetailsAsync(bucketKey);
    }
    catch (ApiException e)
    {
      if (e.ErrorCode == 404)
      {
        await api.CreateBucketAsync(new PostBucketsPayload(bucketKey, null, PostBucketsPayload.PolicyKeyEnum.Temporary));
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
    var api = new ObjectsApi();
    api.Configuration.AccessToken = token.AccessToken;
    var results = new List<ObjectDetails>();
    var response = (await api.GetObjectsAsync(_bucket, PageSize)).ToObject<BucketObjects>();
    results.AddRange(response.Items);
    while (!string.IsNullOrEmpty(response.Next))
    {
      var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(response.Next).Query);
      response = (await api.GetObjectsAsync(_bucket, PageSize, null, queryParams["startAt"])).ToObject<BucketObjects>();
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