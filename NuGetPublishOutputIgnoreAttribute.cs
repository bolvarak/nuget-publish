// Defines our namespace
namespace NuGet.Publish;

/// <summary>
///     This class represents the Command-Line-Interface outputs to be ignored by this application
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class NuGetPublishOutputIgnoreAttribute : Attribute
{
}
