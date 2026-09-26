using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// テスト用に ControlService を生成する共通ファクトリ。
    /// コンストラクタの各引数を AppHost（正式な Composition Root）から型で解決するため、
    /// Phase6-Step2 の各 PR でコンストラクタに引数が追加されても、テスト側の生成コードを修正する必要がない。
    /// </summary>
    internal static class ControlServiceTestFactory
    {
        /// <param name="nullParameterName">
        /// 指定した引数だけに null を渡して生成する（必須引数の null 拒否を検証する用途）。null なら全引数を解決して生成する。
        /// </param>
        /// <param name="outputSlotServiceFactory">Func&lt;IOutputSlotService&gt; 引数を差し替える（遅延解決の検証用）。</param>
        public static ControlService Create(
            string nullParameterName = null,
            Func<IOutputSlotService> outputSlotServiceFactory = null)
        {
            DS4WinWPF.AppHost.CreateHost();

            ConstructorInfo constructor = typeof(ControlService).GetConstructors().Single();
            ParameterInfo[] parameters = constructor.GetParameters();
            object[] arguments = new object[parameters.Length];
            bool nullTargetFound = nullParameterName == null;

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Name == nullParameterName)
                {
                    arguments[i] = null;
                    nullTargetFound = true;
                    continue;
                }

                arguments[i] = Resolve(parameters[i].ParameterType, outputSlotServiceFactory);
            }

            if (!nullTargetFound)
            {
                throw new ArgumentException(
                    $"ControlService のコンストラクタに引数 '{nullParameterName}' が存在しません。", nameof(nullParameterName));
            }

            try
            {
                return (ControlService)constructor.Invoke(arguments);
            }
            catch (TargetInvocationException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        private static object Resolve(Type parameterType, Func<IOutputSlotService> outputSlotServiceFactory)
        {
            if (parameterType == typeof(DS4WinWPF.ArgumentParser))
            {
                return new DS4WinWPF.ArgumentParser();
            }

            if (parameterType == typeof(Func<IOutputSlotService>))
            {
                return outputSlotServiceFactory
                    ?? (Func<IOutputSlotService>)(() => DS4WinWPF.AppHost.GetService<IOutputSlotService>());
            }

            return DS4WinWPF.AppHost.GetService(parameterType);
        }
    }
}