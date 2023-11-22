using System.Text.Json.Serialization;
using System.Xml.Serialization;
using CommandLine;
using Microsoft.Build.Framework;

// Define our namespace
namespace NuGet.Publish;

/// <summary>
///     This class represents the Command-Line-Interface options available to this application for the publish verb.
/// </summary>
[Verb("publish", true, new[] { "p" }, HelpText = "Publish <project> to <nuget-server>.")]
public class NuGetPublishPublishInputModel : NuGetPublishGenerateConfigurationInputModel
{
    /// <summary>
    ///     This property contains the build configuration to use when building
    ///     the project or solution.  (values: Debug|Release, default: Release)
    /// </summary>
    [Option("configuration", Required = false, Default = NuGetPublishConfigurationEnum.Release,
        HelpText =
            "The build configuration to use when building the project or solution.  (values: Debug|Release, default: Release)")]
    [JsonPropertyName("configuration")]
    [XmlElement("configuration")]
    public NuGetPublishConfigurationEnum Configuration { get; set; } =
        NuGetPublishConfigurationEnum.Release;

    /// <summary>
    ///     This property contains the API key which is used to authenticate with the NuGet server.
    /// </summary>
    [Option("nuget-api-key", Required = true,
        HelpText = "The API key which is used to authenticate with the NuGet servet.")]
    [JsonPropertyName("nugetApiKey")]
    [XmlElement("nugetApiKey")]
    public new string NugetApiKey { get; set; }

    /// <summary>
    ///     This property contains a flag that denotes whether to authenticate with the NuGet Server during the build process.
    /// </summary>
    [Option("nuget-auth-for-build", Required = false, Default = false,
        HelpText = "Flag that denotes whether to authenticate with the NuGet Server during the build process.")]
    [JsonPropertyName("nugetAuthForBuild")]
    [XmlElement("nugetAuthForBuild")]
    public bool NugetAuthForBuild { get; set; }

    /// <summary>
    ///     This property contains the path, relative to the root of the repository, to the NuSpec file.
    /// </summary>
    [Option("nuspec-file", Required = false,
        HelpText = "The path, relative to the root of the repository, to the NuSpec file.")]
    [JsonPropertyName("nuSpecFile")]
    [XmlElement("nuSpecFile")]
    public string NuspecFile { get; set; }

    /// <summary>
    ///     This property contains the output format for the application. (default: Plain)
    /// </summary>
    [Option("output", Required = false, Default = NuGetPublishOutputEnum.Plain,
        HelpText = "The output format for the application.")]
    public NuGetPublishOutputEnum Output { get; set; } = NuGetPublishOutputEnum.Plain;

    /// <summary>
    ///     This property contains the name of the package to generate,
    ///     that will be published to the NuGet server.
    /// </summary>
    [Option("package-name", Required = false,
        HelpText = "The name of the package to generate, that will be published to the NuGet server.")]
    [JsonPropertyName("packageName")]
    [XmlElement("packageName")]
    public string PackageName { get; set; }

    /// <summary>
    ///     This property contains the platform on which the package will run. (default: AnyCPU)
    /// </summary>
    [Option("platform", Required = false, Default = NuGetPublishPlatformEnum.AnyCpu,
        HelpText = "The platform on which the package will run.")]
    [JsonPropertyName("platform")]
    [XmlElement("platform")]
    public NuGetPublishPlatformEnum Platform { get; set; } = NuGetPublishPlatformEnum.AnyCpu;

    /// <summary>
    ///     This property contains the path, relative to the root of the
    ///     repository, to the project or solution file to be packaged.
    /// </summary>
    [Option("project", Required = true,
        HelpText = "The path, relative to the root of the repository, to the project or solution file to be packaged.")]
    [JsonPropertyName("project")]
    [XmlElement("project")]
    public string Project { get; set; }

    /// <summary>
    ///     This property contains a flag that denotes whether to scan the project or solution file
    ///     in an attempt to find the package name preferring PackageId over AssemblyName over &lt;project&gt;.
    ///     (default: false)
    /// </summary>
    [Option("scan-for-package-name", Required = false, Default = false,
        HelpText =
            "Flag that denotes whether to scan the project or solution file in an attempt to find the package name preferring PackageId over AssemblyName over <project>.")]
    [JsonPropertyName("scanForPackageName")]
    [XmlElement("scanForPackageName")]
    public bool ScanForPackageName { get; set; }

    /// <summary>
    ///     This property contains the output level for the build process.  (default: minimal)
    /// </summary>
    [Option("verbosity", Required = false, Default = LoggerVerbosity.Minimal,
        HelpText = "The output level for the build process.")]
    [JsonConverter(typeof(JsonStringEnumConverter<LoggerVerbosity>))]
    [JsonPropertyName("verbosity")]
    [XmlElement("verbosity")]
    public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Minimal;

    /// <summary>
    ///     This property contains the static version for the NuGet package.
    /// </summary>
    [Option("version", Required = false, Default = null, HelpText = "The static version for the NuGet package.")]
    [JsonPropertyName("version")]
    [XmlElement("version")]
    public string Version { get; set; }

    /// <summary>
    ///     This method returns the NuGet Package Index URL.
    /// </summary>
    /// <param name="packageName">The name of the package.</param>
    /// <returns>The NuGet Package Index URL.</returns>
    public string GetNuGetPackageIndex(string packageName = null) =>
        $"{(packageName ?? PackageName).Trim()}/index.json";
}
