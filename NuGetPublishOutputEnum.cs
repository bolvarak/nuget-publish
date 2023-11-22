// Define our namespace
namespace NuGet.Publish;

/// <summary>
///     This enum represents the output format of the NuGet Publish application.
/// </summary>
public enum NuGetPublishOutputEnum
{
    /// <summary>
    ///     This enum value denotes that the output should be generated in JSON format.
    /// </summary>
    Json,

    /// <summary>
    ///     This enum value denotes that the output should be generated in plain text format.
    /// </summary>
    Plain,

    /// <summary>
    ///     This enum value denotes that no output will be generated.
    /// </summary>
    Silent,

    /// <summary>
    ///     This enum value denotes that the output should be generated in XML format.
    /// </summary>
    Xml
}
