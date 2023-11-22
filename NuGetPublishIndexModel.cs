using System.Text.Json.Serialization;
using System.Xml.Serialization;

// Define our namespace
namespace NuGet.Publish;

/// <summary>
///     This class models the index.json file that is used to store the versions of the nuget packages
/// </summary>
[XmlRoot("packageIndex")]
public class NuGetPublishIndexModel
{
    /// <summary>
    ///     This property contains the versions of the nuget packages
    /// </summary>
    [JsonPropertyName("versions")]
    public List<string> Versions { get; set; } = new();
}
