namespace DS4Windows.Actions
{
    /// <summary>
    /// Low-level output handler abstraction. Implementations wrap VirtualKBMBase.
    /// </summary>
    public interface IOutputHandler
    {
        bool FakeKeyRepeat { get; }
        void PerformKeyPress(uint vk);
        void PerformKeyRelease(uint vk);
    }
}
