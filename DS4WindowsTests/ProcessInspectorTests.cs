﻿using System;
using System.Diagnostics;
using Xunit;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase3-Step3-6-B で新設した DefaultProcessInspector の単体テスト。
    /// 実プロセス一覧を走査する実装のため、実行中の自プロセス（テストランナー自身）を
    /// 既知の「起動中プロセス」として利用し、UACや実機を必要とせずに検証する。
    /// </summary>
    public class ProcessInspectorTests
    {
        [Fact]
        public void IsProcessRunning_CurrentProcess_ReturnsTrue()
        {
            var inspector = new DefaultProcessInspector();
            string currentExePath = Process.GetCurrentProcess().MainModule.FileName;

            bool result = inspector.IsProcessRunning(currentExePath);

            Assert.True(result);
        }

        [Fact]
        public void IsProcessRunning_NonExistentPath_ReturnsFalse()
        {
            var inspector = new DefaultProcessInspector();
            string bogusPath = @"C:\this\path\definitely\does\not\exist\ghost12345.exe";

            bool result = inspector.IsProcessRunning(bogusPath);

            Assert.False(result);
        }

        [Fact]
        public void IsProcessRunning_EmptyPath_ReturnsFalse()
        {
            var inspector = new DefaultProcessInspector();

            bool result = inspector.IsProcessRunning(string.Empty);

            Assert.False(result);
        }
    }
}
