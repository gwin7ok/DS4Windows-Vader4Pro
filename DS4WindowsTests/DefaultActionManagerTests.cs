using System;
using Xunit;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DS4Control;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    public class DefaultActionManagerTests
    {
        private class MockActionFactory : IActionFactory
        {
            public int CreateFromCalls { get; private set; }

            public DS4Windows.Actions.Action CreateFrom(SpecialAction action, int index = -1)
            {
                CreateFromCalls++;
                return null;
            }
        }

        [Fact]
        public void Constructor_WithMockActionFactory_InitializesSuccessfully()
        {
            var mockFactory = new MockActionFactory();
            var manager = new DefaultActionManager(mockFactory);

            Assert.NotNull(manager);
            Assert.NotEqual(0, manager.InstanceId);
        }

        [Fact]
        public void SetToggledOn_FiresInstanceToggledOnChangedEvent()
        {
            var manager = new DefaultActionManager();
            var action = new SpecialAction("ToggleTest", "Cross", "Key", "Key", 0);

            SpecialAction eventAction = null;
            int eventDevice = -1;
            bool eventOld = false;
            bool eventNew = false;
            bool fired = false;

            manager.ToggledOnChanged += (act, dev, oldV, newV) =>
            {
                fired = true;
                eventAction = act;
                eventDevice = dev;
                eventOld = oldV;
                eventNew = newV;
            };

            manager.SetToggledOn(action, 0, true);

            Assert.True(fired);
            Assert.Same(action, eventAction);
            Assert.Equal(0, eventDevice);
            Assert.False(eventOld);
            Assert.True(eventNew);

            // 同じ値を再設定した場合はイベントが発火しないこと
            fired = false;
            manager.SetToggledOn(action, 0, true);
            Assert.False(fired);

            // 値を解除した時の発火
            manager.SetToggledOn(action, 0, false);
            Assert.True(fired);
            Assert.True(eventOld);
            Assert.False(eventNew);
        }

        [Fact]
        public void ClearDeviceState_ResetsToggledOnStateAndFiresEvent()
        {
            var manager = new DefaultActionManager();
            var action = new SpecialAction("ClearTest", "Circle", "Key", "Key", 0);

            manager.SetToggledOn(action, 1, true);

            bool clearedFired = false;
            manager.ToggledOnChanged += (act, dev, oldV, newV) =>
            {
                if (dev == 1 && oldV == true && newV == false)
                {
                    clearedFired = true;
                }
            };

            manager.ClearDeviceState(1);

            Assert.True(clearedFired);
            var state = manager.GetStateFor(action, 1);
            Assert.NotNull(state);
            Assert.False(state.IsToggledOn);
        }

        [Fact]
        public void GetStateFor_NewAction_DefaultsRequiresFreshPressAfterResetTrue()
        {
            // Issue8-1是正(1)の回帰防止テスト。
            // ActionInstanceStateが新規生成された直後は、プロファイル適用直後の
            // 押しっぱなし誤検知を防ぐため、RequiresFreshPressAfterResetがtrueであること。
            var manager = new DefaultActionManager();
            var action = new SpecialAction("FreshPressTest", "Cross", "Key", "Key", 0);

            var state = manager.GetStateFor(action, 0);

            Assert.NotNull(state);
            Assert.True(state.RequiresFreshPressAfterReset);
            Assert.False(state.IsExecuting);
            Assert.False(state.PendingReExecutionRequested);
        }

        [Fact]
        public void ClearDeviceState_ResetsRequiresFreshPressAfterResetToTrue()
        {
            // Issue8-1是正(1)の回帰防止テスト。
            // 実機バグの直接原因は、Global.ApplyProfile経由でClearDeviceStateが呼ばれるたびに
            // ActionInstanceStateが無条件に再生成され、RequiresFreshPressAfterResetが
            // trueに戻ること自体は正しい（是正後の意図した挙動）。
            // 本テストは、いったんfalse（＝離す動作を観測済み）になった状態が、
            // ClearDeviceStateによって確実にtrue（＝要フレッシュプレス）へ戻ることを確認する。
            var manager = new DefaultActionManager();
            var action = new SpecialAction("FreshPressResetTest", "Circle", "Key", "Key", 0);

            var stateBefore = manager.GetStateFor(action, 2);
            stateBefore.RequiresFreshPressAfterReset = false; // 通常のマッピング評価で「未成立」を観測した状態を模擬

            manager.ClearDeviceState(2);

            var stateAfter = manager.GetStateFor(action, 2);
            Assert.True(stateAfter.RequiresFreshPressAfterReset);
        }

        [Fact]
        public void ClearAllEntries_NewStateForSameAction_RequiresFreshPressAfterResetTrue()
        {
            // Issue8-1是正(1)の回帰防止テスト。ClearAllEntries経由（Global.ApplyProfile等、
            // 複数の呼出元で使われる）でも同様にリセットされることを確認する。
            var manager = new DefaultActionManager();
            var action = new SpecialAction("ClearAllFreshPressTest", "Square", "Key", "Key", 0);

            var stateBefore = manager.GetStateFor(action, 0);
            stateBefore.RequiresFreshPressAfterReset = false;

            manager.ClearAllEntries();

            var stateAfter = manager.GetStateFor(action, 0);
            Assert.True(stateAfter.RequiresFreshPressAfterReset);
        }

        [Fact]
        public void IsExecuting_And_PendingReExecutionRequested_AreIndependentlySettable()
        {
            // Issue8-1是正(2)（仕様③厳格化・実行重複禁止）関連フィールドの基本動作確認。
            // 実際の「実行中は新規実行しない・完了後に1回だけ再実行する」というキュー挙動は
            // Mapping.cs側のトリガー評価ロジックに実装されているため、本テストでは
            // ActionInstanceState側のフィールドが期待通り独立して読み書きできることのみを確認する。
            var manager = new DefaultActionManager();
            var action = new SpecialAction("ExecutingFlagTest", "Triangle", "Key", "Key", 0);

            var state = manager.GetStateFor(action, 0);
            Assert.False(state.IsExecuting);
            Assert.False(state.PendingReExecutionRequested);

            state.IsExecuting = true;
            Assert.True(state.IsExecuting);
            Assert.False(state.PendingReExecutionRequested);

            state.PendingReExecutionRequested = true;
            Assert.True(state.IsExecuting);
            Assert.True(state.PendingReExecutionRequested);

            state.IsExecuting = false;
            state.PendingReExecutionRequested = false;
            Assert.False(state.IsExecuting);
            Assert.False(state.PendingReExecutionRequested);
        }

        [Fact]
        public void DefaultMacroPlayer_InitializesWithVirtualKBM_AndTracksState()
        {
            var mockKBM = new MockVirtualKBM();
            var player = new DefaultMacroPlayer(mockKBM);

            Assert.False(player.IsPlaying(0));
            Assert.False(player.IsPlaying(-1));
            Assert.False(player.IsPlaying(4));

            player.Stop(0);
            Assert.False(player.IsPlaying(0));
        }
    }
}