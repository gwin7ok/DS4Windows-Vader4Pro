using System;
using System.Collections.Generic;
using DS4Windows.Actions;
using DS4Windows.DS4Control;

namespace DS4Windows.Actions
{
    public interface IManagedActionManager
    {
        Actions.Action GetActionByIndex(int index);
        Actions.Action GetActionByName(string name);
        IReadOnlyList<Actions.Action> Actions { get; }
        ActionInstanceState GetStateFor(SpecialAction action, int device);
        void ClearPressedOnceForKey(ushort key);
        void ClearAllPressedOnce();
        void ClearAllEntries();
        void ClearDeviceState(int device);
        void NotifyTriggerEstablished(SpecialAction action, int device, ushort logicalValue, uint nativeValue, bool useScanCode, VirtualKBMBase outputKBMHandler);
        void NotifyTriggerReleased(SpecialAction action, int device, ushort logicalValue, uint nativeValue, bool useScanCode, VirtualKBMBase outputKBMHandler);
    }
}
