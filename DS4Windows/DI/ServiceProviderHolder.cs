using System;
using Microsoft.Extensions.DependencyInjection;

namespace DS4Windows.DI
{
    public static class ServiceProviderHolder
    {
        private static IServiceProvider provider;

        public static IServiceProvider Provider => provider;

        public static void SetProvider(IServiceProvider sp)
        {
            provider = sp;
        }

        public static T GetRequiredService<T>() => (T)provider.GetService(typeof(T));
    }
}
