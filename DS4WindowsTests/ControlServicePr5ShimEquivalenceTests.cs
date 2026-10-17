using System;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step2-5 (PR-5): 入力ループ（On_Report）の最頻度経路で参照するために追加・利用するシムが、
    /// Global（m_Config / BackingStore）と完全に同一の実体・同一の値を返すことを検証する。
    /// 各テストは変更した Global 状態を必ず元に戻す。
    /// </summary>
    public class ControlServicePr5ShimEquivalenceTests
    {
        private readonly AppSettingsService _appSettings = TestRepositoryFactory.CreateAppSettingsService();
        private readonly AppearanceSettingsService _appearance = TestRepositoryFactory.CreateAppearanceSettingsService();
        private readonly ProfileSettingsService _profileSettings = TestRepositoryFactory.CreateProfileSettingsService();
        private readonly ProfileActionProvider _actionProvider = TestRepositoryFactory.CreateProfileActionProvider();

        [Fact]
        public void UseUdpServerSmoothing_SharesStateWithGlobal()
        {
            bool original = Global.store.useUdpSmoothing;
            try
            {
                Global.store.useUdpSmoothing = !original;
                Assert.Equal(!original, _appSettings.UseUdpServerSmoothing);

                _appSettings.UseUdpServerSmoothing = original;
                Assert.Equal(original, Global.store.useUdpSmoothing);
            }
            finally
            {
                Global.store.useUdpSmoothing = original;
            }
        }

        [Fact]
        public void FlashWhenLateAt_SharesStateWithGlobal()
        {
            int original = Global.store.flashWhenLateAt;
            try
            {
                Global.store.flashWhenLateAt = original + 7;
                Assert.Equal(original + 7, _appSettings.FlashWhenLateAt);

                _appSettings.FlashWhenLateAt = original + 3;
                Assert.Equal(original + 3, Global.store.flashWhenLateAt);
            }
            finally
            {
                Global.store.flashWhenLateAt = original;
            }
        }

        [Fact]
        public void InvokeBatteryChanged_RaisesGlobalBatteryChangedWithSameSenderAndValue()
        {
            object receivedSender = null;
            byte receivedValue = 0;
            int fired = 0;
            EventHandler<byte> handler = (sender, value) =>
            {
                fired++;
                receivedSender = sender;
                receivedValue = value;
            };
            Global.BatteryChanged += handler;
            try
            {
                _appearance.InvokeBatteryChanged(42);

                Assert.Equal(1, fired);
                Assert.Same(typeof(Global), receivedSender);
                Assert.Equal((byte)42, receivedValue);
            }
            finally
            {
                Global.BatteryChanged -= handler;
            }
        }

        [Fact]
        public void UseIconChoice_SharesStateWithGlobal()
        {
            TrayIconChoice original = Global.store.useIconChoice;
            TrayIconChoice other = original == TrayIconChoice.Battery ? TrayIconChoice.Default : TrayIconChoice.Battery;
            try
            {
                Global.store.useIconChoice = other;
                Assert.Equal(other, _appearance.UseIconChoice);
            }
            finally
            {
                Global.store.useIconChoice = original;
            }
        }

        [Fact]
        public void ContainsCustomAction_And_Extras_ShareStateWithGlobal()
        {
            const int slot = 0;
            bool originalAction = Global.store.containsCustomAction[slot];
            bool originalExtras = Global.store.containsCustomExtras[slot];
            try
            {
                Global.store.containsCustomAction[slot] = true;
                Global.store.containsCustomExtras[slot] = false;
                Assert.True(_profileSettings.ContainsCustomAction(slot));
                Assert.False(_profileSettings.ContainsCustomExtras(slot));

                Global.store.containsCustomAction[slot] = false;
                Global.store.containsCustomExtras[slot] = true;
                Assert.False(_profileSettings.ContainsCustomAction(slot));
                Assert.True(_profileSettings.ContainsCustomExtras(slot));
            }
            finally
            {
                Global.store.containsCustomAction[slot] = originalAction;
                Global.store.containsCustomExtras[slot] = originalExtras;
            }
        }

        [Fact]
        public void GetProfileActionCount_SharesStateWithGlobal()
        {
            const int slot = 0;
            int original = Global.store.profileActionCount[slot];
            try
            {
                Global.store.profileActionCount[slot] = original + 5;

                Assert.Equal(original + 5, _actionProvider.GetProfileActionCount(slot));
            }
            finally
            {
                Global.store.profileActionCount[slot] = original;
            }
        }

        /// <summary>
        /// Global.ProfileSettingsServiceInstance を指定のサービスに配線して body を実行し、終了後に元へ戻す。
        /// 他のテスト（ProfileSettingsServiceTests など）が Global.ProfileSettingsServiceInstance を差し替えたまま
        /// 戻さないことがあり、テストの実行順序によって Global の参照先が変わるため、本テストでは
        /// 本番と同じ配線（Global が DI の Singleton を参照する状態）を明示的に作ってから検証する。
        /// </summary>
        private static void WithGlobalWiredTo(IProfileSettingsService service, Action body)
        {
            IProfileSettingsService original = Global.ProfileSettingsServiceInstance;
            try
            {
                Global.ProfileSettingsServiceInstance = service;
                body();
            }
            finally
            {
                Global.ProfileSettingsServiceInstance = original;
            }
        }

        [Fact]
        public void TouchpadActiveArray_OfDiSingleton_ReflectsWrites()
        {
            // TouchpadActiveArray はサービスのインスタンスが保持する状態（BackingStore ではない）。
            // TestRepositoryFactory.CreateProfileSettingsService() で作った別インスタンスではなく、ControlService が実際に受け取る
            // DI の Singleton を対象にする。本番では Global.ProfileSettingsServiceInstance も同じ Singleton を返すため、
            // CheckForTouchToggle／StartTPOff が書き換える配列と、Global.TouchActive／touchpadActive の読み取りは同一になる。
            IProfileSettingsService service = DS4WinWPF.AppHost.GetService<IProfileSettingsService>();

            WithGlobalWiredTo(service, () =>
            {
                Assert.Same(Global.ProfileSettingsServiceInstance, service);

                const int slot = 0;
                bool original = service.TouchpadActiveArray[slot];
                try
                {
                    service.TouchpadActiveArray[slot] = !original;
                    Assert.Equal(!original, service.GetTouchpadActive(slot));
                }
                finally
                {
                    service.TouchpadActiveArray[slot] = original;
                }
            });
        }

        [Fact]
        public void EnableTouchToggle_And_RumbleBoost_GettersMatchArrays()
        {
            // Phase6-Step12 Wave 2: 旧シム（Global.getEnableTouchToggle／getRumbleBoost ほか）を削除したため、
            // サービスのメソッドと配列が一致することを確認する。
            const int slot = 0;

            Assert.Equal(_profileSettings.EnableTouchToggle[slot], _profileSettings.GetEnableTouchToggle(slot));
            Assert.Equal(_profileSettings.RumbleBoost[slot], _profileSettings.GetRumbleBoost(slot));
        }
    }
}