using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step9b-2（F02・F07）の回帰テスト。
    /// F02: 2026-09-04（31dddc43）で IManagedActionManager／IControllerRegistry の登録が巻き込み削除され、
    ///      ActionManager.SetToggledOn が例外になり（呼び出し元が握りつぶす）IsToggledOn が true にならなかった。
    /// F07: ServiceProviderHolder.GetRequiredService は、未登録のとき null ではなく例外を投げる。
    /// </summary>
    // ServiceProviderHolder（静的）を差し替えるテストは同じコレクションで直列に実行する
    [Collection("ServiceProviderHolder")]
    public class ManagedActionManagerRegistrationTests
    {
        [Fact]
        public void ServiceRegistration_RegistersManagedActionManagerAndControllerRegistry_AsSingletons()
        {
            DS4WinWPF.AppHost.CreateHost();

            var manager1 = DS4WinWPF.AppHost.GetService<IManagedActionManager>();
            var manager2 = DS4WinWPF.AppHost.GetService<IManagedActionManager>();
            Assert.NotNull(manager1);
            Assert.IsType<DefaultActionManager>(manager1);
            Assert.Same(manager1, manager2);

            var registry1 = DS4WinWPF.AppHost.GetService<IControllerRegistry>();
            var registry2 = DS4WinWPF.AppHost.GetService<IControllerRegistry>();
            Assert.NotNull(registry1);
            Assert.IsType<DefaultControllerRegistry>(registry1);
            Assert.Same(registry1, registry2);
        }

        [Fact]
        public void ActionManager_SetToggledOn_WithRegisteredManager_SetsStateAndDoesNotThrow()
        {
            // 静的な ServiceProviderHolder は、ほかのテストが null に戻すことがあるため、
            // このテスト自身が本番と同じ登録（IManagedActionManager）のコンテナを設定する。
            var previous = ServiceProviderHolder.Provider;
            var action = new SpecialAction("Step9b2ToggleTest", "Cross", "Key", "Key", 0);
            const int device = 0;

            try
            {
                var services = new ServiceCollection();
                services.AddSingleton<IActionFactory>(TestRepositoryFactory.CreateDefaultActionFactory()); // Phase6-Step10-4-4: 工場は部品（マクロ再生役ほか）を必須とする
                // Phase7-Step8 W2: DefaultActionManager は Func<IMapping>（切断時の後始末）を必須とする
                services.AddSingleton<Func<IMapping>>(_ => TestRepositoryFactory.MappingProvider);
                services.AddSingleton<IManagedActionManager, DefaultActionManager>();
                ServiceProviderHolder.SetProvider(services.BuildServiceProvider());

                ActionManager.SetToggledOn(action, device, true);
                Assert.True(ActionManager.GetStateFor(action, device).IsToggledOn);

                ActionManager.SetToggledOn(action, device, false);
                Assert.False(ActionManager.GetStateFor(action, device).IsToggledOn);
            }
            finally
            {
                try { ActionManager.SetToggledOn(action, device, false); } catch { }
                ServiceProviderHolder.SetProvider(previous);
            }
        }

        /// <summary>
        /// Controls タブのキー割り当て（Toggle）は、合成 SpecialAction（Mapping.GetOrCreateSyntheticKeyAction）に変換され、
        /// SpecialActions タブと同じ ActionManager.DispatchTriggerEdge → KeyAction → KeyButtonActionController(Toggle)
        /// の経路で処理される。その経路で IsToggledOn が「1 回目の押下でオン、解放では維持、2 回目の押下でオフ」と
        /// 遷移することを固定する（登録欠落時は SetToggledOn が例外になり、常に false のままだった）。
        /// </summary>
        [Fact]
        public void ControlsTab_SyntheticToggleKey_TogglesIsToggledOn_ThroughActionManager()
        {
            var previous = ServiceProviderHolder.Provider;
            const int device = 0;
            SpecialAction sa = null;
            try
            {
                var services = new ServiceCollection();
                services.AddSingleton<IActionFactory>(TestRepositoryFactory.CreateDefaultActionFactory()); // Phase6-Step10-4-4: 工場は部品（マクロ再生役ほか）を必須とする
                // Phase7-Step8 W2: DefaultActionManager は Func<IMapping>（切断時の後始末）を必須とする。合成アクションのキャッシュは Mapping の instance が持つ
                var mapper = TestMappingFactory.Create();
                services.AddSingleton<Func<IMapping>>(_ => () => mapper);
                services.AddSingleton<IManagedActionManager, DefaultActionManager>();
                services.AddSingleton<IControllerRegistry, DefaultControllerRegistry>();
                // Phase7-Step8 W2: KeyAction は ActionManager 経由で IKeyButtonControllerRegistry を解決する
                services.AddSingleton<IKeyButtonActionControllerFactory, DefaultKeyButtonActionControllerFactory>();
                services.AddSingleton<IKeyButtonControllerRegistry, DS4Windows.Services.KeyButtonControllerRegistry>();
                ServiceProviderHolder.SetProvider(services.BuildServiceProvider());

                mapper.ClearSyntheticActionCache();
                // 一意の control 値（ほかのテストの合成アクションとキャッシュ・状態を共有しないため）
                sa = mapper.GetOrCreateSyntheticKeyAction(device, 91, 0x41, toggle: true, useScan: false);
                Assert.True(sa.keyType.HasFlag(DS4KeyType.Toggle));

                var kbm = new MockVirtualKBM();

                bool Edge(bool established) => ActionManager.DispatchTriggerEdge(new TriggerContext
                {
                    ActionDef = sa,
                    Device = device,
                    LogicalValue = 0x41,
                    NativeValue = 0x41,
                    UseScanCode = false,
                    OutputHandler = kbm,
                    IsEstablished = established
                });

                // 1 回目の押下 → トグルオン
                Assert.True(Edge(true));
                Assert.True(ActionManager.GetStateFor(sa, device).IsToggledOn);

                // 解放 → トグルは維持される
                Edge(false);
                Assert.True(ActionManager.GetStateFor(sa, device).IsToggledOn);

                // 2 回目の押下 → トグルオフ
                Edge(true);
                Assert.False(ActionManager.GetStateFor(sa, device).IsToggledOn);
            }
            finally
            {
                try { if (sa != null) ActionManager.SetToggledOn(sa, device, false); } catch { }
                try { ActionManager.ClearDeviceState(device); } catch { }
                ServiceProviderHolder.SetProvider(previous);
            }
        }

        [Fact]
        public void GetRequiredService_RegisteredService_ReturnsIt()
        {
            var previous = ServiceProviderHolder.Provider;
            try
            {
                var services = new ServiceCollection();
                var marker = new Marker();
                services.AddSingleton(marker);
                ServiceProviderHolder.SetProvider(services.BuildServiceProvider());

                Assert.Same(marker, ServiceProviderHolder.GetRequiredService<Marker>());
            }
            finally
            {
                ServiceProviderHolder.SetProvider(previous);
            }
        }

        [Fact]
        public void GetRequiredService_UnregisteredService_Throws()
        {
            var previous = ServiceProviderHolder.Provider;
            try
            {
                ServiceProviderHolder.SetProvider(new ServiceCollection().BuildServiceProvider());

                Assert.Throws<InvalidOperationException>(() => ServiceProviderHolder.GetRequiredService<Marker>());
            }
            finally
            {
                ServiceProviderHolder.SetProvider(previous);
            }
        }

        [Fact]
        public void GetRequiredService_ProviderNotSet_Throws()
        {
            var previous = ServiceProviderHolder.Provider;
            try
            {
                ServiceProviderHolder.SetProvider(null);

                Assert.Throws<InvalidOperationException>(() => ServiceProviderHolder.GetRequiredService<Marker>());
            }
            finally
            {
                ServiceProviderHolder.SetProvider(previous);
            }
        }

        private sealed class Marker { }
    }
}
