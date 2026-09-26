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

        public static IHost Host => _host;

        public static IHost CreateHost(IConfiguration configuration = null)
        {
            lock (_loggedResolvedTypes) { _loggedResolvedTypes.Clear(); }
            lock (_syncLock)
            {
                if (_host != null)
                    return _host;

                var builder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                    .ConfigureServices((context, services) =>
                    {
                        DS4Windows.DI.ServiceRegistration.RegisterServices(services);
                    });

                _host = builder.Build();
                DS4Windows.DI.ServiceProviderHolder.SetProvider(_host.Services);
                PreallocateActionEntries();
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace("[DI] AppHost.CreateHost: Host initialized and all services registered");
                return _host;
            }
        }

        public static IHost CreateHost(IConfiguration configuration, DS4WinWPF.ArgumentParser parser)
        {
            lock (_loggedResolvedTypes) { _loggedResolvedTypes.Clear(); }
            lock (_syncLock)
            {
                if (_host != null)
                {
                    // Phase6-Step7-4（決定7＝案H）: 現在の起動順序では、ホストは Global の静的初期化の中で
                    // 起動引数なしの CreateHost() により先に作られている（Phase6-Step7-Plan.md §0.4 の訂正）。
                    // 構築済みのコンテナには登録を追加できないため、登録済みの IStartupArguments へ起動引数を設定する。
                    ApplyStartupArguments(parser, hostAlreadyCreated: true);
                    return _host;
                }

                var builder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                    .ConfigureServices((context, services) =>
                    {
                        DS4Windows.DI.ServiceRegistration.RegisterServices(services);
                        services.AddSingleton(parser);
                    });

                _host = builder.Build();
                DS4Windows.DI.ServiceProviderHolder.SetProvider(_host.Services);
                PreallocateActionEntries();
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

        public static IHost CreateHost(string[] args)
        {
            lock (_loggedResolvedTypes) { _loggedResolvedTypes.Clear(); }
            lock (_syncLock)
            {
                if (_host != null)
                    return _host;

                var builder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args ?? Array.Empty<string>())
                    .ConfigureServices((context, services) =>
                    {
                        DS4Windows.DI.ServiceRegistration.RegisterServices(services);
                    });

                _host = builder.Build();
                DS4Windows.DI.ServiceProviderHolder.SetProvider(_host.Services);
                PreallocateActionEntries();
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace("[DI] AppHost.CreateHost: Host initialized with args");
                return _host;
            }
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
                    }
                }
            }
        }
    }
}

namespace DS4Windows
{
    public static class AppHost
    {
        private static readonly System.Collections.Generic.HashSet<System.Type> _loggedResolvedTypes = new System.Collections.Generic.HashSet<System.Type>();
        public static Microsoft.Extensions.Hosting.IHost Host => DS4WinWPF.AppHost.Host;
        public static Microsoft.Extensions.Hosting.IHost CreateHost(string[] args = null) => DS4WinWPF.AppHost.CreateHost(args);
        public static T GetService<T>() where T : class => DS4WinWPF.AppHost.GetService<T>();
        public static object GetService(Type serviceType) => DS4WinWPF.AppHost.GetService(serviceType);
        public static void Dispose() => DS4WinWPF.AppHost.Dispose();
    }
}
