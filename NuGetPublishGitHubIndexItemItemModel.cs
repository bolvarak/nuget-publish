using System.Text.Json.Serialization;

// Define our namespace
namespace NuGet.Publish;

/// <summary>
///     This class models the index.json file that is used to store the versions of the nuget packages on GitHub
/// </summary>
public class NuGetPublishGitHubIndexItemItemModel
{
    /// <summary>
    ///     This property contains the catalog entry of the GitHub package index.
    /// </summary>
    [JsonPropertyName("catalogEntry")]
    public NuGetPublishGitHubIndexItemItemCatalogEntryModel CatalogEntry { get; set; } = new();

    /// <summary>
    ///     This property contains the unnecessary elements from the GitHub package index.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object> ExtraProperties { get; set; } = new();
}
