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
        void ClearToggledOnForKey(ushort key);
        void ClearAllToggledOn();
        void ClearAllEntries();
        void ClearDeviceState(int device);
        // Dispatch variants return true if an Action instance was invoked to handle the trigger
        bool DispatchTriggerEstablished(SpecialAction action, int device, ushort logicalValue, uint nativeValue, bool useScanCode, VirtualKBMBase outputKBMHandler);
        bool DispatchTriggerReleased(SpecialAction action, int device, ushort logicalValue, uint nativeValue, bool useScanCode, VirtualKBMBase outputKBMHandler);
        // Set the toggled-on flag for given action/device and notify listeners.
        void SetToggledOn(SpecialAction action, int device, bool value);
    }
}
