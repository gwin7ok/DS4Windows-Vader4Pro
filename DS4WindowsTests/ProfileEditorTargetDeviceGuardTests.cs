using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step7b-3: プロファイル編集画面の「編集内容の即時反映」を廃止した構造を固定する回帰ガード。
    /// - 編集スロットは常に作業スロット（Global.TEST_PROFILE_INDEX）で、呼び出し元（MainWindow）は編集スロットを指定しない。
    /// - キャンセルはコントローラーのスロットへプロファイルを読み込み直さない。
    /// - 適用は実機（targetDevice）へ ApplyProfileToSlot する。
    /// - 実機に触れるサブ画面・ViewModel の生成には targetDevice を渡す（省略可能な引数の渡し忘れの検出）。
    /// - 到達しなくなった即時フック（決定7＝H1）と ProfileEditor.DeviceNum（決定5＝D1）が再混入しない。
    /// `ProfileEditor` は WPF の UserControl で単体駆動が困難なため、ソース走査で担保する
    /// （Phase6-Step7b-Plan.md §3 Step7b-3）。ソースファイルが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class ProfileEditorTargetDeviceGuardTests
    {
        private readonly ITestOutputHelper _output;

        public ProfileEditorTargetDeviceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private string ReadSourceWithoutComments(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine($"{relativePath} が見つからないため検査を省略した。");
                return null;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        private string ReadRawSource(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine($"{relativePath} が見つからないため検査を省略した。");
                return null;
            }

            return File.ReadAllText(path);
        }

        /// <summary>宣言の先頭から、対応する閉じ波括弧までのメソッド全体を返す。</summary>
        private static string ExtractMethod(string text, string marker)
        {
            int start = text.IndexOf(marker, StringComparison.Ordinal);
            Assert.True(start >= 0, $"{marker} が見つからない（メソッド名・修飾子の変更？ガードの見直しが必要）");

            int open = text.IndexOf('{', start);
            Assert.True(open >= 0, $"{marker} の本体が見つからない");

            int depth = 0;
            for (int i = open; i < text.Length; i++)
            {
                if (text[i] == '{') depth++;
                else if (text[i] == '}')
                {
                    depth--;
                    if (depth == 0) return text.Substring(start, i - start + 1);
                }
            }

            Assert.True(false, $"{marker} の閉じ波括弧が見つからない");
            return null;
        }

        /// <summary>`呼び出しの先頭(` から対応する `)` までの引数部分を、出現順にすべて返す。</summary>
        private static string[] ExtractCallArguments(string text, string callPrefix)
        {
            var result = new System.Collections.Generic.List<string>();
            int index = 0;
            while ((index = text.IndexOf(callPrefix, index, StringComparison.Ordinal)) >= 0)
            {
                int open = index + callPrefix.Length - 1;
                int depth = 0;
                for (int i = open; i < text.Length; i++)
                {
                    if (text[i] == '(') depth++;
                    else if (text[i] == ')')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            result.Add(text.Substring(open + 1, i - open - 1));
                            break;
                        }
                    }
                }
                index = open + 1;
            }
            return result.ToArray();
        }

        [Fact]
        public void ProfileEditor_EditSlotIsAlwaysTheWorkSlot()
        {
            string text = ReadSourceWithoutComments("DS4Windows/DS4Forms/ProfileEditor.xaml.cs");
            if (text == null) return;

            Assert.Contains("private readonly int deviceNum = Global.TEST_PROFILE_INDEX;", text);

            // フィールド初期化子以外で deviceNum へ代入していない（== や ローカル変数の宣言は対象外）
            int assignments = Regex.Matches(text, @"(?<![\w.])deviceNum\s*=(?!=)").Count;
            Assert.Equal(1, assignments);

            Assert.DoesNotContain("deviceNum == 8", text);
            Assert.DoesNotContain("deviceNum < ControlService.CURRENT_DS4_CONTROLLER_LIMIT", text);
            Assert.DoesNotContain("InterimTargetDeviceFor", text);
        }

        [Fact]
        public void ProfileEditor_CancelDoesNotReloadControllerSlot()
        {
            string text = ReadSourceWithoutComments("DS4Windows/DS4Forms/ProfileEditor.xaml.cs");
            if (text == null) return;

            string body = ExtractMethod(text, "private void CancelBtn_Click(");
            Assert.DoesNotContain("LoadProfile(", body);
            Assert.DoesNotContain("HaltReportingRunAction", body);
        }

        /// <summary>
        /// 決定8: Save と Apply の違いは画面を閉じるかどうかだけで、どちらも ProfileSaved を通知し、
        /// 再適用は MainWindow.SyncProfileListAndControllers（そのプロファイルを使っているスロットだけ）に任せる。
        /// 編集画面が特定のコントローラーへ直接 ApplyProfileToSlot すると、新規作成したプロファイルで
        /// そのコントローラーが切り替わってしまう（Phase6-Step7b-Plan.md §2A 決定8）。
        /// </summary>
        [Fact]
        public void ProfileEditor_SaveAndApplyBothDelegateReapplyToMainWindow()
        {
            string text = ReadSourceWithoutComments("DS4Windows/DS4Forms/ProfileEditor.xaml.cs");
            if (text == null) return;

            string body = ExtractMethod(text, "private bool ExecuteSaveOrApply(");
            Assert.DoesNotContain("ApplyProfileToSlot(", body);
            Assert.DoesNotContain("ApplyProfile(", body);
            // ProfileSaved の通知が isApply で分岐していない（1 箇所で無条件に通知する）
            Assert.Single(Regex.Matches(body, @"ProfileSaved\?\.Invoke\("));
            Assert.DoesNotContain("if (!isApply)", body);
            // 保存は作業スロットから行う
            Assert.Contains("SaveProfile(deviceNum,", body);

            // ProfileEditor のどこからも、コントローラーのスロットへ直接適用しない
            Assert.DoesNotContain("ApplyProfileToSlot(", text);

            string saveBtn = ExtractMethod(text, "private void SaveBtn_Click(");
            Assert.Contains("ExecuteSaveOrApply(isApply: false)", saveBtn);
            Assert.Contains("Close()", saveBtn);
            string applyBtn = ExtractMethod(text, "private void ApplyBtn_Click(");
            Assert.Contains("ExecuteSaveOrApply(isApply: true)", applyBtn);
            Assert.DoesNotContain("Close()", applyBtn);
        }

        /// <summary>
        /// 決定8: 保存・適用後の再適用は、そのプロファイル名を使っているスロットに限る
        /// （新規作成したプロファイルは、どのコントローラーにも適用されない）。
        /// </summary>
        [Fact]
        public void MainWindow_ReappliesOnlyToSlotsUsingTheSavedProfile()
        {
            string text = ReadSourceWithoutComments("DS4Windows/DS4Forms/MainWindow.xaml.cs");
            if (text == null) return;

            string saved = ExtractMethod(text, "private void Editor_ProfileSaved(");
            Assert.Contains("SyncProfileListAndControllers(oldProfile: profile, newProfile: null)", saved);

            string sync = ExtractMethod(text, "private void SyncProfileListAndControllers(");
            Assert.Contains("string.Equals(activeProfiles[i], targetProfile, StringComparison.CurrentCultureIgnoreCase)", sync);
            Assert.Contains("Global.ApplyProfileToSlot(i, activeProfiles[i], ProfileChangeSource.Manual)", sync);
        }

        [Fact]
        public void ProfileEditor_PassesTargetDeviceToControllerFeatures()
        {
            string text = ReadSourceWithoutComments("DS4Windows/DS4Forms/ProfileEditor.xaml.cs");
            if (text == null) return;

            var checks = new (string Call, int MinCount)[]
            {
                ("new BindingWindow(", 7),
                ("new SpecialActionEditor(", 2),
                ("CreateProfileSettingsViewModel(", 1),
                ("new ProfileSettingsViewModel(", 1),
            };

            foreach (var (call, minCount) in checks)
            {
                string[] args = ExtractCallArguments(text, call);
                Assert.True(args.Length >= minCount, $"{call} の呼び出しが {minCount} 件未満（{args.Length} 件）");
                foreach (string a in args)
                {
                    Assert.True(a.Contains("targetDevice"), $"{call}{a}) に targetDevice が渡されていない");
                }
            }

            // スティック再校正は、実機がなければ不可とし、ウィンドウには実機（targetDevice）を渡す
            // （StickCalibrationWindow は型を省略した new(...) で生成している）
            string calibrate = ExtractMethod(text, "private void CalibrateStick_OnClick(");
            Assert.Contains("if (!HasTargetDevice)", calibrate);
            Assert.Contains("new(stick, targetDevice, profileSettingsVM)", calibrate);
        }

        [Fact]
        public void ProfileEditor_RemovedCodeIsNotReintroduced()
        {
            string text = ReadSourceWithoutComments("DS4Windows/DS4Forms/ProfileEditor.xaml.cs");
            if (text == null) return;

            // 決定5＝D1: 常に作業スロットを返すだけになった DeviceNum は削除
            Assert.DoesNotContain("public int DeviceNum", text);
            // 決定7＝H1: 到達しなくなった即時フック
            Assert.DoesNotContain("GyroOutModeCombo_SelectionChanged", text);
            Assert.DoesNotContain("FrictionUD_ValueChanged", text);
            Assert.DoesNotContain("touchPad[deviceNum]", text);

            string xaml = ReadRawSource("DS4Windows/DS4Forms/ProfileEditor.xaml");
            if (xaml != null)
            {
                Assert.DoesNotContain("FrictionUD_ValueChanged", xaml);
                Assert.DoesNotContain("GyroOutModeCombo_SelectionChanged", xaml);
            }
        }

        [Fact]
        public void ProfileSettingsViewModel_InstantHooksAreRemoved()
        {
            string text = ReadSourceWithoutComments("DS4Windows/DS4Forms/ViewModels/ProfileSettingsViewModel.cs");
            if (text == null) return;

            // 決定7＝H1: 編集スロット（device）で実機の状態を即時にリセットする処理を持たない
            Assert.DoesNotContain("touchPad[device]", text);
            Assert.DoesNotContain("deltaAccelProcessors[device]", text);
        }

        [Fact]
        public void MainWindow_DoesNotChooseTheEditSlot()
        {
            string text = ReadSourceWithoutComments("DS4Windows/DS4Forms/MainWindow.xaml.cs");
            if (text == null) return;

            string[] calls = ExtractCallArguments(text, "ShowProfileEditor(");
            // 宣言 1 件＋呼び出し 5 件（Edit、New Profile、一覧の Edit・New・ダブルクリック）
            Assert.True(calls.Length >= 6, $"ShowProfileEditor( の出現が想定より少ない（{calls.Length} 件）");
            foreach (string a in calls)
            {
                Assert.False(a.Contains("TEST_PROFILE_INDEX"),
                    $"ShowProfileEditor({a}) に編集スロットが渡されている（編集スロットは ProfileEditor 内部で決める）");
            }

            Assert.Contains("new ProfileEditor(targetDevice)", text);
            Assert.Contains("editor.Reload(entity)", text);
        }

        [Fact]
        public void SubWindows_PassTargetDeviceDown()
        {
            var checks = new (string File, string Call)[]
            {
                ("DS4Windows/DS4Forms/BindingWindow.xaml.cs", "new RecordBox("),
                ("DS4Windows/DS4Forms/BindingWindow.xaml.cs", "new BindingWindowViewModel("),
                ("DS4Windows/DS4Forms/RecordBoxWindow.xaml.cs", "new RecordBox("),
                ("DS4Windows/DS4Forms/RecordBox.xaml.cs", "CreateRecordBoxViewModel("),
                ("DS4Windows/DS4Forms/RecordBox.xaml.cs", "new RecordBoxViewModel("),
                ("DS4Windows/DS4Forms/SpecialActionEditor.xaml.cs", "new RecordBoxWindow("),
                ("DS4Windows/DS4Forms/SpecialActionEditor.xaml.cs", "new BindingWindow("),
            };

            foreach (var group in checks.GroupBy(c => c.File))
            {
                string text = ReadSourceWithoutComments(group.Key);
                if (text == null) continue;

                foreach (var (_, call) in group)
                {
                    string[] args = ExtractCallArguments(text, call);
                    Assert.True(args.Length >= 1, $"{group.Key} に {call} が見つからない");
                    foreach (string a in args)
                    {
                        Assert.True(a.Contains("targetDevice") || a.Contains("TargetDevice"),
                            $"{group.Key}: {call}{a}) に targetDevice が渡されていない");
                    }
                }
            }
        }
    }
}
