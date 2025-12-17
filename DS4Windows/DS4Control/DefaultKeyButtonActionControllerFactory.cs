using System;

namespace DS4Windows.Actions
{
    public class DefaultKeyButtonActionControllerFactory : IKeyButtonActionControllerFactory
    {
        public DS4Windows.KeyButtonActionController Create(int device, DS4Windows.KeyButtonActionController.Mode mode, string actionName = null)
        {
            var inst = new DS4Windows.KeyButtonActionController(device, mode, actionName);
            try
            {
                var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                if (sp != null)
                {
                    var reg = sp.GetService(typeof(DS4Windows.Actions.IControllerRegistry)) as DS4Windows.Actions.IControllerRegistry;
                    if (reg != null)
                    {
                        var key = device + ":" + (string.IsNullOrEmpty(actionName) ? "<default>" : actionName);
                        reg.Register(key, inst);
                    }
                }
            }
            catch { }
            return inst;
        }

        public DS4Windows.KeyButtonActionController Create(int device, DS4Windows.SpecialAction sa, string actionName = null)
        {
            var inst = new DS4Windows.KeyButtonActionController(device, sa, actionName);
            try
            {
                var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                if (sp != null)
                {
                    var reg = sp.GetService(typeof(DS4Windows.Actions.IControllerRegistry)) as DS4Windows.Actions.IControllerRegistry;
                    if (reg != null)
                    {
                        var name = actionName ?? (sa?.name ?? "<sa>");
                        var key = device + ":" + (string.IsNullOrEmpty(name) ? "<default>" : name);
                        reg.Register(key, inst);
                    }
                }
            }
            catch { }
            return inst;
        }
    }
}
