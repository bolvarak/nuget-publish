using System.Text.Json.Serialization;

// Define our namespace
namespace NuGet.Publish;

/// <summary>
///     This class models the index.json file that is used to store the versions of the nuget packages on GitHub
/// </summary>
public class NuGetPublishGitHubIndexModel
{
    /// <summary>
    ///     This property contains the unnecessary elements from the GitHub package index.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object> ExtraProperties { get; set; } = new();

    /// <summary>
    ///     This property contains the items from the GitHub package index.
    /// </summary>
    [JsonPropertyName("items")]
    public List<NuGetPublishGitHubIndexItemModel> Items { get; set; } = new();
}
