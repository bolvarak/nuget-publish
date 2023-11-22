using System.Text.Json.Serialization;
using System.Xml.Serialization;
using CommandLine;
using Microsoft.Build.Framework;

// Define our namespace
namespace NuGet.Publish;

/// <summary>
/// This class represents the Command-Line-Interface options available to this application for the generate-configuration verb.
/// </summary>
[Verb("generate-configuration", false, new[] { "get-config", "gc" }, HelpText = "Generate NuGet.Config file.")]
public class NuGetPublishGenerateConfigurationInputModel
{
    /// <summary>
    ///     This property contains the GitHub NuGet Server URL.
    /// </summary>
    private const string GitHubNugetServer = "https://nuget.pkg.github.com";

    /// <summary>
    ///     This property contains the NuGet.Org NuGet Server URL.
    /// </summary>
    private const string NugetOrgNuGetServer = "https://api.nuget.org";

    /// <summary>
    ///     This property contains the GitHub Organization to publish the generated package to which forces the use of GitHub's NuGet Servers.
    /// </summary>
    [Option("github-organization", Required = false,
        HelpText =
            "The GitHub Organization to publish the generated package to which forces the use of GitHub's NuGet Servers.")]
    [JsonPropertyName("githubOrganization")]
    [XmlElement("githubOrganization")]
    public string GitHubOrganization { get; set; }

    /// <summary>
    ///     This property contains the API key which is used to authenticate with the NuGet server.
    /// </summary>
    [Option("nuget-api-key", Required = false,
        HelpText =
            "The API key which is used to authenticate with the NuGet server which is only required when publishing, otherwise it's used as a password source.")]
    [JsonPropertyName("nugetApiKey")]
    [XmlElement("nugetApiKey")]
    public string NugetApiKey { get; set; }

    /// <summary>
    ///     This property contains the password with which to authenticate with the NuGet Server during the build process.
    /// </summary>
    [Option("nuget-password", Required = false,
        HelpText = "The password with which to authenticate with the NuGet Server.")]
    [JsonPropertyName("nugetPassword")]
    [XmlElement("nugetPassword")]
    public string NugetPassword { get; set; }

    /// <summary>
    ///     This property contains the username with which to authenticate with the NuGet Server during the build process.
    /// </summary>
    [Option("nuget-username", Required = false,
        HelpText = "The username with which to authenticate with the NuGet Server.")]
    [JsonPropertyName("nugetUsername")]
    [XmlElement("nugetUsername")]
    public string NugetUsername { get; set; }

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
    ///     This method returns the NuGet Server URL.
    /// </summary>
    /// <returns>The NuGet Server URL.</returns>
    public string GetNuGetServer() => GitHubOrganization?.Trim() is not null
        ? $"{GitHubNugetServer}/{GitHubOrganization.Trim()}"
        : NugetOrgNuGetServer;

    /// <summary>
    ///     This method returns the NuGet Server Index URL.
    /// </summary>
    /// <returns>The NuGet Server Index URL.</returns>
    public string GetNuGetServerIndex() =>
        $"{GetNuGetServer()}{(GetNuGetServerIsGitHub() ? "/index.json" : "/v3/index.json")}";

    /// <summary>
    ///     This method returns whether the NuGet Server is GitHub or not.
    /// </summary>
    /// <returns>A flag denoting whether the NuGet Server is GitHub or not.</returns>
    public bool GetNuGetServerIsGitHub() => !string.IsNullOrEmpty(GitHubOrganization) && !string.IsNullOrWhiteSpace(GitHubOrganization);

    /// <summary>
    ///     This method returns the NuGet Source Name.
    /// </summary>
    /// <returns>The NuGet Source Name.</returns>
    public string GetNuGetSourceName() => GetNuGetServer().ToLower().Replace("https://", string.Empty)
        .Replace("http://", string.Empty).ToLower().Replace('/', '.').Trim();

    /// <summary>
    ///     This method converts the instance into a <see cref="NuGetPublishPublishInputModel" />.
    /// </summary>
    /// <returns>The <see cref="NuGetPublishPublishInputModel" /> representation of this instance.</returns>
    public NuGetPublishPublishInputModel ToPublishInputModel() => new()
    {
        // Set the configuration into the input model
        Configuration = NuGetPublishConfigurationEnum.Debug,

        // Set the GitHub organization into the input model
        GitHubOrganization = GitHubOrganization,

        // Set the NuGet authentication API key into the input model
        NugetApiKey = NugetApiKey,

        // We don't need to authentication for building since we're not building
        NugetAuthForBuild = false,

        // Set the NuGet authentication password into the input model
        NugetPassword = NugetPassword,

        // Set the NuGet authentication username into the input model
        NugetUsername = NugetUsername,

        // Set the NuSpec file into the input model
        NuspecFile = null,

        // Set the output format into the input model
        Output = NuGetPublishOutputEnum.Silent,

        // Set the package name into the input model
        PackageName = null,

        // Set the platform into the input model
        Platform = NuGetPublishPlatformEnum.AnyCpu,

        // Set the project or solution file into the input model
        Project = null,

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
