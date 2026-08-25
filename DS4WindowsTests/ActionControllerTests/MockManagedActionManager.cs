using System;
using System.Collections.Generic;
using DS4Windows.Actions;
using DS4Windows.DS4Control;

namespace DS4Windows.Actions.Tests
{
    /// <summary>
    /// IManagedActionManager の軽量モック（フェーズ0-4 雛形）。
    /// 実際のテストでの利用はフェーズ1以降で拡張予定。
    /// </summary>
    public class MockManagedActionManager : IManagedActionManager
    {
        public List<string> RegisteredActions { get; } = new List<string>();
        public IReadOnlyList<Action> Actions => new List<Action>();

        public Action GetActionByIndex(int index) => null;
        public Action GetActionByName(string name) => null;
        public ActionInstanceState GetStateFor(SpecialAction action, int device) => new ActionInstanceState();
        public void ClearToggledOnForKey(ushort key) { }
        public void ClearAllToggledOn() { }
        public void ClearAllEntries() { }
        public void ClearDeviceState(int device) { }
        public bool DispatchTriggerEstablished(SpecialAction action, int device, ushort logicalValue, uint nativeValue, bool useScanCode, VirtualKBMBase outputKBMHandler) => false;
        public bool DispatchTriggerReleased(SpecialAction action, int device, ushort logicalValue, uint nativeValue, bool useScanCode, VirtualKBMBase outputKBMHandler) => false;
        public void SetToggledOn(SpecialAction action, int device, bool value) { }

        public void RegisterAction(string actionName, IActionBinding binding)
        {
            RegisteredActions.Add(actionName);
        }

        public IActionBinding GetBinding(string actionName) => null;
        public IEnumerable<string> GetRegisteredActions() => RegisteredActions;
    }
}
