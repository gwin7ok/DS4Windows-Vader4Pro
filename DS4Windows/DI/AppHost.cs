using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DS4Windows;

namespace DS4WinWPF
{
    public static class AppHost
    {
        private static IHost _host;
        private static readonly object _syncLock = new object();

        public static IHost Host => _host;

        public static IHost CreateHost(IConfiguration configuration = null)
        {
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
                AppLogger.LogToGui("[DI] AppHost.CreateHost: Host initialized and all services registered", false, true);
                return _host;
            }
        }

        public static IHost CreateHost(string[] args)
        {
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
                AppLogger.LogToGui("[DI] AppHost.CreateHost: Host initialized with args", false, true);
                return _host;
            }
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
            if (service != null)
            {
                AppLogger.LogToGui($"[DI] AppHost.GetService: Resolved {typeof(T).Name}", false, true);
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
            if (service != null)
            {
                AppLogger.LogToGui($"[DI] AppHost.GetService: Resolved {serviceType.Name}", false, true);
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
                        AppLogger.LogToGui("[DI] AppHost.Dispose: Host disposed", false, true);
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
        public static Microsoft.Extensions.Hosting.IHost Host => DS4WinWPF.AppHost.Host;
        public static Microsoft.Extensions.Hosting.IHost CreateHost(string[] args = null) => DS4WinWPF.AppHost.CreateHost(args);
        public static T GetService<T>() where T : class => DS4WinWPF.AppHost.GetService<T>();
        public static object GetService(Type serviceType) => DS4WinWPF.AppHost.GetService(serviceType);
        public static void Dispose() => DS4WinWPF.AppHost.Dispose();
    }
}
