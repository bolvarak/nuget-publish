using System.Text.Json.Serialization;
using System.Xml.Serialization;
using CommandLine;
using Microsoft.Build.Framework;

// Define our namespace
namespace NuGet.Publish;

/// <summary>
///     This class represents the input model for generate-version.
/// </summary>
[Verb("generate-version", false, new []{ "get-version", "gv" }, HelpText = "Generate the version from the project")]
public class NuGetPublishGenerateVersionInputModel
{
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
    ///     This property defines the working directory for the processes, this defaults to the
    ///     GITHUB_WORKSPACE environment variable or the containing directory of the &lt;project&gt; file.
    /// </summary>
    [Option("working-directory", Required = false, Default = null,
        HelpText =
            "The working directory for the processes, this defaults to the GITHUB_WORKSPACE environment variable or the containing directory of the <project> file.")]
    [JsonPropertyName("workingDirectory")]
    [XmlElement("workingDirectory")]
    public string WorkingDirectory { get; set; }

    /// <summary>
    ///     This method converts the instance into a <see cref="NuGetPublishPublishInputModel" />.
    /// </summary>
    /// <returns>The <see cref="NuGetPublishPublishInputModel" /> representation of this instance.</returns>
    public NuGetPublishPublishInputModel ToPublishInputModel() => new()
    {
        // Set the configuration into the input model
        Configuration = NuGetPublishConfigurationEnum.Debug,

        // Set the GitHub organization into the input model
        GitHubOrganization = null,

        // Set the NuGet authentication API key into the input model
        NugetApiKey = null,

        // We don't need to authentication for building since we're not building
        NugetAuthForBuild = false,

        // Set the NuGet authentication password into the input model
        NugetPassword = null,

        // Set the NuGet authentication username into the input model
        NugetUsername = null,

        // Set the NuSpec file into the input model
        NuspecFile = null,

        // Set the output format into the input model
        Output = NuGetPublishOutputEnum.Silent,

        // Set the package name into the input model
        PackageName = null,

        // Set the platform into the input model
        Platform = NuGetPublishPlatformEnum.AnyCpu,

        // Set the project or solution file into the input model
        Project = Project,

        // We won't need to scan for a package name since we're not building
        ScanForPackageName = false,

        // Set the log verbosity into the input model
        Verbosity = LoggerVerbosity.Quiet,

        // Set the version into the input model
        Version = null,

        // Set the working directory into the input model
        WorkingDirectory = WorkingDirectory
    };
}
