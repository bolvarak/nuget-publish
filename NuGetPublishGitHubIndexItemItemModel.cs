using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

// Define our namespace
namespace NuGet.Publish;

/// <summary>
///     This class models the index.json file that is used to store the versions of the nuget packages on GitHub
/// </summary>
[XmlInclude(typeof(NuGetPublishGitHubIndexItemItemCatalogEntryModel))]
[XmlRoot("package")]
public class NuGetPublishGitHubIndexItemItemModel
{
    /// <summary>
    ///     This property contains the catalog entry of the GitHub package index.
    /// </summary>
    [JsonPropertyName("catalogEntry")]
    [XmlElement("catalogEntry")]
    public NuGetPublishGitHubIndexItemItemCatalogEntryModel CatalogEntry { get; set; } = new();

    /// <summary>
    ///     This property contains the unnecessary elements from the GitHub package index.
    /// </summary>
    [JsonExtensionData]
    [XmlIgnore]
    public Dictionary<string, JsonElement> Elements { get; set; }
}
