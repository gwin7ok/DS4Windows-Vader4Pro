using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

            return _host?.Services.GetService<T>();
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

            return _host?.Services.GetService(serviceType);
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
