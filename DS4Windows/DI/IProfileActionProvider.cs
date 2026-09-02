using System.Collections.Generic;
using DS4Windows;

namespace DS4Windows.DI
{
    public interface IProfileActionProvider
    {
        IReadOnlyList<string> GetProfileActionNames(int deviceIndex);
        SpecialAction GetProfileAction(int deviceIndex, string actionName);
    }
}
