using DS4Windows;

namespace DS4Windows.DI
{
    public interface IProfileApplicationService
    {
        void ApplyFromAction(int deviceIndex, SpecialAction action);
        void RestoreFromAction(int deviceIndex);
    }
}
