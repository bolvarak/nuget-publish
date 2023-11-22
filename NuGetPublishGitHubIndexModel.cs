using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

// Define our namespace
namespace NuGet.Publish;

/// <summary>
///     This class models the index.json file that is used to store the versions of the nuget packages on GitHub
/// </summary>
[XmlInclude(typeof(NuGetPublishGitHubIndexItemModel))]
[XmlRoot("packageIndex")]
public class NuGetPublishGitHubIndexModel
{
    /// <summary>
    ///     This property contains the unnecessary elements from the GitHub package index.
    /// </summary>
    [JsonExtensionData]
    [XmlIgnore]
    public Dictionary<string, JsonElement> Elements { get; set; }

    /// <summary>
    ///     This property contains the items from the GitHub package index.
    /// </summary>
    [JsonPropertyName("items")]
    [XmlElement("item")]
    public List<NuGetPublishGitHubIndexItemModel> Items { get; set; } = new();
}
