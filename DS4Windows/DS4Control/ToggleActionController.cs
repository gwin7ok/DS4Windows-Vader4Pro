// ToggleActionController removed — replaced by per-device `KeyButtonActionController` implementations.
// This file is retained as a harmless informational stub to ease transition.
using System;

namespace DS4Windows
{
    [Obsolete("ToggleActionController removed. Use per-device KeyButtonActionController instead.")]
    public static class ToggleActionController
    {
        public static void OnToggleOn(int device, ushort kvpKey, uint nativeKey, bool useScanCode, object handler) { }
        public static void OnToggleOff(int device, ushort kvpKey, uint nativeKey, bool useScanCode, object handler) { }
        public static void ClearKeyEntries(ushort kvpKey) { }
        public static void SetActive(int device, bool active) { }
        public static bool IsActive(int device) => false;
        public static void Update() { }
    }
}
