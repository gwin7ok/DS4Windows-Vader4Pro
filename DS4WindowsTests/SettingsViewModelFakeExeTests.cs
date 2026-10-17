using System;
using System.IO;
using Xunit;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step9-3d（K9-2）: 偽装 exe 名の作成・削除の回帰テスト。
    /// 以前は設定ファイルの元を <c>実行ファイル名.exe.runtimeconfig.json</c>（実在しない）から組み立てていたため、
    /// コピーが例外になり、設定ファイルが作られず、古いファイルも消えなかった。
    /// 実在しないフォルダ・ファイルを使わず、一時フォルダで検証する。
    /// </summary>
    public class SettingsViewModelFakeExeTests : IDisposable
    {
        private readonly string _dir;
        private readonly string _exe;

        public SettingsViewModelFakeExeTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "Phase6Step9FakeExe_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _exe = Path.Combine(_dir, "DS4Windows.exe");
            File.WriteAllText(_exe, "exe");
            File.WriteAllText(Path.Combine(_dir, "DS4Windows.runtimeconfig.json"), "{\"conf\":1}");
            File.WriteAllText(Path.Combine(_dir, "DS4Windows.deps.json"), "{\"deps\":1}");
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, true); } catch { }
        }

        [Fact]
        public void CreateFakeExeFiles_CopiesExeAndBothConfigFiles()
        {
            SettingsViewModel.CreateFakeExeFiles(_dir, _exe, "TestFake");

            Assert.True(File.Exists(Path.Combine(_dir, "TestFake.exe")));
            Assert.Equal("{\"conf\":1}", File.ReadAllText(Path.Combine(_dir, "TestFake.runtimeconfig.json")));
            Assert.Equal("{\"deps\":1}", File.ReadAllText(Path.Combine(_dir, "TestFake.deps.json")));
        }

        [Fact]
        public void CreateFakeExeFiles_WhenConfigFilesAreMissing_StillCreatesExe()
        {
            File.Delete(Path.Combine(_dir, "DS4Windows.runtimeconfig.json"));
            File.Delete(Path.Combine(_dir, "DS4Windows.deps.json"));

            Exception ex = Record.Exception(() => SettingsViewModel.CreateFakeExeFiles(_dir, _exe, "TestFake"));

            Assert.Null(ex);
            Assert.True(File.Exists(Path.Combine(_dir, "TestFake.exe")));
            Assert.False(File.Exists(Path.Combine(_dir, "TestFake.runtimeconfig.json")));
        }

        [Fact]
        public void CreateFakeExeFiles_SameNameTwice_Overwrites()
        {
            SettingsViewModel.CreateFakeExeFiles(_dir, _exe, "TestFake");

            Exception ex = Record.Exception(() => SettingsViewModel.CreateFakeExeFiles(_dir, _exe, "TestFake"));

            Assert.Null(ex);
        }

        [Fact]
        public void DeleteFakeExeFiles_RemovesAllThreeFiles_AndKeepsTheRealOnes()
        {
            SettingsViewModel.CreateFakeExeFiles(_dir, _exe, "TestFake");

            SettingsViewModel.DeleteFakeExeFiles(_dir, "TestFake");

            Assert.False(File.Exists(Path.Combine(_dir, "TestFake.exe")));
            Assert.False(File.Exists(Path.Combine(_dir, "TestFake.runtimeconfig.json")));
            Assert.False(File.Exists(Path.Combine(_dir, "TestFake.deps.json")));
            Assert.True(File.Exists(_exe));
            Assert.True(File.Exists(Path.Combine(_dir, "DS4Windows.runtimeconfig.json")));
            Assert.True(File.Exists(Path.Combine(_dir, "DS4Windows.deps.json")));
        }

        [Fact]
        public void DeleteFakeExeFiles_WithEmptyName_DoesNothing()
        {
            SettingsViewModel.CreateFakeExeFiles(_dir, _exe, "TestFake");

            SettingsViewModel.DeleteFakeExeFiles(_dir, string.Empty);
            SettingsViewModel.DeleteFakeExeFiles(_dir, null);

            Assert.True(File.Exists(Path.Combine(_dir, "TestFake.exe")));
        }

        [Fact]
        public void Rename_RemovesOldFilesBeforeCreatingNewOnes()
        {
            // 名前の変更: 古い 3 ファイルを消してから、新しい 3 ファイルを作る（SettingsViewModel の呼び出し順と同じ）
            SettingsViewModel.CreateFakeExeFiles(_dir, _exe, "TestFake");

            SettingsViewModel.DeleteFakeExeFiles(_dir, "TestFake");
            SettingsViewModel.CreateFakeExeFiles(_dir, _exe, "TestFake2");

            Assert.False(File.Exists(Path.Combine(_dir, "TestFake.exe")));
            Assert.True(File.Exists(Path.Combine(_dir, "TestFake2.exe")));
            Assert.True(File.Exists(Path.Combine(_dir, "TestFake2.runtimeconfig.json")));
            Assert.True(File.Exists(Path.Combine(_dir, "TestFake2.deps.json")));
        }
    }
}
