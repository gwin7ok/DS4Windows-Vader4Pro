using System;

namespace DS4Windows.Actions
{
    public interface IKeyButtonActionControllerFactory
    {
        DS4Windows.KeyButtonActionController Create(int device, DS4Windows.KeyButtonActionController.Mode mode, string actionName = null);
        DS4Windows.KeyButtonActionController Create(int device, DS4Windows.SpecialAction sa, string actionName = null);
    }
}
