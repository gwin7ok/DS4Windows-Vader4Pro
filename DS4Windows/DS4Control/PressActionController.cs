// PressActionController removed — replaced by per-device `KeyButtonActionController` implementations.
// This file is retained as a harmless informational stub to ease transition.
using System;

namespace DS4Windows
{
    [Obsolete("PressActionController removed. Use per-device KeyButtonActionController instead.")]
    public static class PressActionController
    {
        public static void OnPressDown(int device, ushort kvpKey, uint nativeKey, bool useScanCode, object handler, bool enableRepeat = false) { }
        public static void OnPressUp(int device, ushort kvpKey, uint nativeKey, bool useScanCode, object handler) { }
        public static void Update() { }
        public static void ClearKeyEntries(ushort kvpKey) { }
        public static void SetActive(int device, bool active) { }
        public static bool IsActive(int device) => false;
    }
}
