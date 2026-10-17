using System;
using System.IO;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step1 W3: アプリ設定（<c>Profiles.xml</c>）の DTO 読込（<c>BackingStore.Load</c>）の後処理の特性化テスト。
    /// 不具合 B: DTO の経路は <c>MapTo</c> の直後に <c>return</c> し、偽の exe 名の読み込み、<c>lastVersionCheckedNum</c> の算出、
    /// ポート番号の範囲の丸め、Distance プロファイルの判定が行われていなかった（105575a7 〜。移行前から続く不具合）。
    /// 保存は <c>BackingStore.Save</c> の実出力（DTO）を使い、読込は別の <c>BackingStore</c> で行う。
    /// <c>Global.exedirpath</c>（偽の exe 名ファイルの置き場）は、テストの間だけ一時フォルダへ向け、終了時に元へ戻す。
    /// 並列実行は xunit.runner.json で無効（静的状態の書き換えが他のテストと競合しない）。
    /// </summary>
    public class BackingStoreAppSettingsLoadTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _originalExeDirPath;
        private readonly string _profilesXml;
        private readonly string _fakeExeFile;

        public BackingStoreAppSettingsLoadTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "ds4w_p7s1w3_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _originalExeDirPath = Global.exedirpath;
            Global.exedirpath = _tempDir;
            _profilesXml = Path.Combine(_tempDir, "Profiles.xml");
            _fakeExeFile = Path.Combine(_tempDir, Global.CUSTOM_EXE_CONFIG_FILENAME);
        }

        public void Dispose()
        {
            Global.exedirpath = _originalExeDirPath;
            try { Directory.Delete(_tempDir, true); } catch { }
        }

        private BackingStore SaveStore(Action<BackingStore> configure = null)
        {
            var writer = new BackingStore { m_Profile = _profilesXml };
            configure?.Invoke(writer);
            Assert.True(TestRepositoryFactory.CreateAppSettingsXml(writer).Save(), "テスト用の Profiles.xml（DTO）の保存に失敗した");
            return writer;
        }

        private BackingStore LoadStore()
        {
            var reader = new BackingStore { m_Profile = _profilesXml };
            Assert.True(TestRepositoryFactory.CreateAppSettingsXml(reader).Load());
            return reader;
        }

        [Fact]
        public void Load_Dto_RestoresFakeExeNameFromFile()
        {
            SaveStore();
            File.WriteAllText(_fakeExeFile, "fake_game.exe");

            BackingStore reader = LoadStore();

            Assert.Equal("fake_game.exe", reader.fakeExeFileName);
        }

        [Fact]
        public void Load_Dto_InvalidFakeExeName_IsIgnored()
        {
            SaveStore();
            File.WriteAllText(_fakeExeFile, "bad|name?.exe");

            BackingStore reader = LoadStore();

            Assert.Equal(string.Empty, reader.fakeExeFileName);
        }

        [Fact]
        public void SaveAfterLoad_DoesNotWipeExistingFakeExeFile()
        {
            SaveStore();
            File.WriteAllText(_fakeExeFile, "fake_game.exe");
            BackingStore reader = LoadStore();

            Assert.True(TestRepositoryFactory.CreateAppSettingsXml(reader).Save());

            Assert.Equal("fake_game.exe", File.ReadAllText(_fakeExeFile).Trim());
        }

        [Fact]
        public void Load_Dto_ComputesLastVersionCheckedNum()
        {
            SaveStore(s => s.lastVersionChecked = "3.9.0");

            BackingStore reader = LoadStore();

            Assert.Equal("3.9.0", reader.lastVersionChecked);
            Assert.True(reader.lastVersionCheckedNum > 0);
            Assert.Equal(VersionUtil.CompileVersionNumberFromString("3.9.0"), reader.lastVersionCheckedNum);
        }

        [Fact]
        public void Load_Dto_InvalidLastVersionChecked_IsCleared()
        {
            SaveStore(s => s.lastVersionChecked = "not-a-version");

            BackingStore reader = LoadStore();

            Assert.Equal(string.Empty, reader.lastVersionChecked);
            Assert.Equal(0UL, reader.lastVersionCheckedNum);
        }

        [Fact]
        public void Load_Dto_ClampsPortNumbersToValidRange()
        {
            SaveStore(s =>
            {
                s.oscServPort = 80;
                s.oscSendPort = 70000;
                s.udpServPort = 1;
            });

            BackingStore reader = LoadStore();

            Assert.Equal(1024, reader.oscServPort);
            Assert.Equal(65535, reader.oscSendPort);
            Assert.Equal(1024, reader.udpServPort);
        }

        [Fact]
        public void Load_Dto_KeepsPortNumbersInsideValidRange()
        {
            SaveStore(s =>
            {
                s.oscServPort = 9000;
                s.udpServPort = 26760;
            });

            BackingStore reader = LoadStore();

            Assert.Equal(9000, reader.oscServPort);
            Assert.Equal(26760, reader.udpServPort);
        }

        [Fact]
        public void Load_Dto_MarksDistanceProfiles()
        {
            SaveStore(s =>
            {
                s.selectedProfile[0] = "My Distance Profile";
                s.selectedProfile[1] = "Regular";
            });

            BackingStore reader = LoadStore();

            Assert.True(reader.distanceProfiles[0]);
            Assert.False(reader.distanceProfiles[1]);
        }
    }
}
