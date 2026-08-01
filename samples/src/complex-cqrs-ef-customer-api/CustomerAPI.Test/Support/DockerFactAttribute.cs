namespace CustomerAPI.Test.Support;

/// <summary>
/// Marks an integration test that requires Docker. Skips when Docker is unavailable.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class DockerFactAttribute : FactAttribute
{
    public DockerFactAttribute()
    {
        if (!DockerAvailability.IsAvailable)
        {
            Skip = DockerAvailability.SkipReason;
        }
    }
}
