using System;
using Microsoft.Extensions.DependencyInjection;
using DS4Windows.Actions;
using DS4Windows.Services;
using DS4WinWPF;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase 3 / Phase 4 / Phase 5: アプリケーション全体のDIコンテナ初期登録を一元管理する。
    /// </summary>
    public static class ServiceRegistration
    {
        // Phase7-Step8 W3: BackingStore.ActionsLoaded の購読者（コンテナの作り直しで重複購読しないための記憶）
        private static ActionsLoadedRuntimeReset _subscribedReset;

        public static IServiceCollection RegisterServices(this IServiceCollection services)
        {
            // === 第4層: 4-c DIサービス群 (Singleton) ===
            // Phase7-Step3 W1: BackingStore（設定の唯一の実体）を登録する。実体は Global.store と同一（Global の生成は Step10 まで残す）
            // Phase7-Step5 W4（Q6＝A）: BackingStore は Global が生成する（コンテナ外）ため、IProcessInspector はプロパティで設定する
            // （プロファイル読込の「起動するプログラム」の稼働確認で使う。Step10 の分割でコンストラクター注入へ）
            services.AddSingleton<BackingStore>(sp =>
            {
                var store = Global.store;
                store.ProcessInspector = sp.GetRequiredService<IProcessInspector>();
                // Phase7-Step7 W2（Q4＝A）: Actions.xml の読み込み後の実行時状態のリセット。Global.store は静的でコンテナより長く生きるため、
                // コンテナが作り直されても購読が 1 つになるよう、前回の購読者を外してから登録する（Phase7-Step8 W3: instance の購読者）
                var reset = sp.GetRequiredService<ActionsLoadedRuntimeReset>();
                if (_subscribedReset != null) store.ActionsLoaded -= _subscribedReset.Reset;
                store.ActionsLoaded += reset.Reset;
                _subscribedReset = reset;
                return store;
            });
            // Phase7-Step8 W4: 状態の保持クラス 3 つ（合成入力・マクロ・スティック／ジャイロ／トリガー）。暫定の静的な窓口（Shared）は削除し、
            // コンテナの Singleton を、利用者（Mapping、Mouse、ControlService ほか）がコンストラクターで受ける
            services.AddSingleton<SyntheticInputState>();
            services.AddSingleton<MacroRuntimeState>();
            services.AddSingleton<StickMotionState>();
            // Phase7-Step8 W3: Actions.xml の読み込み後のリセットの購読者（保持クラスをコンストラクターで受ける）
            services.AddSingleton<ActionsLoadedRuntimeReset>();
            // Phase7-Step8 W3（Q6＝A）: 合成キーの送出。Actions サブシステム（静的な呼び出し）が使う橋渡し SyntheticDispatcher へ、作成時に 1 度だけ束縛する（Phase8 で削除）
            services.AddSingleton<ISyntheticDispatcher>(sp =>
            {
                var dispatcher = new SyntheticDispatcherService(
                    sp.GetRequiredService<SyntheticInputState>(), sp.GetRequiredService<OutputKBMHandlerAdapter>(), sp.GetRequiredService<IProfileSettingsService>());
                SyntheticDispatcher.Bind(dispatcher);
                return dispatcher;
            });
            services.AddSingleton<IPathService, PathService>();
            services.AddSingleton<IDeviceStateService, DeviceStateService>();
            services.AddSingleton<IEnvironmentService, EnvironmentService>();
            services.AddSingleton<IAppearanceSettingsService, AppearanceSettingsService>();
            services.AddSingleton<INotificationService, AppNotificationService>();

            // Phase6-Step7-4（決定7＝案H）: 起動引数の保持役。値は AppHost.CreateHost(config, parser) が設定する。
            services.AddSingleton<IStartupArguments, StartupArguments>();

            // Phase 5 Step 12: 出力スロット永続化・管理サービス
            services.AddSingleton<IOutputSlotStore, OutputSlotStore>();
            // Phase6-Step10-4-3: スロット管理の実体は ControlService が所有する。循環を避けるため、遅延で取得する
            // Phase7-Step6 W1: 出力種別の配列は OutputSlotService が持ち、BackingStore へは本サービスの生成時に渡す。
            // BackingStore の登録側から IOutputSlotService を解決してはならない（IOutputSlotStore → IPathService → BackingStore と循環し、
            // 実行時にスタックオーバーフローになる）。配列を書き込むのは ControlService だけで、IOutputSlotService を通すため、書き込みの前に必ず接続済みになる。
            services.AddSingleton<IOutputSlotService>(sp =>
            {
                var service = new OutputSlotService(
                    () => sp.GetRequiredService<ControlService>().OutputslotMan,
                    sp.GetRequiredService<IOutputSlotStore>(),
                    sp.GetRequiredService<Func<ControlService>>());
                sp.GetRequiredService<BackingStore>().ActiveOutDevType = service.ActiveOutDevType;
                return service;
            });

            // ControlService は遅延解決（循環の回避）
            services.AddSingleton<IProfileSettingsService>(sp => new ProfileSettingsService(
                sp.GetRequiredService<BackingStore>(), sp.GetRequiredService<IEnvironmentService>(), sp.GetRequiredService<Func<ControlService>>()));
            // Phase7-Step10c W2: 設定ファイルの保存・読込を担う型（旧: BackingStore のメソッド）。BackingStore を受ける具象クラスの Singleton
            services.AddSingleton<SpecialActionsXmlSerializer>();
            services.AddSingleton<LinkedProfilesXmlSerializer>();
            services.AddSingleton<ControllerConfigsXmlSerializer>();
            services.AddSingleton<AppSettingsXmlSerializer>();
            services.AddSingleton<LegacyProfileXmlSerializer>();
            services.AddSingleton<ProfilePresets>();
            services.AddSingleton<IProfileXmlStore, ProfileXmlStore>();
            services.AddSingleton<IProfileRepository, ProfileRepository>();
            // Phase6-Step10-2（決定 E2＝A）: プリセットの読み込み（Global.Load…Profile への薄い委譲）
            services.AddSingleton<IProfilePresetLoader, ProfilePresetLoader>();
            services.AddSingleton<IProfileSlotApplier, ProfileSlotApplier>();
            services.AddSingleton<ISpecialActionRepository>(sp => new SpecialActionRepository(
                sp.GetRequiredService<BackingStore>(), sp.GetRequiredService<IPathService>(),
                sp.GetRequiredService<SyntheticInputState>(), sp.GetRequiredService<SpecialActionsXmlSerializer>()));

            // Phase 3 Step 3-6: プロセス検査・昇格起動サービスの登録
            services.AddSingleton<IProcessInspector, DefaultProcessInspector>();
            services.AddSingleton<IElevatedProcessLauncher, DefaultElevatedProcessLauncher>();


            // プロファイルのアクション取得（ControlService が使用）。Phase6-Step10-4-2d: アクション連鎖サービスは廃止
            services.AddSingleton<IProfileActionProvider>(sp => new ProfileActionProvider(sp.GetRequiredService<BackingStore>()));

            // Phase7-Step8 W2（Q3＝A）: デバイス × アクション名ごとの KeyButtonActionController の登録簿（旧: Mapping の静的フィールド）
            services.AddSingleton<IKeyButtonControllerRegistry, KeyButtonControllerRegistry>();

            // Phase 5 Step 3: プロファイル適用サービス
            services.AddSingleton<IProfileApplicationService, ProfileApplicationService>();

            // Phase 5 Step 5: 自動プロファイル設定コレクション・実行サービス
            services.AddSingleton<AutoProfileHolder>();
            services.AddSingleton<IAutoProfileService, AutoProfileService>();

            // Phase 5 Step 6: アプリ全体設定サービス
            // 修正前:
            // services.AddSingleton<IAppSettingsService, AppSettingsService>();

            // 修正後（完全修飾名で確実にバインド）:
            services.AddSingleton<IAppSettingsService, DS4Windows.Services.AppSettingsService>();

            // Phase 5 Step 10: UDP サーバーサービス（Cemuhook モーションサーバー境界化）
            services.AddSingleton<IUdpServerService, UdpServerService>();

            // === 第3層: Actions基盤サービス ===
            services.AddSingleton<IActionFactory, DefaultActionFactory>();
            services.AddSingleton<IKeyActionCreator, DefaultKeyActionCreator>();
            services.AddSingleton<IKeyButtonActionControllerFactory, DefaultKeyButtonActionControllerFactory>();
            services.AddSingleton<IRepeater, RepeatHelperToIRepeaterAdapter>();
            services.AddSingleton<IProcessLauncher, DefaultProcessLauncher>();
            services.AddSingleton<IProfileSwitcher, DefaultProfileSwitcher>();
            // Phase7-Step8 W3（Q6＝A）: 出力ハンドラの実体を持つ OutputKBMHandlerAdapter を 1 つ登録し、IVirtualKBM（送出）と IVirtualKBMLifecycle（決定・破棄）が共有する
            services.AddSingleton<OutputKBMHandlerAdapter>();
            services.AddSingleton<IVirtualKBM>(sp => sp.GetRequiredService<OutputKBMHandlerAdapter>());
            services.AddSingleton<IVirtualKBMLifecycle, OutputKBMHandlerLifecycle>();
            services.AddSingleton<IMacroPlayer, DefaultMacroPlayer>();

            // Phase6-Step9b-2（F02）: IManagedActionManager と IControllerRegistry の再登録。
            // 2026-09-04（31dddc43、自動プロファイル設定コレクションの追加）で、無関係な差分として 3 件（ほかに IActionRegistry）が
            // 巻き込み削除されていた。IManagedActionManager が未登録だと、ActionManager.SetToggledOn が例外を投げ
            // （KeyButtonActionController が握りつぶす）IsToggledOn が true にならず、IControllerRegistry が未登録だと
            // KeyButtonActionController の登録と終了時の破棄が行われない。
            // AppHost.PreallocateActionEntries は、登録された DefaultActionManager を前提にしている。
            // IActionRegistry（系統A）は、実装が静的クラスで呼び出し元もないため再登録しない。
            services.AddSingleton<IManagedActionManager, DefaultActionManager>();
            services.AddSingleton<IControllerRegistry, DefaultControllerRegistry>();

            // Phase6-Step9-4（決定2＝C1）: ViewModelFactory が ControlService を生成のたびに遅延解決するための Func
            // （ControlService は Singleton。明示的なホスト構築より前に解決しないため、構築時には解決しない）
            services.AddSingleton<Func<ControlService>>(sp => () => sp.GetRequiredService<ControlService>());
            // Phase7-Step8 W2（Q1＝A・Q4＝A・Q8＝A）: 入力の評価の窓口（Singleton）。Mapping は IDeviceStateAccessor（＝ControlService）を遅延で受け、
            // DefaultActionManager／DefaultMacroPlayer は Func<IMapping> で受ける（ホスト構築時に Mapping とその依存を作らないため。Phase5-Plan §5.4）。
            services.AddSingleton<IMapping, Mapping>();
            services.AddSingleton<Func<IMapping>>(sp => () => sp.GetRequiredService<IMapping>());
            services.AddSingleton<Func<IDeviceStateAccessor>>(sp => () => sp.GetRequiredService<IDeviceStateAccessor>());
            // Phase6-Step10-2-3: IAutoProfileService の遅延解決（AutoProfileHolder は設定の場所が決まった後でなければ生成できない）
            services.AddSingleton<Func<IAutoProfileService>>(sp => () => sp.GetRequiredService<IAutoProfileService>());

            // Phase 4: UI層 ViewModel ファクトリの登録
            services.AddSingleton<IViewModelFactory, ViewModelFactory>();

            // Phase6-Step10-2（決定 E1＝A）: 補助ウィンドウ（ボタン割り当て、マクロ記録、プリセット、スティック校正、スペシャルアクション編集）の生成の窓口
            services.AddSingleton<IEditorWindowFactory, EditorWindowFactory>();

            // Phase7-Step5 W5（Q4＝A）: メイン画面の生成の窓口。MainWindow のコンストラクターで AppHost.GetService を使わないための窓口
            services.AddSingleton<IMainWindowFactory, MainWindowFactory>();

            // Phase6-Step10-1-5（決定D7＝A）: ログ設定の反映の窓口。LoggerHolder は設定の読み込み後にしか生成できないため、
            // 遅延接続のプロキシを登録し、App が LoggerHolder の生成直後に Attach する。
            services.AddSingleton<LogSettingsApplierProxy>();
            services.AddSingleton<ILogSettingsApplier>(sp => sp.GetRequiredService<LogSettingsApplierProxy>());

            // Phase 4: Pattern A ViewModel (Transient)
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<LogViewModel>();
            services.AddTransient<AboutViewModel>();

            // Phase 4: Pattern B ViewModel (Singleton)
            services.AddSingleton<MainWindowsViewModel>();

            // === 既存Singletonインスタンスの取得登録 ===
            services.AddSingleton<IDs4DeviceRegistry>(sp => new Ds4DeviceRegistryAdapter());
            services.AddSingleton<ControlService>(sp =>
            {
                // Phase7-Step5 W2（Q2＝A）: 旧 Program.rootHub ?? の分岐を削除した。シングルトンの生成は、このファクトリーだけが行う
                return new ControlService(
                    // Phase6-Step7-4（決定5＝A、決定7＝案H）: AppHost.CreateHost(config, parser) が IStartupArguments に
                    // 設定した実際の起動引数を渡す。2026-09-04（4c89cd91）以降は常に空の new ArgumentParser() を渡していたため、
                    // 起動引数 -virtualkbm（キーボード・マウス出力方式の指定）が無視されていた。
                    // ホストが起動引数なしに先に作られる順序（-driverinstall 分岐など）でも動くよう、コンテナへの登録
                    // （AddSingleton(parser)）ではなく、登録済みの保持役を経由する（Phase6-Step7-Plan.md §0.4・決定7）。
                    // 起動引数が設定されていない場合（引数なしの CreateHost()、テスト）は、従来どおり空のパーサーを使う。
                    // 注意: ControlService はこの時点の値を保持するため、CreateHost(config, parser) より前に解決してはならない
                    // （現在は App.CreateControlService で、明示的なホスト構築の後に解決している）。
                    sp.GetRequiredService<IStartupArguments>().Parser ?? new ArgumentParser(),
                    sp.GetRequiredService<IDs4DeviceRegistry>(),
                    sp.GetRequiredService<IProfileSettingsService>(),
                    sp.GetRequiredService<IAppSettingsService>(),
                    sp.GetRequiredService<IEnvironmentService>(),
                    sp.GetRequiredService<IPathService>(),
                    () => sp.GetRequiredService<IOutputSlotService>(), // 決定D1: 遅延解決（OutputSlotService → ControlService の逆依存を回避）
                    sp.GetRequiredService<IProfileRepository>(),
                    sp.GetRequiredService<IDeviceStateService>(),
                    sp.GetRequiredService<IProfileXmlStore>(),
                    sp.GetRequiredService<IProfileSlotApplier>(),
                    sp.GetRequiredService<IVirtualKBM>(),
                    sp.GetRequiredService<IVirtualKBMLifecycle>(),
                    sp.GetRequiredService<IAppearanceSettingsService>(),
                    sp.GetRequiredService<IProfileActionProvider>(),
                    sp.GetRequiredService<ISpecialActionRepository>(),
                    sp.GetRequiredService<IElevatedProcessLauncher>(),
                    // Phase7-Step8 W1: 保持クラスをコンストラクターで受ける（旧: ControlService／Mouse が Shared を参照）
                    sp.GetRequiredService<StickMotionState>(),
                    sp.GetRequiredService<SyntheticInputState>(),
                    sp.GetRequiredService<IMapping>()
                );
            });
            services.AddSingleton<IDeviceStateAccessor>(sp => sp.GetRequiredService<ControlService>());

            return services;
        }
    }
}