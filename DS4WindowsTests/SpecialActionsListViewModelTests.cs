using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4WinWPF;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    public class SpecialActionsListViewModelTests
    {
        static SpecialActionsListViewModelTests()
        {
            if (string.IsNullOrEmpty(Global.appdatapath))
            {
                Global.appdatapath = AppContext.BaseDirectory;
            }
        }

        /// <summary>
        /// 対象オブジェクト（インスタンスのイベント）または型（静的イベント）が保持するデリゲートの購読者のうち、
        /// 指定の購読者オブジェクトを対象にしているものの件数を返す。
        /// イベントの購読は発行側（Singleton サービス）のデリゲートに保持されるため、購読者側のフィールドを
        /// 調べても検出できない。発行側のバッキングフィールドを調べる必要がある。
        /// </summary>
        private static int CountSubscriptions(object publisher, Type publisherType, object subscriber)
        {
            int count = 0;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            for (Type t = publisherType; t != null && t != typeof(object); t = t.BaseType)
            {
                foreach (FieldInfo f in t.GetFields(flags | BindingFlags.DeclaredOnly))
                {
                    if (!typeof(Delegate).IsAssignableFrom(f.FieldType)) continue;
                    object holder = f.IsStatic ? null : publisher;
                    if (!f.IsStatic && holder == null) continue;

                    var del = f.GetValue(holder) as Delegate;
                    if (del == null) continue;

                    foreach (Delegate d in del.GetInvocationList())
                    {
                        if (ReferenceEquals(d.Target, subscriber)) count++;
                    }
                }
            }

            return count;
        }

        [Fact]
        public void CountSubscriptions_DetectsSubscriptionHeldByThePublisher()
        {
            // 検出器自身の検証: 発行側（偽のサービス）に購読すると、購読者側のフィールドには何も現れないが、
            // 発行側のバッキングフィールドを調べれば検出できる。
            var publisher = new FakePublisher();
            var subscriber = new FakeSubscriber();
            Assert.Equal(0, CountSubscriptions(publisher, typeof(FakePublisher), subscriber));

            publisher.Changed += subscriber.OnChanged;
            Assert.Equal(1, CountSubscriptions(publisher, typeof(FakePublisher), subscriber));

            publisher.Changed -= subscriber.OnChanged;
            Assert.Equal(0, CountSubscriptions(publisher, typeof(FakePublisher), subscriber));
        }

        [Fact]
        public void SpecialActionsListViewModel_DoesNotSubscribeToSingletonServiceEvents()
        {
            // Watchpoint 2 監査テスト（動作による検証）:
            // SpecialActionsListViewModel が Singleton サービスのイベントを購読していないことを、
            // 発行側（サービス）のデリゲートの購読者を調べて証明する（Singleton からの強参照によるメモリリーク経路が無いこと）。
            DS4WinWPF.AppHost.CreateHost();

            var specialActionRepo = DS4WinWPF.AppHost.GetService<ISpecialActionRepository>();
            var profileRepo = DS4WinWPF.AppHost.GetService<IProfileRepository>();
            var outputSlotService = DS4WinWPF.AppHost.GetService<IOutputSlotService>();
            var profileSettings = DS4WinWPF.AppHost.GetService<IProfileSettingsService>();
            var actionManager = DS4WinWPF.AppHost.GetService<IManagedActionManager>();

            var vm = new SpecialActionsListViewModel(0, specialActionRepo, profileRepo);

            var publishers = new List<(object instance, Type type)>
            {
                (specialActionRepo, specialActionRepo.GetType()),
                (profileRepo, profileRepo.GetType()),
                (outputSlotService, outputSlotService.GetType()),
                (profileSettings, profileSettings.GetType()),
            };
            if (actionManager != null) publishers.Add((actionManager, actionManager.GetType()));

            foreach (var (instance, type) in publishers)
            {
                Assert.True(CountSubscriptions(instance, type, vm) == 0,
                    $"ViewModel が {type.Name} のイベントを購読している（Singleton からの強参照リーク）");
            }

            // 静的イベント（ActionManager.ToggledOnChanged など）も対象
            Assert.True(CountSubscriptions(null, typeof(ActionManager), vm) == 0,
                "ViewModel が ActionManager の静的イベントを購読している");
            Assert.True(CountSubscriptions(null, typeof(Global), vm) == 0,
                "ViewModel が Global の静的イベントを購読している");
        }

        [Fact]
        public void SpecialActionsListViewModel_CanBeInstantiatedWithInjectedServices()
        {
            DS4WinWPF.AppHost.CreateHost();

            var specialActionRepo = DS4WinWPF.AppHost.GetService<ISpecialActionRepository>();
            var profileRepo = DS4WinWPF.AppHost.GetService<IProfileRepository>();
            var outputSlotService = DS4WinWPF.AppHost.GetService<IOutputSlotService>();
            var profileSettings = DS4WinWPF.AppHost.GetService<IProfileSettingsService>();

            // コンストラクタへの DI サービス直接注入による ViewModel 生成を検証
            var vm = new SpecialActionsListViewModel(0, specialActionRepo, profileRepo);

            Assert.NotNull(vm);
            Assert.NotNull(vm.ActionCol);
        }

        private sealed class FakePublisher
        {
            public event EventHandler Changed;
            public void Raise() => Changed?.Invoke(this, EventArgs.Empty);
        }

        private sealed class FakeSubscriber
        {
            public void OnChanged(object sender, EventArgs e) { }
        }
    }
}
