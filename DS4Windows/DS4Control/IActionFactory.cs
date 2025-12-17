using DS4Windows;

namespace DS4Windows.Actions
{
    public interface IActionFactory
    {
        Action CreateFrom(SpecialAction sa, int index);
    }
}
