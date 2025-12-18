namespace DS4Windows
{
    // Marker interface for runtime instances that expose a stable InstanceId for logging/diagnostics
    public interface IInstanceIdentifiable
    {
        int InstanceId { get; }
    }
}
