using System.Text.Json.Serialization;

// Define our namespace
namespace NuGet.Publish;

/// <summary>
///     This class models the catalog entry of the index.json file that is used to store the versions of the nuget packages on GitHub
/// </summary>
public class NuGetPublishGitHubIndexItemItemCatalogEntryModel
{
    /// <summary>
    ///     This property contains the unnecessary elements from the GitHub package index.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object> ExtraProperties { get; set; }

    /// <summary>
    ///     This property contains the version of the GitHub package.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; }
}
