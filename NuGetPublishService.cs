using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

// Define our namespace
namespace NuGet.Publish;

/// <summary>
///     This class maintains the service provider functionality for the NuGet Publish CLI.
/// </summary>
public class NuGetPublishService
{
    /// <summary>
    ///     This property contains the base NuGet configuration XML.
    /// </summary>
    private const string NugetConfigXml = """
                                          <?xml version="1.0" encoding="utf-8"?>
                                          <configuration>
                                              <packageSources></packageSources>
                                              <packageSourceCredentials></packageSourceCredentials>
                                          </configuration>
                                          """;

    /// <summary>
    ///     This property contains the <see cref="HttpClient" /> to use for HTTP requests.
    /// </summary>
    private static readonly HttpClient HttpClient = new();

    /// <summary>
    ///     This method instantiates a new <see cref="NuGetPublishService" /> instance.
    /// </summary>
    /// <param name="options">The service options from the CLI.</param>
    /// <returns>A new <see cref="NuGetPublishService" /> instance.</returns>
    public static NuGetPublishService NewInstance(NuGetPublishPublishInputModel options) => new(options);

    /// <summary>
    ///     This property contains the service options from the CLI.
    /// </summary>
    private readonly NuGetPublishPublishInputModel _options;

    /// <summary>
    ///     This property contains the outputs for the Github Action.
    /// </summary>
    private readonly NuGetPublishOutputModel _outputs = new();

    /// <summary>
    ///     This method instantiates our service provider with <paramref name="options" /> from the CLI.
    /// </summary>
    /// <param name="options">This service options from the CLI.</param>
    private NuGetPublishService(NuGetPublishPublishInputModel options) => _options = options;


    /// <summary>
    ///     This method authenticates with the NuGet Server for dependencies during the build process.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token to stop the command.</param>
    private async Task AuthenticateWithNuGetAsync(CancellationToken stoppingToken = default)
    {
        // Ensure we have a username and password for NuGet Server authentication
        if (string.IsNullOrEmpty(_options.NugetUsername) || string.IsNullOrWhiteSpace(_options.NugetUsername) ||
            string.IsNullOrEmpty(_options.NugetPassword) || string.IsNullOrWhiteSpace(_options.NugetPassword))
        {
            // Check the NuGet auth for build flag then send the message to the console
            if (_options.NugetAuthForBuild)
                PrintMessage(
                    "No NuGet Server authentication information provided so we're skipping NuGet build authentication.  This may cause problems in your build process.",
                    LogLevel.Warning);

            // We're done with this method
            return;
        }

        // Define our nuget.config file path
        string nugetConfigFilePath = Path.Combine(_options.WorkingDirectory, "nuget.config");

        // Localize any existing nuget configuration file content
        bool nuGetConfigurationExists = File.Exists(nugetConfigFilePath);

        // Define our XML string
        string xml = null;

        // Check for an existing NuGet.Config file
        if (nuGetConfigurationExists)
        {
            // Localize the existing NuGet.Config file
            xml = await File.ReadAllTextAsync(nugetConfigFilePath, stoppingToken);

            // Ensure the existing NuGet.Config file is valid XML
            xml = xml.Trim() is null or "" ? null : xml.Trim();
        }

        // Check for an existing NuGet configuration file then print the message to the console
        PrintMessage(nuGetConfigurationExists
            ? $"Existing NuGet configuration file found at {nugetConfigFilePath}."
            : $"Generating new NuGet configuration file at {nugetConfigFilePath}.");

        // Localize the NuGet.Config file
        string nugetConfig = await GetNuGetConfigurationAsync(xml, stoppingToken);

        // Write the NuGet.Config file back to the system
        await File.WriteAllTextAsync(nugetConfigFilePath, nugetConfig, stoppingToken);

        // Register the output of the NuGet source add
        _outputs.NugetConfiguration = await File.ReadAllTextAsync(nugetConfigFilePath, stoppingToken);
    }

    /// <summary>
    ///     This method checks for an existing package then publishes the new package if necessary.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token to stop the command.</param>
    /// <param name="authorize">Denotes whether to authorize the request or not.</param>
    /// <returns>An awaitable <see cref="Task" /> that resolves when the package verified or published.</returns>
    private async Task CheckForUpdateAsync(CancellationToken stoppingToken = default, bool authorize = false)
    {
        // Sent the message notifying the package name and version
        PrintMessage(authorize
            ? $"Authorizing check update request for {_options.PackageName} {_outputs.Version} at {_options.GetNuGetPackageIndexUrl()}..."
            : $"Checking for updates to {_options.PackageName} {_outputs.Version} at {_options.GetNuGetPackageIndexUrl()}...");

        // Define our request
        HttpRequestMessage request = new(HttpMethod.Get, _options.GetNuGetPackageIndexUrl());

        // Check for a GitHub NuGet server then add the authorization header to the request
        if (authorize)
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.NugetUsername}:{_options.NugetPassword}")));

        // Send the request
        HttpResponseMessage response = await HttpClient.SendAsync(request, stoppingToken);

        // Check for 401 unauthorized without authorization
        if (response.StatusCode is HttpStatusCode.Unauthorized && !authorize)
        {
            // Execute this method again with authorization enabled
            await CheckForUpdateAsync(stoppingToken, true);

            // We're done
            return;
        }

        // Check for a 401 unauthorized with authorization
        if (response.StatusCode is HttpStatusCode.Unauthorized && authorize)
        {
            // Print the message to the console
            await PrintMessageAndExitAsync(
                $"Unable to authenticate with NuGet Server at {_options.GetNuGetPackageIndexUrl()}.", LogLevel.Error,
                stoppingToken);

            // We're done
            return;
        }

        // Check for a 200 OK
        if (response.StatusCode is HttpStatusCode.OK)
        {
            // Deserialize the JSON response
            NuGetPublishIndexModel document = await JsonSerializer.DeserializeAsync<NuGetPublishIndexModel>(
                await response.Content.ReadAsStreamAsync(stoppingToken), null as JsonSerializerOptions, stoppingToken);

            // Check the document for the version we're trying to publish
            if (document.Versions.Any(v => v == _options.Version))
            {

                // Print the message to the console
                await PrintMessageAndExitAsync(
                    $"Existing package found for {_options.PackageName} {_outputs.Version} at {_options.GetNuGetPackageIndexUrl()}.",
                    LogLevel.Error, stoppingToken);

                // Exit the application
                return;
            }
        }

        // Check the response status code
        if (response.StatusCode is not HttpStatusCode.OK and not HttpStatusCode.NotFound)
        {
            // Print the message to the console
            await PrintMessageAndExitAsync(
                $"Unable to check for updates to {_options.PackageName} {_outputs.Version} at {_options.GetNuGetPackageIndexUrl()}.",
                LogLevel.Critical, stoppingToken);

            // Exit the application
            return;
        }

        // Print the message to the console
        PrintMessage(
            $"No existing package found for {_options.PackageName} {_outputs.Version} at {_options.GetNuGetPackageIndexUrl()}.");

        // Push the package to the NuGet Server
        await PublishPackageAsync(stoppingToken);
    }

    /// <summary>
    ///     This method executes a CLI command outside of the application.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="arguments">The arguments to pass to the command.</param>
    /// <param name="stoppingToken">The cancellation token to stop the command.</param>
    /// <param name="workingDirectoryOverride">The working directory to execute the command in.</param>
    /// <param name="exit">Denotes whether to exit the application on error or not.</param>
    /// <returns>An awaitable <see><cref>Task{Tuple{string, string}}</cref></see> that contains the standard output (stdout) and error (stderr) of the command.</returns>
    private async Task<Tuple<string, string>> ExecuteCommandAsync(string command, IEnumerable<string> arguments,
        CancellationToken stoppingToken = default, string workingDirectoryOverride = null, bool exit = true)
    {
        // Define our process
        using Process process = new();

        // Iterate over the arguments and add them to the process
        foreach (string argument in arguments) process.StartInfo.ArgumentList.Add(argument);

        // We won't want any window to show
        process.StartInfo.CreateNoWindow = true;

        // Iterate over the environment variables and pass them to the process
        foreach (DictionaryEntry variable in Environment.GetEnvironmentVariables())
        {
            // Localize the key
            string key = variable.Key.ToString();

            // Localize the value
            string value = variable.Value?.ToString();

            // Ensure we have a key and value then set the environment variable into the process
            if (key is not null && value is not null) process.StartInfo.EnvironmentVariables[key] = value;
        }

        // Define our process start info, starting with the command path
        process.StartInfo.FileName = command;

        // We'll want to redirect standard error
        process.StartInfo.RedirectStandardError = true;

        // We'll want to redirect standard output
        process.StartInfo.RedirectStandardOutput = true;

        // We'll want to use UTF-8 for the standard error output
        process.StartInfo.StandardErrorEncoding = Encoding.UTF8;

        // We'll want to use UTF-8 for the standard output
        process.StartInfo.StandardOutputEncoding = Encoding.UTF8;

        // We'll want to use the shell to execute the command
        process.StartInfo.UseShellExecute = false;

        // We'll want hidden windows, if any are created
        process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

        // We'll need to start in the appropriate working directory
        process.StartInfo.WorkingDirectory = workingDirectoryOverride ?? _options.WorkingDirectory;

        // Start the process, then if it doesn't start, send the message and exit the application
        if (!process.Start() && exit)
        {
            // Print the message to the console
            await PrintMessageAndExitAsync(await process.StandardError.ReadToEndAsync(stoppingToken), LogLevel.Critical,
                stoppingToken);

            // Exit the application
            return null;
        }

        // Wait for the process to exit, then return the standard output
        await process.WaitForExitAsync(stoppingToken);

        // Localize the standard error
        string stderr = await process.StandardError.ReadToEndAsync(stoppingToken);

        // Localize the standard output
        string stdout = await process.StandardOutput.ReadToEndAsync(stoppingToken);

        // Check for an error
        if (process.ExitCode is not 0 && exit)
        {
            // Print the message to the console
            await PrintMessageAndExitAsync(
                $"Process exited with:\n\t{(string.IsNullOrEmpty(stderr) || string.IsNullOrWhiteSpace(stderr) ? stdout : stderr)}",
                LogLevel.Critical, stoppingToken);

            // Exit the application
            return null;
        }

        // We're done, return the standard output
        return new(stdout, stderr);
    }

    /// <summary>
    ///     This method generates and returns a <see cref="XDocument" /> of the NuGet.Config file.
    /// </summary>
    /// <param name="xml">The source XML from any existing NuGet.Config file.</param>
    /// <param name="stoppingToken">The cancellation token to stop the command.</param>
    /// <returns>An awaitable <see><cref>Task{string}</cref></see> containing the complete NuGet.Config file.</returns>
    private async Task<string> GetNuGetConfigurationAsync(string xml = null, CancellationToken stoppingToken = default)
    {
        // Ensure we have XML
        xml ??= NugetConfigXml;

        // Localize our XML document
        XmlDocument document = new();

        // Load our XML into the document
        document.LoadXml(xml.Trim());

        // Localize the package source credentials node name
        string packageSourceCredentialsNodeName = _options.GetNuGetSourceName();

        // Check for package sources in the NuGet.Config document
        if (document.SelectSingleNode($"//configuration/packageSources/add[@value='{_options.GetNuGetServerIndex()}']")
            is null)
        {
            // Define our new package source element
            XmlElement packageSourceElement = document.CreateElement("add");

            // Define our key attribute
            XmlAttribute keyAttribute = document.CreateAttribute("key");

            // Set the key attribute
            keyAttribute.Value = packageSourceCredentialsNodeName;

            // Define our value attribute
            XmlAttribute valueAttribute = document.CreateAttribute("value");

            // Set the value attribute
            valueAttribute.Value = _options.GetNuGetServerIndex();

            // Add the key attribute to the package source element
            packageSourceElement.Attributes.Append(keyAttribute);

            // Add the value attribute to the package source element
            packageSourceElement.Attributes.Append(valueAttribute);

            // Add our package source to the NuGet.Config document
            document.SelectSingleNode("//configuration/packageSources")!.AppendChild(packageSourceElement);
        }

        // Otherwise reset the package source credentials node name
        else
            packageSourceCredentialsNodeName =
                document.SelectSingleNode(
                        $"//configuration/packageSources/add[@value='{_options.GetNuGetServerIndex()}']")!.Attributes!
                    .GetNamedItem("key")?.Value ?? _options.GetNuGetSourceName();

        // Check for package source credentials in the NuGet.Config document
        if (document.SelectSingleNode($"//configuration/packageSourceCredentials/{packageSourceCredentialsNodeName}") is
            null)
        {
            // Define our clear-text-password key attribute
            XmlAttribute clearTextPasswordKeyAttribute = document.CreateAttribute("key");

            // Set the clear-text-password key attribute
            clearTextPasswordKeyAttribute.Value = "ClearTextPassword";

            // Define our clear-text-password value attribute
            XmlAttribute clearTextPasswordValueAttribute = document.CreateAttribute("value");

            // Set the clear-text-password value attribute
            clearTextPasswordValueAttribute.Value = _options.NugetPassword;

            // Define our clear-text-password element
            XmlElement clearTextPasswordElement = document.CreateElement("add");

            // Add the clear-text-password key attribute to the clear-text-password element
            clearTextPasswordElement.Attributes.Append(clearTextPasswordKeyAttribute);

            // Add the clear-text-password value attribute to the clear-text-password element
            clearTextPasswordElement.Attributes.Append(clearTextPasswordValueAttribute);

            // Define our username key attribute
            XmlAttribute usernameKeyAttribute = document.CreateAttribute("key");

            // Set the username key attribute
            usernameKeyAttribute.Value = "Username";

            // Define our username value attribute
            XmlAttribute usernameValueAttribute = document.CreateAttribute("value");

            // Set the username value attribute
            usernameValueAttribute.Value = _options.NugetUsername;

            // Define our username element
            XmlElement usernameElement = document.CreateElement("add");

            // Add the username key attribute to the username element
            usernameElement.Attributes.Append(usernameKeyAttribute);

            // Add the username value attribute to the username element
            usernameElement.Attributes.Append(usernameValueAttribute);

            // Define our new package source credentials element
            XmlElement packageSourceCredentialsElement = document.CreateElement(packageSourceCredentialsNodeName);

            // Add the clear-text-password element to the package source credentials element
            packageSourceCredentialsElement.AppendChild(clearTextPasswordElement);

            // Add the username element to the package source credentials element
            packageSourceCredentialsElement.AppendChild(usernameElement);

            // Add our package source credentials to the NuGet.Config document
            document.SelectSingleNode("//configuration/packageSourceCredentials")!.AppendChild(
                packageSourceCredentialsElement);
        }

        // Otherwise create the package source credentials node
        else
        {
            // Check for a clear-text-password element
            if (document.SelectSingleNode(
                    $"//configuration/packageSourceCredentials/{packageSourceCredentialsNodeName}/add[@key='ClearTextPassword']")
                is null)
            {
                // Define our clear-text-password key attribute
                XmlAttribute clearTextPasswordKeyAttribute = document.CreateAttribute("key");

                // Set the clear-text-password key attribute
                clearTextPasswordKeyAttribute.Value = "ClearTextPassword";

                // Define our clear-text-password value attribute
                XmlAttribute clearTextPasswordValueAttribute = document.CreateAttribute("value");

                // Set the clear-text-password value attribute
                clearTextPasswordValueAttribute.Value = _options.NugetPassword;

                // Define our clear-text-password element
                XmlElement clearTextPasswordElement = document.CreateElement("add");

                // Add the clear-text-password key attribute to the clear-text-password element
                clearTextPasswordElement.Attributes.Append(clearTextPasswordKeyAttribute);

                // Add the clear-text-password value attribute to the clear-text-password element
                clearTextPasswordElement.Attributes.Append(clearTextPasswordValueAttribute);

                // Add the clear-text-password element to the package source credentials element
                document.SelectSingleNode(
                        $"//configuration/packageSourceCredentials/{packageSourceCredentialsNodeName}")!
                    .AppendChild(clearTextPasswordElement);
            }

            // Otherwise reset the clear-text-password element
            else
                document.SelectSingleNode(
                        $"//configuration/packageSourceCredentials/{packageSourceCredentialsNodeName}/add[@key='ClearTextPassword']")
                    !.Attributes!.GetNamedItem("value")!.Value = _options.NugetPassword;

            // Check for a username element
            if (document.SelectSingleNode(
                    $"//configuration/packageSourceCredentials/{packageSourceCredentialsNodeName}/add[@key='Username']")
                is null)
            {
                // Define our username key attribute
                XmlAttribute usernameKeyAttribute = document.CreateAttribute("key");

                // Set the username key attribute
                usernameKeyAttribute.Value = "Username";

                // Define our username value attribute
                XmlAttribute usernameValueAttribute = document.CreateAttribute("value");

                // Set the username value attribute
                usernameValueAttribute.Value = _options.NugetUsername;

                // Define our username element
                XmlElement usernameElement = document.CreateElement("add");

                // Add the username key attribute to the username element
                usernameElement.Attributes.Append(usernameKeyAttribute);

                // Add the username value attribute to the username element
                usernameElement.Attributes.Append(usernameValueAttribute);

                // Add the username element to the package source credentials element
                document.SelectSingleNode(
                        $"//configuration/packageSourceCredentials/{packageSourceCredentialsNodeName}")!
                    .AppendChild(usernameElement);
            }

            // Otherwise reset the username element
            else
                document.SelectSingleNode(
                        $"//configuration/packageSourceCredentials/{packageSourceCredentialsNodeName}/add[@key='Username']")
                    !.Attributes!.GetNamedItem("value")!.Value = _options.NugetUsername;
        }

        // Define our XML memory stream
        await using MemoryStream memory = new();

        // Define our XML writer
        await using XmlWriter writer = XmlWriter.Create(memory, new XmlWriterSettings
        {
            // We'll want to run this asynchronously
            Async = true,

            // We'll want to ensure everything is valid
            //CheckCharacters = true,

            // We'll want to keep our output open
            CloseOutput = false,

            // We should be dealing with a whole document
            ConformanceLevel = ConformanceLevel.Document,

            // We'll want UTF-8 encoding
            Encoding = new UTF8Encoding(false),

            // We'll want the XML formatted
            Indent = true,

            // We'll want to automatically close the tags when we're done
            WriteEndDocumentOnClose = true
        });

        // Write the XML to the writer
        document.Save(memory);

        // Flush the writer
        await writer.FlushAsync();

        // Reset the memory position
        memory.Position = 0;

        // Localize our stream reader
        using StreamReader reader = new(memory);

        // Reset the XML string
        xml = await reader.ReadToEndAsync(stoppingToken);

        // Read the memory stream and return the XML
        return xml.Trim();
    }

    /// <summary>
    ///     This method determines the NuGet password to use for authentication with the NuGet Server.
    /// </summary>
    private void GetNuGetPassword()
    {
        // Localize the password preferring the options over the environment
        if (string.IsNullOrEmpty(_options.NugetPassword) || string.IsNullOrWhiteSpace(_options.NugetPassword))
        {
            // Take the GITHUB_TOKEN from the environment variables if it exists and the NuGet API Key was not provided
            if (string.IsNullOrEmpty(_options.NugetApiKey) || string.IsNullOrWhiteSpace(_options.NugetApiKey))
                _options.NugetPassword = Environment.GetEnvironmentVariable("GITHUB_TOKEN")?.Trim();

            // Take the NuGet API Key if it was provided
            else if (!string.IsNullOrEmpty(_options.NugetApiKey) && !string.IsNullOrWhiteSpace(_options.NugetApiKey))
                _options.NugetPassword = _options.NugetApiKey?.Trim();

            // Default the password to null
            else
                _options.NugetPassword = null;
        }

        // Trim the NuGet password
        if (_options.NugetPassword is not null) _options.NugetPassword = _options.NugetPassword.Trim();
    }

    /// <summary>
    ///     This method determines the NuGet username to use for authentication with the NuGet Server.
    /// </summary>
    private void GetNuGetUsername()
    {
        // Localize the username preferring the options over the environment
        if (string.IsNullOrEmpty(_options.NugetUsername) || string.IsNullOrWhiteSpace(_options.NugetUsername))
        {
            // Reset the NuGet username to the GITHUB_ACTOR environment variable if it exists
            _options.NugetUsername = Environment.GetEnvironmentVariable("GITHUB_ACTOR")?.Trim();

            // Check the NuGet username then reset it to the GITHUB_TRIGGERING_ACTOR environment variable if it exists
            if (string.IsNullOrEmpty(_options.NugetUsername) || string.IsNullOrWhiteSpace(_options.NugetUsername))
                _options.NugetUsername = Environment.GetEnvironmentVariable("GITHUB_TRIGGERING_ACTOR")?.Trim();

            // Check the NuGet username then reset it null
            if (string.IsNullOrEmpty(_options.NugetUsername) || string.IsNullOrWhiteSpace(_options.NugetUsername))
                _options.NugetUsername = null;
        }

        // Trim the NuGet username
        if (_options.NugetUsername is not null) _options.NugetUsername = _options.NugetUsername.Trim();
    }

    /// <summary>
    ///     This method generates the package name if one isn't provided.
    /// </summary>
    /// <param name="projectFile">The path to the project or solution file.</param>
    /// <param name="document">The <see cref="XmlDocument" /> representing the project or solution file.</param>
    /// <param name="noPrint">Denotes whether to skip printing messages to the console or not.</param>
    /// <param name="stoppingToken">The cancellation token to stop the command.</param>
    private Task GetPackageNameAsync(string projectFile, XmlDocument document, bool noPrint = false,
        CancellationToken stoppingToken = default)
    {
        // Check for an existing package name
        if (!string.IsNullOrEmpty(_options.PackageName) && !string.IsNullOrWhiteSpace(_options.PackageName))
        {
            // Trim the package name
            _options.PackageName = _options.PackageName.Trim();

            // We're done
            return Task.CompletedTask;
        }

        // Check to see if we need to scan for the package name in the project or solution file
        if (_options.ScanForPackageName)
            _options.PackageName = document.SelectSingleNode("//Project/PropertyGroup/PackageId")?.InnerText.Trim() ??
                                   document.SelectSingleNode("//Project/PropertyGroup/AssemblyName")?.InnerText.Trim();

        // Set the package name from the project file
        _options.PackageName ??= Path.GetFileNameWithoutExtension(
            !string.IsNullOrEmpty(projectFile) && !string.IsNullOrWhiteSpace(projectFile)
                ? projectFile
                : Path.Combine(_options.WorkingDirectory, _options.Project!));

        // One final check for a package name, exit this time if we don't have one
        if (string.IsNullOrEmpty(_options.PackageName) || string.IsNullOrWhiteSpace(_options.PackageName))
            return PrintMessageAndExitAsync("Unable to find a package name.", LogLevel.Error, stoppingToken);

        // Print the message to the console
        if (noPrint is false) PrintMessage($"Using package name: {_options.PackageName}.");

        // We're done
        return Task.CompletedTask;
    }

    /// <summary>
    ///     This method generates the package version if one isn't provided.
    /// </summary>
    /// <param name="document">the <see cref="XmlDocument" /> representing the project or solution file.</param>
    /// <param name="noPrint">Denotes whether to skip printing messages to the console or not.</param>
    /// <param name="stoppingToken">The cancellation token to stop the command.</param>
    private Task GetPackageVersionAsync(XmlDocument document, bool noPrint = false,
        CancellationToken stoppingToken = default)
    {
        // Check for an existing package name
        if (!string.IsNullOrEmpty(_options.Version) && !string.IsNullOrWhiteSpace(_options.Version))
        {
            // Trim the package name
            _options.Version = _options.Version.Trim();

            // Set the output version
            _outputs.Version = $"v{_options.Version}";

            // We're done
            return Task.CompletedTask;
        }

        // Set the package name into the options
        _options.Version = document.SelectSingleNode("//Project/PropertyGroup/Version")?.InnerText.Trim();

        // One final check for a package name, exit this time if we don't have one
        if (string.IsNullOrEmpty(_options.Version) || string.IsNullOrWhiteSpace(_options.Version))
            return PrintMessageAndExitAsync("Unable to find a version.", LogLevel.Error, stoppingToken);

        // Trim the version in the options
        _options.Version = _options.Version.Trim();

        // Set the output version
        _outputs.Version = $"v{_options.Version}";

        // Print the message to the console
        if (noPrint is false) PrintMessage($"Using version: {_options.Version}.");

        // We're done
        return Task.CompletedTask;
    }

    /// <summary>
    ///     This method determines the working directory for the publish.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token to use.</param>
    private Task GetWorkingDirectoryAsync(CancellationToken stoppingToken = default)
    {
        // Check for a GitHub container environment then reset the project path
        if ((string.IsNullOrEmpty(_options.WorkingDirectory) || string.IsNullOrWhiteSpace(_options.WorkingDirectory)) &&
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_WORKSPACE")) &&
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GITHUB_WORKSPACE")))
            _options.WorkingDirectory = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE")!.Trim();

        // Otherwise, grab the directory from the project path
        else if (string.IsNullOrEmpty(_options.WorkingDirectory) ||
                 string.IsNullOrWhiteSpace(_options.WorkingDirectory))
            _options.WorkingDirectory = Path.GetDirectoryName(_options.Project);

        // Otherwise trim the working directory
        else _options.WorkingDirectory = _options.WorkingDirectory?.Trim();

        // Ensure we have a working directory
        if (string.IsNullOrEmpty(_options.WorkingDirectory) || string.IsNullOrWhiteSpace(_options.WorkingDirectory))
            return PrintMessageAndExitAsync("Unable to find a working directory.", LogLevel.Critical, stoppingToken);

        // We're done
        return Task.CompletedTask;
    }

    /// <summary>
    ///     This method prints a <paramref name="message" /> to the console.
    /// </summary>
    /// <param name="message">The message to print to the console.</param>
    /// <param name="code">The severity of the message.</param>
    private void PrintMessage(string message, LogLevel code = LogLevel.Information) =>
        Console.WriteLine("[{0}] {1} {2}", code.ToString(), code > LogLevel.Warning ? "🙊" : "👽", message);

    /// <summary>
    ///     This method prints a <paramref name="message" /> then exits the application with an <paramref name="exitCode" />.
    /// </summary>
    /// <param name="message">The message to print to the console.</param>
    /// <param name="exitCode">The exit code to exit the application with.</param>
    /// <param name="stoppingToken">The cancellation token to stop the command.</param>
    private async Task PrintMessageAndExitAsync(string message, LogLevel exitCode = LogLevel.Information,
        CancellationToken stoppingToken = default)
    {
        // Print the message to the console
        PrintMessage(message, exitCode);

        // Ensure outputs get written
        await WriteOutputsAsync(stoppingToken);

        // Otherwise, exit the application normally
        Environment.Exit(exitCode > LogLevel.Warning ? 1 : 0);
    }

    /// <summary>
    ///     This method builds and publishes the project to the NuGet Server.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token to stop the command.</param>
    /// <returns>An awaitable <see cref="Task" /> that resolves when the package is built and published.</returns>
    private async Task PublishPackageAsync(CancellationToken stoppingToken = default)
    {
        // Print the message to the console
        PrintMessage(
            $"Found new version ({_outputs.Version}) of {_options.PackageName}...");

        // Ensure we have a NuGet Server API key
        if (string.IsNullOrEmpty(_options.NugetApiKey) || string.IsNullOrWhiteSpace(_options.NugetApiKey))
        {
            // Print the message to the console
            await PrintMessageAndExitAsync("NuGet API key not given.", LogLevel.Error, stoppingToken);

            // Exit the application
            return;
        }

        // Print the message to the console
        PrintMessage($"Using NuGet Server: {_options.GetNuGetServer()}...");

        // Check the authenticate with NuGet flag and the provided NuGet credentials then authenticate with the NuGet Server
        if (_options.NugetAuthForBuild) await AuthenticateWithNuGetAsync(stoppingToken);

        // Otherwise, print the skip message to the console
        else PrintMessage("Skipping NuGet build authentication.");

        // Print the message to the console
        PrintMessage($"Restoring {_options.PackageName} {_outputs.Version}...");

        // Restore the project then localize the response
        Tuple<string, string> restoreOutput = File.Exists(_outputs.NugetConfiguration)
            ? await ExecuteCommandAsync("dotnet",
                new[]
                {
                    "restore", _options.Project, "--configfile", _outputs.NugetConfiguration, "--no-cache",
                    "--verbosity", _options.Verbosity.ToString()
                }, stoppingToken)
            : await ExecuteCommandAsync("dotnet",
                new[] { "restore", _options.Project, "--no-cache", "--verbosity", _options.Verbosity.ToString() },
                stoppingToken);

        // Register the output of the restore
        _outputs.ProcessRestoreOutput = restoreOutput.Item1;

        // Register the error output of the restore
        _outputs.ProcessRestoreError = restoreOutput.Item2;

        // Print the message to the console
        PrintMessage($"Cleaning {_options.PackageName} {_outputs.Version}...");

        // Clean the project then localize the response
        Tuple<string, string> cleanOutput = await ExecuteCommandAsync("dotnet",
            new[]
            {
                "clean", _options.Project, $"-property:Platform={_options.Platform}", "--configuration",
                _options.Configuration.ToString(),
            }, stoppingToken);

        // Register the output of the clean
        _outputs.ProcessCleanOutput = cleanOutput.Item1;

        // Register the error output of the clean
        _outputs.ProcessCleanError = cleanOutput.Item2;

        // Print the message to the console
        PrintMessage($"Cleaning old nupkg files fo {_options.PackageName} {_options.Version}...");

        // Remove any old generated nupkg files
        foreach (string file in Directory.GetFiles(_options.WorkingDirectory, $"*.nupkg"))
        {
            // Print the message to the console
            PrintMessage($"Deleting old NuGet Package: {file}");

            // Delete the file from the filesystem
            File.Delete(file);
        }

        // Remove any old generated snupkg files
        foreach (string file in Directory.GetFiles(_options.WorkingDirectory, $"*.snupkg"))
        {
            // Print the message to the console
            PrintMessage($"Deleting old NuGet Symbols Package: {file}");


            // Delete the file from the filesystem
            File.Delete(file);
        }

        // Print the message to the console
        PrintMessage($"Building {_options.PackageName} {_outputs.Version}...");

        // Build the project then localize the response
        Tuple<string, string> buildOutput = await ExecuteCommandAsync("dotnet",
            new[]
            {
                "build", _options.Project, $"-property:Platform={_options.Platform}", "--configuration",
                _options.Configuration.ToString(), "--no-restore"
            }, stoppingToken);

        // Register the output of the build
        _outputs.ProcessBuildOutput = buildOutput.Item1;

        // Register the error output of the build
        _outputs.ProcessBuildError = buildOutput.Item2;

        // Print the message to the console
        PrintMessage($"Packing {_options.PackageName} {_outputs.Version}...");

        // Pack the project then localize the response
        Tuple<string, string> packOutput = await ExecuteCommandAsync("dotnet",
            new[]
            {
                "pack", _options.Project, $"-property:NuspecFile={_options.NuspecFile}",
                $"-property:Platform={_options.Platform}", "--configuration", _options.Configuration.ToString(),
                "--nologo", "--no-build", "--no-restore", "--output", _options.WorkingDirectory, "--verbosity",
                _options.Verbosity.ToString(),
            }, stoppingToken);

        // Register the output of the pack
        _outputs.ProcessPackOutput = packOutput.Item1;

        // Register the error output of the pack
        _outputs.ProcessPackError = packOutput.Item2;

        // Localize the package filename
        string packageFileName = Directory.GetFiles(_options.WorkingDirectory, "*.nupkg").FirstOrDefault();

        // Print the project and/or symbols package to the console
        PrintMessage($"Generated: {packageFileName}.");

        // Check for a package filename
        if (string.IsNullOrEmpty(packageFileName) || string.IsNullOrWhiteSpace(packageFileName))
        {
            // Print the message to the console
            await PrintMessageAndExitAsync("Unable to find a package to publish.", LogLevel.Warning, stoppingToken);

            // Exit the application
            return;
        }

        // Register the package filename to the outputs
        _outputs.PackageName = Path.GetFileName(packageFileName);

        // Register the package path to the outputs
        _outputs.PackagePath = Path.GetFullPath(packageFileName);

        // Print the message to the console
        PrintMessage($"Pushing {_options.PackageName} {_outputs.Version}...");

        // Push the package then localize the response
        Tuple<string, string> pushOutput = await ExecuteCommandAsync("dotnet",
            new[]
            {
                "nuget", "push", "*.nupkg", "--source", _options.GetNuGetServerIndex(), "--api-key",
                _options.NugetApiKey, "--skip-duplicate", "--no-symbols"
            }, stoppingToken);

        // Register the output of the push
        _outputs.ProcessNuGetPushOutput = pushOutput.Item1;

        // Register the error output of the push
        _outputs.ProcessNuGetPushError = pushOutput.Item2;

        // Check for errors
        if (Regex.IsMatch(pushOutput.Item1, "error.*",
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline))
            await PrintMessageAndExitAsync(
                $"{Regex.Match(pushOutput.Item1, "error.*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline).Value}",
                LogLevel.Error, stoppingToken);
    }

    /// <summary>
    ///     This method writes the outputs for the Github Action.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token to stop the command.</param>
    private Task WriteOutputsAsync(CancellationToken stoppingToken = default)
    {
        // Print the message to the console
        PrintMessage("Writing outputs...");

        // Write the outputs to the console
        return _outputs.WriteAsync(_options.Output, stoppingToken);
    }

    /// <summary>
    ///     This method executes the service provider process which
    ///     is to build and publish the project to the NuGet Server.
    /// </summary>
    /// <param name="stoppingToken"></param>
    public async Task ExecuteAsync(CancellationToken stoppingToken = default)
    {
        // Determine the working directory we should use
        await GetWorkingDirectoryAsync(stoppingToken);

        // Print the message
        PrintMessage($"Using working directory: {_options.WorkingDirectory}.");

        // Localize the project file path
        string projectFile = Path.Combine(_options.WorkingDirectory, _options.Project);

        // Ensure the project file exists
        if (!File.Exists(projectFile))
        {
            // Print the message to the console
            await PrintMessageAndExitAsync($"Unable to find project file: {projectFile}.", LogLevel.Critical,
                stoppingToken);

            // Exit the application
            return;
        }

        // Otherwise, print the message to the console
        PrintMessage($"Using project file: {projectFile}.");

        // Try to execute the action
        try
        {
            // Localize our XML document
            XmlDocument document = new();

            // Load our XML into the document
            document.LoadXml(await File.ReadAllTextAsync(projectFile, stoppingToken));

            // Ensure we've captured the NuGet password if one was provided
            GetNuGetPassword();

            // Ensure we've captured the NuGet username if one was provided
            GetNuGetUsername();

            // Ensure we have a version for the package
            await GetPackageVersionAsync(document, false, stoppingToken);

            // Ensure we have a name for the package
            await GetPackageNameAsync(projectFile, document, false, stoppingToken);

            // We're done, check for updates then publish the new package if necessary
            await CheckForUpdateAsync(stoppingToken);

            // Print the message to the console
            await PrintMessageAndExitAsync("Action complete.", LogLevel.Information, stoppingToken);
        }

        // Catch any unhandled exceptions that occur
        catch (Exception exception)
        {
            // Print the message to the console
            await PrintMessageAndExitAsync($"Unable to continue the action: {exception.Message}", LogLevel.Critical,
                stoppingToken);
        }
    }

    /// <summary>
    ///     This method generates a NuGet.Config file from scratch or from an existing one.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token to use.</param>
    /// <returns>An awaitable <see><cref>Task{string}</cref></see> containing the NuGet.Config XML contents.</returns>
    public async Task GenerateNuGetConfigurationAsync(CancellationToken stoppingToken = default)
    {
        // Determine the working directory we should use
        await GetWorkingDirectoryAsync(stoppingToken);

        // Define our nuget.config file path
        string nugetConfigFilePath = Path.Combine(_options.WorkingDirectory!, "nuget.config");

        // Localize any existing nuget configuration file content
        bool nuGetConfigurationExists = File.Exists(nugetConfigFilePath);

        // Define our XML string
        string xml = null;

        // Check for an existing NuGet.Config file
        if (nuGetConfigurationExists)
        {
            // Localize the existing NuGet.Config file
            xml = await File.ReadAllTextAsync(nugetConfigFilePath, stoppingToken);

            // Ensure the existing NuGet.Config file is valid XML
            xml = xml.Trim() is null or "" ? null : xml.Trim();
        }

        // Define the NuGet authentication username
        GetNuGetUsername();

        // Define the NuGet authentication password
        GetNuGetPassword();

        // Localize the NuGet.Config file
        string nugetConfig = await GetNuGetConfigurationAsync(xml, stoppingToken);

        // We're done, write the NuGet.Config XML to the console
        Console.WriteLine(nugetConfig);
    }

    /// <summary>
    ///     This method generates the name of the NuGet package from the project or solution file.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token to use.</param>
    public async Task GeneratePackageNameAsync(CancellationToken stoppingToken = default)
    {
        // Determine the working directory we should use
        await GetWorkingDirectoryAsync(stoppingToken);

        // Localize the project file path
        string projectFile = Path.Combine(_options.WorkingDirectory, _options.Project);

        // Ensure the project file exists
        if (!File.Exists(projectFile))
        {
            // Print the message to the console
            await PrintMessageAndExitAsync($"Unable to find project file: {projectFile}.", LogLevel.Critical,
                stoppingToken);

            // Exit the application
            return;
        }

        // Localize our XML document
        XmlDocument document = new();

        // Load our XML into the document
        document.LoadXml(await File.ReadAllTextAsync(projectFile, stoppingToken));

        // Ensure we have a name for the package
        await GetPackageNameAsync(projectFile, document, true, stoppingToken);

        // We're done, write the package name to the console
        Console.WriteLine(_options.PackageName);
    }

    /// <summary>
    ///     This method generates the version of the NuGet package from the project or solution file.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token to use.</param>
    public async Task GenerateVersionAsync(CancellationToken stoppingToken = default)
    {
        // Determine the working directory we should use
        await GetWorkingDirectoryAsync(stoppingToken);

        // Localize the project file path
        string projectFile = Path.Combine(_options.WorkingDirectory, _options.Project);

        // Ensure the project file exists
        if (!File.Exists(projectFile))
        {
            // Print the message to the console
            await PrintMessageAndExitAsync($"Unable to find project file: {projectFile}.", LogLevel.Critical,
                stoppingToken);

            // Exit the application
            return;
        }

        // Localize our XML document
        XmlDocument document = new();

        // Load our XML into the document
        document.LoadXml(await File.ReadAllTextAsync(projectFile, stoppingToken));

        // Ensure we have a version for the package
        await GetPackageVersionAsync(document, true, stoppingToken);

        // We're done, write the version to the console
        Console.WriteLine(_options.Version);
    }
}
