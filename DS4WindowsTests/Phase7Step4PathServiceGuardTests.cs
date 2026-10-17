using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DS4Windows;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step4 W3: 実行ファイルの情報と設定の保存場所（旧 <c>Global</c> の状態と <c>FindConfigLocation</c>／<c>SaveWhere</c>／<c>SaveDefault</c>）を
    /// <see cref="PathService"/> へ移した結果の特性化テストと回帰ガード。
    /// - <see cref="PathService.FindConfigLocation(BackingStore, string, string, Func{string, bool})"/> の 4 通りの分岐（旧 <c>Global.FindConfigLocation</c> の分岐そのまま）。
    /// - <c>Global</c> に残る 4 つの窓口（appdatapath／exedirpath／exelocation／exeversion）が <see cref="PathService"/> の状態を指すこと。
    /// - 削除済みの <c>Global</c> のメンバーが再び宣言されないこと。静的初期化の循環を作らないこと。
    /// 静的な状態（AppDataPathState、FirstRunState ほか）を書き換えるため、同じ状態を使うテストと直列に実行し、必ず元へ戻す。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase7Step4PathServiceGuardTests
    {
        private const string Exe = @"C:\Phase7Exe";
        private const string Roaming = @"C:\Phase7Roaming";

        private static readonly string[] Removed =
        {
            "FindConfigLocation", "SaveWhere", "SaveDefault", "firstRun", "multisavespots", "appDataPpath", "runHotPlug", "fileVersion",
        };

        private readonly ITestOutputHelper _output;

        public Phase7Step4PathServiceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static string StripComments(string text)
        {
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        private string Read(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した: " + relativePath);
                return null;
            }

            return StripComments(File.ReadAllText(path));
        }

        /// <summary>静的な状態を退避して、テストの後で元へ戻す。</summary>
        private sealed class StateScope : IDisposable
        {
            private readonly string _appData = PathService.AppDataPathState;
            private readonly bool _firstRun = PathService.FirstRunState;
            private readonly bool _multi = PathService.MultipleSaveLocationsState;

            public void Dispose()
            {
                PathService.AppDataPathState = _appData;
                PathService.FirstRunState = _firstRun;
                PathService.MultipleSaveLocationsState = _multi;
            }
        }

        private static Func<string, bool> Exists(bool programFolder, bool appData) =>
            path => path == Path.Combine(Exe, "Auto Profiles.xml") ? programFolder
                  : path == Path.Combine(Roaming, "Auto Profiles.xml") ? appData
                  : false;

        // ---- FindConfigLocation の分岐 ----

        [Fact]
        public void FindConfigLocation_BothFolders_MarksFirstRunAndMultipleLocations_AndDoesNotChooseASaveLocation()
        {
            using (new StateScope())
            {
                var config = new BackingStore();
                PathService.AppDataPathState = "untouched";
                PathService.FirstRunState = false;
                PathService.MultipleSaveLocationsState = false;

                PathService.FindConfigLocation(config, Exe, Roaming, Exists(true, true));

                Assert.True(PathService.FirstRunState);
                Assert.True(PathService.MultipleSaveLocationsState);
                Assert.Equal("untouched", PathService.AppDataPathState);
            }
        }

        [Fact]
        public void FindConfigLocation_ProgramFolderOnly_ChoosesTheProgramFolder_AndKeepsTheFlags()
        {
            using (new StateScope())
            {
                var config = new BackingStore();
                PathService.FirstRunState = false;
                PathService.MultipleSaveLocationsState = false;

                PathService.FindConfigLocation(config, Exe, Roaming, Exists(true, false));

                Assert.Equal(Exe, PathService.AppDataPathState);
                Assert.Equal(Exe + "\\Profiles.xml", config.m_Profile);
                Assert.Equal(Exe + "\\Actions.xml", config.m_Actions);
                Assert.Equal(Exe + "\\LinkedProfiles.xml", config.m_linkedProfiles);
                Assert.Equal(Exe + "\\ControllerConfigs.xml", config.m_controllerConfigs);
                Assert.False(PathService.FirstRunState);
                Assert.False(PathService.MultipleSaveLocationsState);
            }
        }

        [Fact]
        public void FindConfigLocation_AppDataOnly_ChoosesAppData_AndKeepsTheFlags()
        {
            using (new StateScope())
            {
                var config = new BackingStore();
                PathService.FirstRunState = true;
                PathService.MultipleSaveLocationsState = true;

                PathService.FindConfigLocation(config, Exe, Roaming, Exists(false, true));

                Assert.Equal(Roaming, PathService.AppDataPathState);
                Assert.Equal(Roaming + "\\Profiles.xml", config.m_Profile);
                // 旧実装は、この分岐でフラグを false に戻さない
                Assert.True(PathService.FirstRunState);
                Assert.True(PathService.MultipleSaveLocationsState);
            }
        }

        [Fact]
        public void FindConfigLocation_NeitherFolder_MarksFirstRun_ResetsMultipleLocations_AndDoesNotChooseASaveLocation()
        {
            using (new StateScope())
            {
                var config = new BackingStore();
                PathService.AppDataPathState = "untouched";
                PathService.FirstRunState = false;
                PathService.MultipleSaveLocationsState = true;

                PathService.FindConfigLocation(config, Exe, Roaming, Exists(false, false));

                Assert.True(PathService.FirstRunState);
                Assert.False(PathService.MultipleSaveLocationsState);
                Assert.Equal("untouched", PathService.AppDataPathState);
            }
        }

        [Fact]
        public void FindConfigLocation_SameFolderForBoth_IsTreatedAsTheProgramFolder()
        {
            using (new StateScope())
            {
                var config = new BackingStore();
                PathService.FirstRunState = false;
                PathService.MultipleSaveLocationsState = false;
                Func<string, bool> both = path => path == Path.Combine(Exe, "Auto Profiles.xml");

                PathService.FindConfigLocation(config, Exe, Exe, both);

                Assert.Equal(Exe, PathService.AppDataPathState);
                Assert.False(PathService.FirstRunState);
                Assert.False(PathService.MultipleSaveLocationsState);
            }
        }

        [Fact]
        public void SaveWhere_UpdatesTheStateAndAllFourSettingsFilePaths()
        {
            using (new StateScope())
            {
                var config = new BackingStore();

                PathService.SaveWhere(config, @"C:\Phase7SaveWhere");

                Assert.Equal(@"C:\Phase7SaveWhere", PathService.AppDataPathState);
                Assert.Equal(@"C:\Phase7SaveWhere\Profiles.xml", config.m_Profile);
                Assert.Equal(@"C:\Phase7SaveWhere\Actions.xml", config.m_Actions);
                Assert.Equal(@"C:\Phase7SaveWhere\LinkedProfiles.xml", config.m_linkedProfiles);
                Assert.Equal(@"C:\Phase7SaveWhere\ControllerConfigs.xml", config.m_controllerConfigs);
            }
        }

        // ---- Global の窓口 ----

        [Fact]
        public void GlobalWindows_PointAtThePathServiceState()
        {
            using (new StateScope())
            {
                string originalExeDir = PathService.ExecutableDirectoryState;
                try
                {
                    PathService.AppDataPathState = @"C:\Phase7Window";
                    Assert.Equal(@"C:\Phase7Window", Global.appdatapath);

                    Global.appdatapath = @"C:\Phase7Window2";
                    Assert.Equal(@"C:\Phase7Window2", PathService.AppDataPathState);

                    Global.exedirpath = @"C:\Phase7ExeWindow";
                    Assert.Equal(@"C:\Phase7ExeWindow", PathService.ExecutableDirectoryState);

                    Assert.Equal(PathService.ExecutableLocationState, Global.exelocation);
                    Assert.Equal(PathService.ExecutableVersionState, Global.exeversion);
                }
                finally
                {
                    PathService.ExecutableDirectoryState = originalExeDir;
                }
            }
        }

        [Fact]
        public void PathService_Instances_ShareTheSameState()
        {
            using (new StateScope())
            {
                PathService.AppDataPathState = @"C:\Phase7Shared";

                Assert.Equal(@"C:\Phase7Shared", new PathService().AppDataPath);
                Assert.Equal(PathService.ExecutableDirectoryState, new PathService().ProgramDirectory);
                Assert.Equal(PathService.ExecutableLocationState, new PathService().ExecutablePath);
            }
        }

        // ---- 回帰ガード ----

        [Fact]
        public void RemovedMembers_DoNotReappearInGlobal()
        {
            string text = Read("DS4Windows/DS4Control/Global.cs");
            if (text == null) return;

            List<string> reappeared = Removed
                .Where(n => Regex.IsMatch(text, @"(?m)^\s*(public|internal|private)?\s*static\s+[\w<>\[\],.? ]+\s+" + Regex.Escape(n) + @"\s*(\(|\{|=>|;|=)"))
                .ToList();
            Assert.True(reappeared.Count == 0, "移管済みのメンバーが Global.cs に再び宣言されている: " + string.Join(", ", reappeared));
        }

        [Fact]
        public void NoSourceFile_ReferencesRemovedMembersThroughGlobal()
        {
            string anchor = SourceFileLocator.Find("DS4Windows/DS4Control/Global.cs");
            if (anchor == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(anchor), "..", ".."));
            string sourceDir = Path.Combine(root, "DS4Windows");
            var offenders = new List<string>();
            foreach (string file in Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories))
            {
                string normalized = file.Replace('\\', '/');
                if (normalized.Contains("/obj/") || normalized.Contains("/bin/")) continue;

                string text = StripComments(File.ReadAllText(file));
                foreach (string name in Removed)
                {
                    if (Regex.IsMatch(text, @"\bGlobal\." + Regex.Escape(name) + @"\b"))
                        offenders.Add(Path.GetRelativePath(sourceDir, file) + ": Global." + name);
                }
            }

            Assert.True(offenders.Count == 0, "移管済みのメンバーを Global 経由で参照している: " + string.Join(", ", offenders));
        }

        [Fact]
        public void PathService_StaticInitialization_DoesNotReferenceGlobal()
        {
            string text = Read("DS4Windows/DS4Control/Services/PathService.cs");
            if (text == null) return;

            // static の初期化子と static メソッドから Global を呼ばない。許可するのは、インスタンスのコンストラクタの Global.store だけ
            // （BackingStore の field 初期化子が Global.appdatapath を読むため、PathService の静的初期化が Global を起こすと循環する）。
            var uses = Regex.Matches(text, @"\bGlobal\.(\w+)").Cast<Match>().Select(m => m.Groups[1].Value).ToList();
            Assert.Equal(new[] { "store" }, uses);
            Assert.Matches(@"_config\s*=\s*config\s*\?\?\s*Global\.store;", text);
        }
    }
}
