using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

// Define our namespace
namespace NuGet.Publish;

/// <summary>
///     This class models the catalog entry of the index.json file that is used to store the versions of the nuget packages on GitHub
/// </summary>
[XmlRoot("catalogEntry")]
public class NuGetPublishGitHubIndexItemItemCatalogEntryModel
{
    /// <summary>
    ///     This property contains the unnecessary elements from the GitHub package index.
    /// </summary>
    [JsonExtensionData]
    [XmlIgnore]
    public Dictionary<string, JsonElement> Elements { get; set; }

    /// <summary>
    ///     This property contains the version of the GitHub package.
    /// </summary>
    [JsonPropertyName("version")]
    [XmlElement("version")]
    public string Version { get; set; }
}
