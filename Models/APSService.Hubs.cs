using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.DataManagement.Model;

public partial class APSService
{
  public async Task<List<HubsData>> GetHubs(Tokens tokens)
  {
    Hubs hubs = await _dataManagementClient.GetHubsAsync(accessToken: tokens.InternalToken);
    return hubs.Data;
  }

  public async Task<List<ProjectsData>> GetProjects(string hubId, Tokens tokens)
  {
    Projects projects = await _dataManagementClient.GetHubProjectsAsync(hubId, accessToken: tokens.InternalToken);
    return projects.Data;
  }

  public async Task<IEnumerable<dynamic>> GetContents(string hubId, string projectId, string? folderId, Tokens tokens)
  {
    var contents = new List<dynamic>();
    if (string.IsNullOrEmpty(folderId))
    {
      TopFolders topFolders = await _dataManagementClient.GetProjectTopFoldersAsync(hubId, projectId, accessToken: tokens.InternalToken);
      foreach (TopFoldersData topFolderData in topFolders.Data)
      {
        contents.Add(new
        {
          type = topFolderData.Type,
          id = topFolderData.Id,
          name = topFolderData.Attributes.DisplayName
        });
      }
    }
    else
    {
      FolderContents folderContents = await _dataManagementClient.GetFolderContentsAsync(projectId, folderId, accessToken: tokens.InternalToken);
      foreach (FolderContentsData folderContentData in folderContents.Data)
      {
        contents.Add(new
        {
          type = folderContentData.Type,
          id = folderContentData.Id,
          name = folderContentData.Attributes.DisplayName
        });
      }
    }
    return contents;
  }

  public async Task<IEnumerable<dynamic>> GetVersions(string hubId, string projectId, string itemId, Tokens tokens)
  {
    var versions = new List<dynamic>();
    Versions versionsData = await _dataManagementClient.GetItemVersionsAsync(projectId, itemId, accessToken: tokens.InternalToken);
    foreach (VersionsData version in versionsData.Data)
    {
      versions.Add(new
      {
        id = version.Id,
        createTime = version.Attributes.CreateTime
      });
    }
    return versions;
  }
}