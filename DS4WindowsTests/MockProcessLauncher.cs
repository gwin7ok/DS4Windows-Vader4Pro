using System;
using System.Collections.Generic;
using DS4Windows.Actions;

namespace DS4WindowsTests
{
    /// <summary>
    /// C5 LaunchProcessAction の単体テスト用モック（T1〜T6 用）
    /// 4引数オーバーロード（引数、ShellExecute、非表示ウィンドウ制御）に対応
    /// </summary>
    public class MockProcessLauncher : IProcessLauncher
    {
        public class LaunchCall
        {
            public string FileName { get; set; }
            public string Arguments { get; set; }
            public bool UseShellExecute { get; set; }
            public bool Hidden { get; set; }
        }

        public bool LaunchCalled { get; private set; } = false;
        public string LastPath { get; private set; } = null;
        public string LastArguments { get; private set; } = null;
        public bool LastUseShellExecute { get; private set; } = false;
        public bool LastHidden { get; private set; } = false;

        /// <summary>
        /// 呼び出し履歴リスト
        /// </summary>
        public List<LaunchCall> Calls { get; } = new List<LaunchCall>();

        /// <summary>
        /// 1引数版 Launch
        /// </summary>
        public void Launch(string filePath)
        {
            LaunchCalled = true;
            LastPath = filePath;
            LastArguments = null;
            LastUseShellExecute = false;
            LastHidden = false;

            Calls.Add(new LaunchCall
            {
                FileName = filePath,
                Arguments = null,
                UseShellExecute = false,
                Hidden = false
            });
        }

        /// <summary>
        /// 4引数版 Launch（引数・ShellExecute・Hidden制御）
        /// </summary>
        public void Launch(string fileName, string arguments, bool useShellExecute, bool hidden)
        {
            LaunchCalled = true;
            LastPath = fileName;
            LastArguments = arguments;
            LastUseShellExecute = useShellExecute;
            LastHidden = hidden;

            Calls.Add(new LaunchCall
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = useShellExecute,
                Hidden = hidden
            });
        }

        /// <summary>
        /// 状態リセット
        /// </summary>
        public void Reset()
        {
            LaunchCalled = false;
            LastPath = null;
            LastArguments = null;
            LastUseShellExecute = false;
            LastHidden = false;
            Calls.Clear();
        }
    }
}
