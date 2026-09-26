using DS4Windows;

namespace DS4Windows.DI
{
    public interface IProfileActionChainService
    {
        void DispatchNextActions(int deviceIndex, SpecialAction sourceAction);
    }
}
