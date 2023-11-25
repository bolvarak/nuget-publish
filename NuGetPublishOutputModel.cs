using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Serialization;

// Define our namespace
namespace NuGet.Publish;

/// <summary>
///     This class represents the output of the NuGet Publish application.
/// </summary>
[XmlRoot("output")]
public class NuGetPublishOutputModel
{
    /// <summary>
    ///     This property contains the generated NuGet.Config file if one was generated.
    /// </summary>
    [JsonPropertyName("nugetConfig")]
    [NuGetPublishOutput("nuget_config")]
    [XmlElement("nugetConfig")]
    public string NugetConfiguration { get; set; }

    /// <summary>
    ///     This property contains the name of the package which was generated.
    /// </summary>
    [JsonPropertyName("packageName")]
    [NuGetPublishOutput("package_name")]
    [XmlElement("packageName")]
    public string PackageName { get; set; }

    /// <summary>
    ///     This property contains the path to the package which was generated.
    /// </summary>
    [JsonPropertyName("packagePath")]
    [NuGetPublishOutput("package-path")]
    [XmlElement("packagePath")]
    public string PackagePath { get; set; }

    /// <summary>
    ///     This property contains the standard error output from the process which built the package.
    /// </summary>
    [JsonPropertyName("processBuildError")]
    [NuGetPublishOutput("process-build-error")]
    [XmlElement("processBuildError")]
    public string ProcessBuildError { get; set; }

    /// <summary>
    ///     This property contains the standard output from the process which built the package.
    /// </summary>
    [JsonPropertyName("processBuildOutput")]
    [NuGetPublishOutput("process-build-output")]
    [XmlElement("processBuildOutput")]
    public string ProcessBuildOutput { get; set; }

    /// <summary>
    ///     This property contains the standard error output from the process which cleaned the package.
    /// </summary>
    [JsonPropertyName("processCleanError")]
    [NuGetPublishOutput("process-clean-error")]
    [XmlElement("processCleanError")]
    public string ProcessCleanError { get; set; }

    /// <summary>
    ///     This property contains the standard output from the process which cleaned the package.
    /// </summary>
    [JsonPropertyName("processCleanOutput")]
    [NuGetPublishOutput("process-clean-output")]
    [XmlElement("processCleanOutput")]
    public string ProcessCleanOutput { get; set; }

    /// <summary>
    ///     This property contains the standard error output from the process which pushed the package to the NuGet server.
    /// </summary>
    [JsonPropertyName("processNuGetPushError")]
    [NuGetPublishOutput("process-nuget-push-error")]
    [XmlElement("processNuGetPushError")]
    public string ProcessNuGetPushError { get; set; }

    /// <summary>
    ///     This property contains the standard output from the process which pushed the package to the NuGet server.
    /// </summary>
    [JsonPropertyName("processNuGetPushOutput")]
    [NuGetPublishOutput("process-nuget-push-output")]
    [XmlElement("processNuGetPushOutput")]
    public string ProcessNuGetPushOutput { get; set; }

    /// <summary>
    ///     This property contains the standard error output from the process which packed the package.
    /// </summary>
    [JsonPropertyName("processPackError")]
    [NuGetPublishOutput("process-pack-error")]
    [XmlElement("processPackError")]
    public string ProcessPackError { get; set; }

    /// <summary>
    ///     This property contains the standard output from the process which packed the package.
    /// </summary>
    [JsonPropertyName("processPackOutput")]
    [NuGetPublishOutput("process-pack-output")]
    [XmlElement("processPackOutput")]
    public string ProcessPackOutput { get; set; }

    /// <summary>
    ///     This property contains the standard error output from the process which restored the package.
    /// </summary>
    [JsonPropertyName("processRestoreError")]
    [NuGetPublishOutput("process-restore-error")]
    [XmlElement("processRestoreError")]
    public string ProcessRestoreError { get; set; }

    /// <summary>
    ///     This property contains the standard output from the process which restored the package.
    /// </summary>
    [JsonPropertyName("processRestoreOutput")]
    [NuGetPublishOutput("process-restore-output")]
    [XmlElement("processRestoreOutput")]
    public string ProcessRestoreOutput { get; set; }

    /// <summary>
    ///     This property contains the version of the package which was generated.
    /// </summary>
    [JsonPropertyName("version")]
    [NuGetPublishOutput("version")]
    [XmlElement("version")]
    public string Version { get; set; }

    /// <summary>
    ///     This method writes the output to the <paramref name="outputFile" /> as Bash-style variables.
    /// </summary>
    /// <param name="outputFile">The output file to write to.</param>
    /// <param name="stoppingToken">The token to monitor for cancellation requests.</param>
    private Task WriteToOutputFileAsync(string outputFile, CancellationToken stoppingToken = default)
    {
        // Ensure we have a path to write to
        if (string.IsNullOrEmpty(outputFile) || string.IsNullOrWhiteSpace(outputFile) || !File.Exists(outputFile))
            return Task.CompletedTask;

        // Define our lines to write
        List<string> lines = new();

        // Iterate over the public properties on the class
        foreach (PropertyInfo property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .OrderBy(p => p.Name))
        {
            // Check to see if we need to ignore this property
            if (property.GetCustomAttribute<JsonIgnoreAttribute>() is not null) continue;

            // Get the property for a specific output name, otherwise use the property name
            NuGetPublishOutputAttribute attribute =
                property.GetCustomAttribute<NuGetPublishOutputAttribute>() ?? new(property.Name);

            // Get the value of the property from the current instance
            string value = property.GetValue(this)?.ToString()?.Trim() ?? string.Empty;

            // Check to see if the value starts with double quotes
            if (!value.StartsWith("\"")) value = $"\"{value.Trim()}\"";

            // Check for a value
            if (value != string.Empty) value = value.Trim().Replace("\n", "\"$'\\n'\"").Trim();

            // Add te line to the outputs
            lines.Add($"{attribute.Name}={value}");
        }

        // Write the lines to the file
        return File.AppendAllLinesAsync(outputFile, lines, stoppingToken);
    }

    /// <summary>
    ///     This method writes the output to the console.
    /// </summary>
    /// <param name="outputFormat">The output format to use.</param>
    /// <param name="outputFile">The output file to write to.</param>
    /// <param name="stoppingToken">The token to monitor for cancellation requests.</param>
    public async Task WriteAsync(NuGetPublishOutputEnum outputFormat, string outputFile = null,
        CancellationToken stoppingToken = default)
    {
        // Write to GitHub
        if (outputFile is not null) await WriteToOutputFileAsync(outputFile, stoppingToken);

        // Check the output format
        if (outputFormat is NuGetPublishOutputEnum.Silent) return;

        // Check for JSON output
        if (outputFormat is NuGetPublishOutputEnum.Json)
        {
            // Serialize the output the write it to the console
            Console.WriteLine(new Regex(@"(\\u(?<Value>[a-zA-Z0-9]{4}))+", RegexOptions.Compiled).Replace(
                JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    // We don't want anything ignored
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never,

                    // We'll want pretty JSON
                    WriteIndented = true
                }), m => HttpUtility.UrlDecode(m.Groups[0].Value.Replace(@"\u00", "%"))));

            // We're done
            return;
        }

        // Check for plain-text output
        if (outputFormat is NuGetPublishOutputEnum.Plain)
        {
            // Iterate over the public properties on the class
            foreach (PropertyInfo property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                         .OrderBy(p => p.Name))
            {
                // Check to see if we need to ignore this property
                if (property.GetCustomAttribute<JsonIgnoreAttribute>() is not null) continue;

                // Get the property for a specific output name, otherwise use the property name
                NuGetPublishOutputAttribute attribute =
                    property.GetCustomAttribute<NuGetPublishOutputAttribute>() ?? new(property.Name);

                // Get the value of the property from the current instance
                string value = property.GetValue(this)?.ToString()?.Trim() ?? string.Empty;

                // Write the output to the console
                Console.WriteLine($"{attribute.Name}={value}\n");
            }

            // We're done
            return;
        }

        // Create an XML serializer to serialize the output
        XmlSerializer serializer = new(typeof(NuGetPublishOutputModel));

        // Create a memory stream to write the output to
        await using MemoryStream memoryStream = new();

        // Create a writer to write the output to
        await using XmlTextWriter writer = new(memoryStream, Encoding.UTF8);

        // Set the formatting (we want it pretty)
        writer.Formatting = Formatting.Indented;

        // Serialize the output into XML
        serializer.Serialize(writer, this);

        // Write the output to the console
        Console.WriteLine(Encoding.UTF8.GetString(memoryStream.ToArray()));
    }
}
