using DS4Windows.Services;
using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using DS4Windows.DS4Control;
using DS4Windows.Actions;

namespace DS4Windows
{
    // Per-action per-device instance state
    public class ActionInstanceState
    {
        public bool IsToggledOn = false;
        public long LastToggleTimeUtcTicks = 0;
        public bool FirstTouch = false;
        // Whether a macro iteration is currently executing on this device.
        // This flag indicates a single macro run (one iteration) is in progress.
        // Do not use it to represent the lifetime of a repeat-in-place task.
        public bool IsMacroRunning = false;
        // Whether the action is currently considered 'being triggered' (replaces legacy actionDone)
        // Exposed as a read-only property to prevent direct assignment from other code.
        private bool _beingTriggered = false;
        public bool BeingTriggered { get { return _beingTriggered; } }

        // Internal setter used by ActionManager to mutate the state in a controlled way.
        internal void SetBeingTriggeredInternal(bool value)
        {
            _beingTriggered = value;
        }

        // Index of an expected untrigger action for this state (-1 if none). Mirrors Mapping.untriggerindex[device] semantics
        public int UntriggerIndex = -1;

        // Bitmask for one-shot flags (GyroCalibrate, BatteryCheck, etc.) so we can record multiple one-shot states per action/device.
        // Interpretation of bits is up to higher-level managers; using uint for compactness.
        public uint OneShotFlags = 0u;

        // Issue8-1是正(1): プロファイル適用後は、以後発生した成立イベントのみを成立とみなす（仕様④）。
        // ActionInstanceStateが新規生成された直後はtrue。以後、当該アクションのトリガーが
        // 連続して指定時間（FreshPressReleaseHoldMs）「未成立」状態として観測されるまでは、
        // たとえ全ボタン押下状態が検出されても新規成立とみなさない。
        public bool RequiresFreshPressAfterReset = true;

        // A案是正: 未成立状態が開始された時刻（Environment.TickCount64）。
        // 過渡的な0クリアによる瞬間的OFFで即座にガード解除されるのを防ぐデバウンスタイマー。
        public long UntriggeredTimestampTicks = 0;

        // ガード解除に必要なOFF継続時間（ミリ秒）
        public const int FreshPressReleaseHoldMs = 80;

        // Issue8-1是正(2): 同一アクションの実行重複禁止（仕様③の厳格化）。
        // Macro型はIsMacroRunningで既に管理されているため、それ以外の種別
        // （Profile/Program/GyroCalibrate等）向けに新設する。
        public bool IsExecuting = false;

        // 実行中に新たな成立イベントが発生した場合、完了後にもう一度だけ実行してほしいという
        // 要求を記録する（実行キュー方式・キュー深さ1。無制限キューによる際限のない滞留を防ぐため）。
        public bool PendingReExecutionRequested = false;
    }
}