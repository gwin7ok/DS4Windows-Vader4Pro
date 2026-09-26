namespace DS4Windows.Services
{
    public interface IDeviceStateAccessor
    {
        DS4Device GetController(int deviceIndex);
    }
}