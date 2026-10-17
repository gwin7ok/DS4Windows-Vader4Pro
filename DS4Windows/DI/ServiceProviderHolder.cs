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

        /// <summary>
        /// 登録済みのサービスを返す。プロバイダー未設定、またはサービス未登録のときは例外を投げる（null を返さない）。
        /// Phase6-Step9b-2（F07）: 旧実装は未登録のとき null を T として返し、失敗が無関係な呼び出し先へ移っていた。
        /// 呼び出し元は 0 件だったため、動作の変更で影響する箇所はない。null を許容する取得は Provider.GetService を使う。
        /// </summary>
        public static T GetRequiredService<T>()
        {
            var sp = provider ?? throw new InvalidOperationException(
                $"ServiceProviderHolder: provider is not set (requested {typeof(T).Name})");
            return (T)(sp.GetService(typeof(T)) ?? throw new InvalidOperationException(
                $"ServiceProviderHolder: service {typeof(T).Name} is not registered"));
        }
    }
}
