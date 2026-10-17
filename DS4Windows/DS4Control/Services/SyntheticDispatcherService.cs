using System;
using System.Collections.Generic;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase7-Step8 W3（Q6＝A）: <see cref="ISyntheticDispatcher"/> の実装。旧 静的クラス <c>SyntheticDispatcher</c> の処理を、
    /// ログ・例外の握りつぶし・状態の更新の順序まで変えずに移した。旧: 合成状態は <c>SyntheticInputState.Shared</c>、出力先の既定は
    /// <c>Global.outputKBMHandler</c>、キー変換は <c>Global.outputKBMMapping</c> だった。コンストラクターで受ける。
    /// 出力先の既定（<paramref name="handlerOwner"/>）は、ハンドラが未決定（<c>Handler == null</c>）のとき、従来どおり何もせず戻る。
    /// </summary>
    public class SyntheticDispatcherService : ISyntheticDispatcher
    {
        private readonly SyntheticInputState _syntheticInput;
        private readonly OutputKBMHandlerAdapter _handlerOwner;
        private readonly IProfileSettingsService _profileSettings;

        public SyntheticDispatcherService(SyntheticInputState syntheticInput, OutputKBMHandlerAdapter handlerOwner,
            IProfileSettingsService profileSettings)
        {
            _syntheticInput = syntheticInput ?? throw new ArgumentNullException(nameof(syntheticInput));
            _handlerOwner = handlerOwner ?? throw new ArgumentNullException(nameof(handlerOwner));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
        }

        public uint ResolveNativeKey(ushort kvpKey)
        {
            try
            {
                var mapping = _profileSettings.OutputKBMMapping;
                if (mapping != null)
                    return mapping.GetRealEventKey(kvpKey);
            }
            catch { }
            return kvpKey;
        }

        // 旧: var h = handler ?? Global.outputKBMHandler; if (h == null) return;
        private IVirtualKBM ResolveHandler(IVirtualKBM handler)
        {
            if (handler != null) return handler;
            return _handlerOwner.Handler != null ? _handlerOwner : null;
        }

        public void SendPress(int device, ushort kvpKey, uint nativeKey, bool useScanCode, IVirtualKBM handler = null)
        {
            try
            {
                var h = ResolveHandler(handler);
                if (h == null) return;

                AppLogger.LogTrace($"SYNTHETIC TRACE dispatcher device={device} key={kvpKey} nativeKey={nativeKey} event=KeyPress");
                AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] dispatcher device={device} key={kvpKey} nativeKey={nativeKey} event=KeyPress");

                if (useScanCode) h.PerformKeyPressAlt(nativeKey); else h.PerformKeyPress(nativeKey);

                // Update simple timing counters to help throttle logic elsewhere
                try
                {
                    var now = DateTime.UtcNow.Ticks;
                    if (_syntheticInput.DeviceState != null && device >= 0 && device < _syntheticInput.DeviceState.Length)
                    {
                        var ds = _syntheticInput.DeviceState[device];
                        if (ds.keyPresses == null) ds.keyPresses = new Dictionary<UInt16, SyntheticState.KeyPresses>();
                        if (!ds.keyPresses.TryGetValue(kvpKey, out var kp))
                        {
                            kp = new SyntheticState.KeyPresses();
                            ds.keyPresses[kvpKey] = kp;
                            try { ds.nativeKeyAlias[kvpKey] = nativeKey; } catch { }
                        }
                        kp.current.lastSyntheticSendUtcTicks = now;
                        kp.current.repeatCount++;
                    }

                    if (_syntheticInput.GlobalState != null)
                    {
                        if (_syntheticInput.GlobalState.keyPresses == null) _syntheticInput.GlobalState.keyPresses = new Dictionary<UInt16, SyntheticState.KeyPresses>();
                        if (!_syntheticInput.GlobalState.keyPresses.TryGetValue(kvpKey, out var gkp))
                        {
                            gkp = new SyntheticState.KeyPresses();
                            _syntheticInput.GlobalState.keyPresses[kvpKey] = gkp;
                        }
                        gkp.current.lastSyntheticSendUtcTicks = DateTime.UtcNow.Ticks;
                        gkp.current.repeatCount++;
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                AppLogger.LogTrace($"SyntheticDispatcher.SendPress failed: {ex}");
            }
        }

        public void SendRelease(int device, ushort kvpKey, uint nativeKey, bool useScanCode, IVirtualKBM handler = null)
        {
            try
            {
                var h = ResolveHandler(handler);
                if (h == null) return;

                AppLogger.LogTrace($"SYNTHETIC TRACE dispatcher device={device} key={kvpKey} nativeKey={nativeKey} event=KeyRelease");
                AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] dispatcher device={device} key={kvpKey} nativeKey={nativeKey} event=KeyRelease");

                if (useScanCode) h.PerformKeyReleaseAlt(nativeKey); else h.PerformKeyRelease(nativeKey);

                try
                {
                    // Mark last send time and clear counts to avoid duplicates
                    var now = DateTime.UtcNow.Ticks;
                    if (_syntheticInput.DeviceState != null && device >= 0 && device < _syntheticInput.DeviceState.Length)
                    {
                        var ds = _syntheticInput.DeviceState[device];
                        if (ds.keyPresses != null && ds.keyPresses.TryGetValue(kvpKey, out var kp))
                        {
                            kp.current.lastSyntheticSendUtcTicks = now;
                            kp.current.vkCount = 0;
                            kp.current.scanCodeCount = 0;
                            kp.current.repeatCount = 0;
                            kp.current.toggleCount = 0;
                            kp.current.toggle = false;
                        }
                    }

                    if (_syntheticInput.GlobalState != null && _syntheticInput.GlobalState.keyPresses != null && _syntheticInput.GlobalState.keyPresses.TryGetValue(kvpKey, out var gkp))
                    {
                        gkp.current.lastSyntheticSendUtcTicks = now;
                        gkp.current.vkCount = 0;
                        gkp.current.scanCodeCount = 0;
                        gkp.current.repeatCount = 0;
                        gkp.current.toggleCount = 0;
                        gkp.current.toggle = false;
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                AppLogger.LogTrace($"SyntheticDispatcher.SendRelease failed: {ex}");
            }
        }

        public void ResetKeyTiming(int device, ushort kvpKey)
        {
            try
            {
                var now = DateTime.UtcNow.Ticks;
                if (_syntheticInput.DeviceState != null && device >= 0 && device < _syntheticInput.DeviceState.Length)
                {
                    var ds = _syntheticInput.DeviceState[device];
                    if (ds.keyPresses != null && ds.keyPresses.TryGetValue(kvpKey, out var kp))
                    {
                        kp.current.lastSyntheticSendUtcTicks = 0;
                        kp.current.repeatCount = 0;
                    }
                }
                if (_syntheticInput.GlobalState != null && _syntheticInput.GlobalState.keyPresses != null && _syntheticInput.GlobalState.keyPresses.TryGetValue(kvpKey, out var gkp))
                {
                    gkp.current.lastSyntheticSendUtcTicks = 0;
                    gkp.current.repeatCount = 0;
                }
            }
            catch { }
        }
    }
}
