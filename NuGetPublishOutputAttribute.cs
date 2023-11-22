// Defines our namespace
namespace NuGet.Publish;

/// <summary>
///     This class represents the Command-Line-Interface outputs available to this application.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class NuGetPublishOutputAttribute : Attribute
{
    /// <summary>
    ///     This property contains the name of the value to be used in the output.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    ///     This constructor creates a new instance of the NuGetPublishOutputAttribute
    ///     class with an output property <paramref name="name" />.
    /// </summary>
    /// <param name="name">The name of the value to be used in the output.</param>
    public NuGetPublishOutputAttribute(string name) => Name = name.Replace('-', '_').ToUpper().Trim();
}
