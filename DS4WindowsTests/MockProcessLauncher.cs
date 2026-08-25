using System;
using DS4Windows.Actions;

namespace DS4WindowsTests
{
    /// <summary>
    /// C5 LaunchProcessAction の単体テスト用モック（T2, T3, T4 用）
    /// §2.1修正版: 新経路（DI経由）とフォールバック（直接 Process.Start）を両方検証可能にする
    /// </summary>
    public class MockProcessLauncher : IProcessLauncher
    {
        public bool LaunchCalled { get; private set; } = false;
        public string LastPath { get; private set; } = null;

        public void Launch(string filePath)
        {
            LaunchCalled = true;
            LastPath = filePath;
        }

        public void Reset()
        {
            LaunchCalled = false;
            LastPath = null;
        }
    }
}
