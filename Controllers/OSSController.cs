using Autodesk.Forge;
using Autodesk.Forge.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace forgeSample.Controllers
{
    [ApiController]
    public class OSSController : ControllerBase
    {
        private IWebHostEnvironment _env;
        public OSSController(IWebHostEnvironment env) { _env = env; }
        public string ClientId { get { return OAuthController.GetAppSetting("FORGE_CLIENT_ID").ToLower(); } }

        /// <summary>
        /// Return list of buckets (id=#) or list of objects (id=bucketKey)
        /// </summary>
        [HttpGet]
        [Route("api/forge/oss/buckets")]
        public async Task<IList<TreeNode>> GetOSSAsync(string id)
        {
            IList<TreeNode> nodes = new List<TreeNode>();
            dynamic oauth = await OAuthController.GetInternalAsync();

            if (id == "#") // root
            {
                // in this case, let's return all buckets
                BucketsApi appBckets = new BucketsApi();
                appBckets.Configuration.AccessToken = oauth.access_token;

                // to simplify, let's return only the first 100 buckets
                dynamic buckets = await appBckets.GetBucketsAsync("US", 100);
                foreach (KeyValuePair<string, dynamic> bucket in new DynamicDictionaryItems(buckets.items))
                {
                    nodes.Add(new TreeNode(bucket.Value.bucketKey, bucket.Value.bucketKey.Replace(ClientId + "-", string.Empty), "bucket", true));
                }
            }
            else
            {
                // as we have the id (bucketKey), let's return all 
                ObjectsApi objects = new ObjectsApi();
                objects.Configuration.AccessToken = oauth.access_token;
                var objectsList = await objects.GetObjectsAsync(id, 100);
                foreach (KeyValuePair<string, dynamic> objInfo in new DynamicDictionaryItems(objectsList.items))
                {
                    nodes.Add(new TreeNode(Base64Encode((string)objInfo.Value.objectId),
                      objInfo.Value.objectKey, "object", false));
                }
            }
            return nodes;
        }

        /// <summary>
        /// Model data for jsTree used on GetOSSAsync
        /// </summary>
        public class TreeNode
        {
            public TreeNode(string id, string text, string type, bool children)
            {
                this.id = id;
                this.text = text;
                this.type = type;
                this.children = children;
            }

            public string id { get; set; }
            public string text { get; set; }
            public string type { get; set; }
            public bool children { get; set; }
        }

        /// <summary>
        /// Base64 enconde a string
        /// </summary>
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
}