using System;

namespace DS4Windows.Actions
{
    public class DefaultKeyButtonActionControllerFactory : IKeyButtonActionControllerFactory
    {
        public DS4Windows.KeyButtonActionController Create(int device, DS4Windows.KeyButtonActionController.Mode mode, string actionName = null)
        {
            return new DS4Windows.KeyButtonActionController(device, mode, actionName);
        }

        public DS4Windows.KeyButtonActionController Create(int device, DS4Windows.SpecialAction sa, string actionName = null)
        {
            return new DS4Windows.KeyButtonActionController(device, sa, actionName);
        }
    }
}
