using System.Text.Json.Serialization;

// Define our namespace
namespace NuGet.Publish;

/// <summary>
///     This class represents the NuGet Server package index.
/// </summary>
public class NuGetPushPackageIndexModel
{
    /// <summary>
    ///     This property contains the package versions from the NuGet Server.
    /// </summary>
    [JsonPropertyName("versions")]
    public List<string> Versions { get; set; } = new();
}
