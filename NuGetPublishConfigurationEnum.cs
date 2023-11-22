using System.Text.Json.Serialization;

// Define our namespace
namespace NuGet.Publish;

/// <summary>
///     This enum defines our build configuration values.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<NuGetPublishConfigurationEnum>))]
public enum NuGetPublishConfigurationEnum
{
    /// <summary>
    ///     This enum value defines a Debug build configuration.
    /// </summary>
    Debug,

    /// <summary>
    ///     This enum value defines a Release build configuration.
    /// </summary>
    Release
}
