using System;
using DS4Windows.DI;
using DS4WinWPF.DS4Control;

namespace DS4Windows.Services
{
    public class OutputSlotStore : IOutputSlotStore
    {
        private readonly IPathService _pathService;

        public OutputSlotStore(IPathService pathService = null)
        {
            _pathService = pathService ?? DS4WinWPF.AppHost.GetService<IPathService>() ?? new PathService();
        }

        public bool Load(OutputSlotManager slotManager)
        {
            if (slotManager == null) return false;
            try
            {
                bool result = OutputSlotPersist.ReadConfig(slotManager);
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace($"[DI] OutputSlotStore.Load: result={result}");
                return result;
            }
            catch (Exception ex)
            {
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace($"[DI] OutputSlotStore.Load failed: {ex}");
                return false;
            }
        }

        public bool Save(OutputSlotManager slotManager)
        {
            if (slotManager == null) return false;
            try
            {
                bool result = OutputSlotPersist.WriteConfig(slotManager);
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace($"[DI] OutputSlotStore.Save: result={result}");
                return result;
            }
            catch (Exception ex)
            {
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace($"[DI] OutputSlotStore.Save failed: {ex}");
                return false;
            }
        }
    }
}
