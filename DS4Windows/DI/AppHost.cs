using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DS4Windows;

namespace DS4WinWPF
{
    public static class AppHost
    {
        private static readonly System.Collections.Generic.HashSet<System.Type> _loggedResolvedTypes = new System.Collections.Generic.HashSet<System.Type>();
        private static IHost _host;
        private static readonly object _syncLock = new object();

        // Phase7-Step4 W2: Dispose 済みかどうか。終了処理の途中で（Dispose の後に）GetService が呼ばれても、
        // 暗黙に 2 つ目のホストを作らないために使う。CreateHost を明示的に呼ぶと解除される。
        private static bool _disposed;

        public static IHost Host => _host;

        /// <summary>
        /// Phase6-Step12（K7-1 の原因特定用）: ホストがどの経路で作られたかの記録。
        /// ホスト構築の時点では、ログの最小レベルがまだ反映されておらず Trace が捨てられるため、
        /// App.xaml.cs が、ロガー初期化後に、Info で出力する。
        /// </summary>
        public static string CreationNote { get; private set; }

        // Phase7-Step4 W2: 未使用だった configuration 引数を削除した（テストはすべて引数なしで呼ぶ）。
        // 本体の起動は CreateHost(configuration, parser) を使う。引数なしの本メソッドはテストと、GetService の暗黙の構築が使う。
        public static IHost CreateHost()
        {
            lock (_loggedResolvedTypes) { _loggedResolvedTypes.Clear(); }
            lock (_syncLock)
            {
                if (_host != null)
                    return _host;

                _disposed = false;

                var builder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                    .ConfigureServices((context, services) =>
                    {
                        DS4Windows.DI.ServiceRegistration.RegisterServices(services);
                    });

                _host = builder.Build();
                DS4Windows.DI.ServiceProviderHolder.SetProvider(_host.Services);
                PreallocateActionEntries();
                BindSyntheticDispatcher();
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace("[DI] AppHost.CreateHost: Host initialized and all services registered");
                return _host;
            }
        }

        /// <summary>
        /// 起動引数つきでホストを作る（本体の起動と -driverinstall 分岐が使う）。
        /// Phase7-Step4 W4（D3＝S1）: コンテナの構築より前に作ったブートストラップのサービスを渡すと、同じインスタンスをコンテナへ登録する
        /// （Pre-Host の起動処理と、コンテナから解決するサービスが同じインスタンスになる）。ホストが構築済みのときは、登録は変えられない。
        /// </summary>
        public static IHost CreateHost(IConfiguration configuration, DS4WinWPF.ArgumentParser parser,
            DS4Windows.DI.IPathService bootstrapPathService = null,
            DS4Windows.DI.IEnvironmentService bootstrapEnvironmentService = null)
        {
            lock (_loggedResolvedTypes) { _loggedResolvedTypes.Clear(); }
            lock (_syncLock)
            {
                if (_host != null)
                {
                    // Phase6-Step7-4（決定7＝案H）: 構築済みのコンテナには登録を追加できないため、
                    // 登録済みの IStartupArguments へ起動引数を設定する。
                    // 通常の起動では、App.xaml.cs の明示的な CreateHost(config, parser) がホストを作る
                    // （2026-10-17 の起動ログで確認。Global の静的初期化は、もうホストを作らない。
                    // 旧: ScpUtil.cs の fallbackProfileRepository は Step12 までに削除済み）。
                    // この分岐に来るのは、それより前に GetService が呼ばれた場合。
                    // Phase7-Step4 W2: -driverinstall 分岐も、設定の読込の前に、この CreateHost(config, parser) を明示的に呼ぶ
                    // ようにした（旧: Global.Load() の中の GetService が、引数なしでホストを暗黙に作っていた。K7-1 の残存経路）。
                    CreationNote = CreationNote ?? "host already existed (creator not recorded)";
                    ApplyStartupArguments(parser, hostAlreadyCreated: true);
                    return _host;
                }

                _disposed = false;
                var builder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                    .ConfigureServices((context, services) =>
                    {
                        DS4Windows.DI.ServiceRegistration.RegisterServices(services);
                        services.AddSingleton(parser);
                        // 後から登録した実装が、同じ型の解決で優先される（既定の登録を、ブートストラップのインスタンスで置き換える）
                        if (bootstrapPathService != null)
                            services.AddSingleton<DS4Windows.DI.IPathService>(bootstrapPathService);
                        if (bootstrapEnvironmentService != null)
                            services.AddSingleton<DS4Windows.DI.IEnvironmentService>(bootstrapEnvironmentService);
                    });

                _host = builder.Build();
                DS4Windows.DI.ServiceProviderHolder.SetProvider(_host.Services);
                PreallocateActionEntries();
                BindSyntheticDispatcher();
                CreationNote = "explicit CreateHost(config, parser) created the host";
                ApplyStartupArguments(parser, hostAlreadyCreated: false);
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace("[DI] AppHost.CreateHost: Host initialized with runtime parser");
                return _host;
            }
        }

        /// <summary>
        /// Phase6-Step7-4（決定7＝案H）: 起動引数を、登録済みの <see cref="DS4Windows.DI.IStartupArguments"/> へ設定する。
        /// ControlService の生成（ServiceRegistration）はここで設定された値を使う。
        /// </summary>
        private static void ApplyStartupArguments(DS4WinWPF.ArgumentParser parser, bool hostAlreadyCreated)
        {
            if (parser == null)
                return;

            var startupArguments = _host?.Services.GetService<DS4Windows.DI.IStartupArguments>();
            if (startupArguments == null)
                return;

            startupArguments.SetParser(parser);
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] AppHost.CreateHost: startup arguments applied to IStartupArguments (host {(hostAlreadyCreated ? "already created" : "newly created")})");
        }

        /// <summary>
        /// Phase7-Step8 W3（Q6＝A）: 合成キーの送出（ISyntheticDispatcher）を作り、Actions サブシステムが静的に呼ぶ橋渡し
        /// （SyntheticDispatcher.Bind。ServiceRegistration の登録の中で束縛される）を、ホストの構築直後に有効にする。
        /// </summary>
        private static void BindSyntheticDispatcher()
        {
            try { _host?.Services.GetService<DS4Windows.DI.ISyntheticDispatcher>(); } catch { }
        }

        private static void PreallocateActionEntries()
        {
            try
            {
                var manager = _host?.Services.GetService<DS4Windows.Actions.IManagedActionManager>()
                    as DS4Windows.Actions.DefaultActionManager;
                manager?.PreallocateEntries();
            }
            catch { }
        }

        public static T GetService<T>() where T : class
        {
            if (_host == null)
            {
                lock (_syncLock)
                {
                    if (_host == null)
                    {
                        // Phase7-Step4 W2: Dispose の後は、暗黙にホストを作らない（終了処理中に 2 つ目のホストができるのを防ぐ）
                        if (_disposed)
                            return default;

                        // Phase6-Step12（K7-1 の原因特定用）: ホストが明示的な構築より前に、暗黙に作られる箇所を、呼び出し元つきで記録する
                        CreationNote = $"implicit CreateHost (no parser) triggered by resolving {typeof(T).Name}. Caller stack:{Environment.NewLine}{Environment.StackTrace}";
                        CreateHost();
                    }
                }
            }

            var service = _host?.Services.GetService<T>();
            if (AppLogger.IsTraceEnabled)
            {
                lock (_loggedResolvedTypes)
                {
                    if (_loggedResolvedTypes.Add(typeof(T)))
                    {
                        AppLogger.LogTrace($"[DI] AppHost.GetService: Resolved {typeof(T).Name}");
                    }
                }
            }
            return service;
        }

        public static object GetService(Type serviceType)
        {
            if (_host == null)
            {
                lock (_syncLock)
                {
                    if (_host == null)
                    {
                        // Phase7-Step4 W2: Dispose の後は、暗黙にホストを作らない（終了処理中に 2 つ目のホストができるのを防ぐ）
                        if (_disposed)
                            return null;

                        // Phase6-Step12（K7-1 の原因特定用）: ホストが明示的な構築より前に、暗黙に作られる箇所を、呼び出し元つきで記録する
                        CreationNote = $"implicit CreateHost (no parser) triggered by resolving {serviceType.Name}. Caller stack:{Environment.NewLine}{Environment.StackTrace}";
                        CreateHost();
                    }
                }
            }

            var service = _host?.Services.GetService(serviceType);
            if (AppLogger.IsTraceEnabled)
            {
                lock (_loggedResolvedTypes)
                {
                    if (_loggedResolvedTypes.Add(serviceType))
                    {
                        AppLogger.LogTrace($"[DI] AppHost.GetService: Resolved {serviceType.Name}");
                    }
                }
            }
            return service;
        }

        public static void Dispose()
        {
            lock (_syncLock)
            {
                if (_host != null)
                {
                    try
                    {
                        _host.Dispose();
                        if (AppLogger.IsTraceEnabled)
                            AppLogger.LogTrace("[DI] AppHost.Dispose: Host disposed");
                    }
                    catch { }
                    finally
                    {
                        _host = null;
                        _disposed = true;
                    }
                }
            }
        }
    }
}
