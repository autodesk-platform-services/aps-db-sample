using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Forge;
using Autodesk.Forge.Model;

public partial class APSService
{
  public async Task<IEnumerable<dynamic>> GetHubs(Tokens tokens)
  {
    var hubs = new List<dynamic>();
    var api = new HubsApi();
    api.Configuration.AccessToken = tokens.InternalToken;
    var response = await api.GetHubsAsync();
    foreach (KeyValuePair<string, dynamic> hub in new DynamicDictionaryItems(response.data))
    {
      hubs.Add(hub.Value);
    }
    return hubs;
  }

  public async Task<IEnumerable<dynamic>> GetProjects(string hubId, Tokens tokens)
  {
    var projects = new List<dynamic>();
    var api = new ProjectsApi();
    api.Configuration.AccessToken = tokens.InternalToken;
    var response = await api.GetHubProjectsAsync(hubId);
    foreach (KeyValuePair<string, dynamic> project in new DynamicDictionaryItems(response.data))
    {
      projects.Add(project.Value);
    }
    return projects;
  }

  public async Task<IEnumerable<dynamic>> GetContents(string hubId, string projectId, string? folderId, Tokens tokens)
  {
    var contents = new List<dynamic>();
    if (string.IsNullOrEmpty(folderId))
    {
      var api = new ProjectsApi();
      api.Configuration.AccessToken = tokens.InternalToken;
      var response = await api.GetProjectTopFoldersAsync(hubId, projectId);
      foreach (KeyValuePair<string, dynamic> folders in new DynamicDictionaryItems(response.data))
      {
        contents.Add(folders.Value);
      }
    }
    else
    {
      var api = new FoldersApi();
      api.Configuration.AccessToken = tokens.InternalToken;
      var response = await api.GetFolderContentsAsync(projectId, folderId); // TODO: add paging
      foreach (KeyValuePair<string, dynamic> item in new DynamicDictionaryItems(response.data))
      {
        contents.Add(item.Value);
      }
    }
    return contents;
  }

  public async Task<IEnumerable<dynamic>> GetVersions(string hubId, string projectId, string itemId, Tokens tokens)
  {
    var versions = new List<dynamic>();
    var api = new ItemsApi();
    api.Configuration.AccessToken = tokens.InternalToken;
    var response = await api.GetItemVersionsAsync(projectId, itemId);
    foreach (KeyValuePair<string, dynamic> version in new DynamicDictionaryItems(response.data))
    {
      versions.Add(version.Value);
    }
    return versions;
  }
}