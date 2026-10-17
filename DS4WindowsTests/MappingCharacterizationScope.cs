using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.DS4Control;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step7 W1: <c>Mapping</c>（静的）の特性化テストの共通の土台。
    /// - 専用のデバイス添字（<see cref="Device"/>）を使い、他のテストと状態を共有しない。
    /// - <c>Mapping.globalState</c>／<c>deviceState</c> を新品へ差し替え、<c>Global.outputKBMHandler</c> へ記録用のスタブを差し、
    ///   KBM マッピングの定数を初期化する。終了時にすべて元へ戻す。
    /// - 設定は <c>Global.store</c>（コンテナの <c>IProfileSettingsService</c> と、<c>Mapping</c> の静的な束縛の両方が指す実体）へ書き、
    ///   終了時に触ったボタンを <c>Reset()</c> する。
    /// </summary>
    public sealed class MappingCharacterizationScope : IDisposable
    {
        public const int Device = 5;

        private readonly SyntheticState _savedGlobalState;
        private readonly SyntheticState _savedDeviceState;
        private readonly DS4Windows.DS4Control.VirtualKBMBase _savedHandler;
        private readonly DS4Windows.Services.OutputKBMHandlerAdapter _handlerOwner;
        private readonly VirtualKBMMapping _savedMapping;
        private readonly List<DS4Controls> _touched = new List<DS4Controls>();

        public RecordingVirtualKbmBase Recorder { get; } = new RecordingVirtualKbmBase();
        public IProfileSettingsService Settings { get; }
        public ControlService Control { get; }
        /// <summary>コンテナの <c>IMapping</c>（Singleton）。Phase7-Step8 W2 で static から instance になった。</summary>
        public IMapping Mapper { get; }
        /// <summary>コンテナの合成キーの送出（Mapper が使うものと同じ instance）。</summary>
        public ISyntheticDispatcher Dispatcher { get; }
        /// <summary>コンテナの保持クラス（Mapper が使うものと同じ instance）。</summary>
        public SyntheticInputState Synthetic { get; }
        private readonly StickMotionState _stickMotion;
        public VirtualKBMMapping KbmMapping { get; }

        public MappingCharacterizationScope()
        {
            DS4WinWPF.AppHost.CreateHost();
            Settings = DS4WinWPF.AppHost.GetService<IProfileSettingsService>();
            Control = DS4WinWPF.AppHost.GetService<ControlService>();
            Mapper = DS4WinWPF.AppHost.GetService<IMapping>();
            Dispatcher = DS4WinWPF.AppHost.GetService<ISyntheticDispatcher>();
            _handlerOwner = DS4WinWPF.AppHost.GetService<DS4Windows.Services.OutputKBMHandlerAdapter>();
            Synthetic = DS4WinWPF.AppHost.GetService<SyntheticInputState>();
            _stickMotion = DS4WinWPF.AppHost.GetService<StickMotionState>();

            _savedGlobalState = Synthetic.GlobalState;
            _savedDeviceState = Synthetic.DeviceState[Device];
            _savedHandler = _handlerOwner.Handler; // Phase7-Step8 W3: 旧 Global.outputKBMHandler
            _savedMapping = Settings.OutputKBMMapping;

            Synthetic.GlobalState = new SyntheticState();
            Synthetic.DeviceState[Device] = new SyntheticState();

            KbmMapping = VirtualKBMFactory.GetMappingInstance();
            KbmMapping.PopulateConstants();
            KbmMapping.PopulateMappings();
            Settings.OutputKBMMapping = KbmMapping;
            _handlerOwner.Handler = Recorder;
        }

        /// <summary>デバイス <see cref="Device"/> の、指定のボタンの割り当てを「ボタン」へ変える（終了時に既定へ戻す）。</summary>
        public void RemapToButton(DS4Controls control, X360Controls target)
        {
            DS4ControlSettings dcs = Global.store.GetDS4CSetting(Device, control);
            dcs.Reset();
            dcs.actionType = DS4ControlSettings.ActionType.Button;
            dcs.action.actionBtn = target;
            _touched.Add(control);
        }

        public static DS4State NeutralState()
        {
            return new DS4State { LX = 128, LY = 128, RX = 128, RY = 128, L2 = 0, R2 = 0 };
        }

        /// <summary>実機のレポート処理と同様に <c>MapCustom</c> を 1 回実行し、出力の状態を返す。</summary>
        public DS4State RunMapCustom(DS4State input)
        {
            var device = (DS4Device)RuntimeHelpers.GetUninitializedObject(typeof(DS4Device));
            var mouse = new Mouse(Device, device, Settings, Recorder, _stickMotion, Synthetic);
            var exposed = new DS4StateExposed(input);
            var mapped = new DS4State();

            Mapper.MapCustom(Device, input, mapped, exposed, mouse, Control);
            return mapped;
        }

        public void Dispose()
        {
            foreach (DS4Controls control in _touched)
                Global.store.GetDS4CSetting(Device, control)?.Reset();

            Mapper.HandleDeviceDisconnect(Device);
            _handlerOwner.Handler = _savedHandler;
            Settings.OutputKBMMapping = _savedMapping;
            Synthetic.GlobalState = _savedGlobalState;
            Synthetic.DeviceState[Device] = _savedDeviceState;
        }
    }
}
