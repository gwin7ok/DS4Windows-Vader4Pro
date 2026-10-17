using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step8-4: ProfileEditor（View）から ViewModel へ移したバックエンド操作を、UI なしで検証する。
    /// - <see cref="SpecialActionsListViewModel.CommitEnabledActionsToProfile"/>: 有効なアクション名を
    ///   編集スロットの ProfileActions へ反映する（保存時・チェック切り替え時の共通処理）。
    /// - <see cref="ProfileSettingsViewModel"/> の SaveProfile／LoadBlankProfile／RefreshCustomsFlags／
    ///   RefreshExtraProfileInfo: 編集スロットだけを対象にし、コントローラーのスロットへの再適用をしないこと。
    ///   実ファイル・実機が必要な副作用があるため、委譲先の呼び出し形をソース走査で固定する。
    /// 変更した Global 状態は必ず元に戻す（Phase6-Step2 の教訓）。
    /// </summary>
    public class ProfileEditorBackendViewModelTests
    {
        private readonly ITestOutputHelper _output;

        public ProfileEditorBackendViewModelTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void CommitEnabledActionsToProfile_WritesEnabledNamesToEditSlotOnly()
        {
            const int editSlot = DS4Windows.ControllerLimits.TestProfileIndex;
            // 既存テスト（ProfileSettingsServiceSubSettingsTests.ProfileActions_GetSet_ShouldPersistAndNotify）が
            // Global.store.profileActions を 8 要素の配列に置き換えたまま戻さないため、実行順によって
            // Global.store.profileActions の長さが変わる。独立した 9 要素の配列に差し替えて検証し、終了時に戻す。
            BackingStore store = Global.store;
            List<string>[] originalArray = store.profileActions;
            List<string>[] profileActions = new List<string>[DS4Windows.ControllerLimits.TestProfileItemCount];
            store.profileActions = profileActions;
            try
            {
                var sentinel = new List<string> { "keep" };
                profileActions[0] = sentinel;

                var vm = new SpecialActionsListViewModel(editSlot, TestRepositoryFactory.CreateSpecialActionRepository(), TestRepositoryFactory.CreateProfileRepository());
                var itemA = vm.CreateActionItem(new SpecialAction("ActA", "Cross", "Macro", ""), "ActA");
                var itemB = vm.CreateActionItem(new SpecialAction("ActB", "Circle", "Macro", ""), "ActB");
                var itemC = vm.CreateActionItem(new SpecialAction("ActC", "Square", "Macro", ""), "ActC");
                vm.ActionCol.Add(itemA);
                vm.ActionCol.Add(itemB);
                vm.ActionCol.Add(itemC);

                itemA.Active = true;
                itemC.Active = true;

                vm.CommitEnabledActionsToProfile();

                Assert.Equal(new[] { "ActA", "ActC" }, profileActions[editSlot]);
                // コントローラーのスロットには触れない
                Assert.Same(sentinel, profileActions[0]);
                Assert.Equal(new[] { "keep" }, profileActions[0]);

                // チェックを外して再度反映すると、編集スロットの内容が追従する
                itemA.Active = false;
                vm.CommitEnabledActionsToProfile();
                Assert.Equal(new[] { "ActC" }, profileActions[editSlot]);
            }
            finally
            {
                store.profileActions = originalArray;
            }
        }

        [Fact]
        public void ProfileSettingsViewModel_BackendOperations_TargetOnlyTheEditSlotAndDoNotApplyToControllers()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Forms/ViewModels/ProfileSettingsViewModel.cs");
            if (path == null)
            {
                _output.WriteLine("ProfileSettingsViewModel.cs が見つからないため検査を省略した。");
                return;
            }

            string text = File.ReadAllText(path);

            Assert.Matches(new Regex(@"public bool SaveProfile\(string profileName\)\s*\{\s*return profileRepo\.SaveProfile\(device, profileName\);\s*\}"), text);
            Assert.Matches(new Regex(@"public void LoadBlankProfile\(ControlService control\)\s*\{\s*presetLoader\.LoadBlank\(device, false, control, false\);\s*\}"), text);
            Assert.Matches(new Regex(@"public void RefreshCustomsFlags\(\)\s*\{\s*profileRepo\.CacheProfileCustomsFlags\(device\);\s*\}"), text);
            Assert.Matches(new Regex(@"public void RefreshExtraProfileInfo\(\)\s*\{\s*profileRepo\.CacheExtraProfileInfo\(device\);\s*\}"), text);

            // 保存の一連は、コントローラーのスロットへの再適用をしない（再適用は ProfileSaved を受けた MainWindow の責務）
            int start = text.IndexOf("public bool SaveProfile(string profileName)", StringComparison.Ordinal);
            string body = text.Substring(start, 200);
            Assert.DoesNotContain("ApplyProfile", body);
        }

        [Fact]
        public void ProfileEditor_UsesViewModelForBackendOperations()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Forms/ProfileEditor.xaml.cs");
            if (path == null)
            {
                _output.WriteLine("ProfileEditor.xaml.cs が見つからないため検査を省略した。");
                return;
            }

            string text = Regex.Replace(File.ReadAllText(path), @"//[^\r\n]*", string.Empty);

            // View から repository を直接呼ばず、ViewModel のメソッドを使う
            Assert.DoesNotContain("profileRepository.SaveProfile(", text);
            Assert.DoesNotContain("profileRepository.LoadBlankProfile(", text);
            Assert.DoesNotContain("profileRepository.CacheProfileCustomsFlags(", text);
            Assert.DoesNotContain("profileRepository.CacheExtraProfileInfo(", text);
            Assert.Contains("profileSettingsVM.LoadBlankProfile(controlService)", text);
            Assert.Contains("profileSettingsVM.SaveProfile(profileName)", text);
            Assert.Contains("specialActionsVM.CommitEnabledActionsToProfile()", text);
        }
    }
}
