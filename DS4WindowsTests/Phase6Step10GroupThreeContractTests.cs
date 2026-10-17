using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Xunit;
using Xunit.Abstractions;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF.DS4Forms.ViewModels.SpecialActions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step10-3-1: グループ3（スペシャルアクション系）の View／ViewModel の Global 直接参照解消に先立って
    /// 追加した契約が、<c>Global.SaveAction</c>／<c>Global.RemoveAction</c> と同じ動作（薄い委譲）であることを検証する。
    /// - ISpecialActionRepository.SaveAction: Actions.xml へ保存し、メモリ上のリストが読み直されること
    /// - ISpecialActionRepository.RemoveActionAndSave: メモリ上のリストとファイルの両方から削除されること
    ///   （メモリ上のリストだけを変えていた旧 RemoveAction は Phase7-Step2 W6 で削除した。別物）
    /// Global 状態を変更するため、保存先とアクションの一覧を必ず元に戻す（Phase6-Status.md §6.4-1）。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase6Step10GroupThreeContractTests
    {
        private readonly ITestOutputHelper _output;

        public Phase6Step10GroupThreeContractTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>Global.store の Actions.xml の保存先と、アクションの一覧を、退避・復元する</summary>
        private sealed class ActionsStoreScope : IDisposable
        {
            private readonly string _originalPath;
            private readonly List<SpecialAction> _originalActions;
            public string Dir { get; }
            public string ActionsPath { get; }

            public ActionsStoreScope()
            {
                Dir = Path.Combine(Path.GetTempPath(), "Phase6Step10_3_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Dir);
                ActionsPath = Path.Combine(Dir, "Actions.xml");

                _originalPath = Global.store.m_Actions;
                _originalActions = new List<SpecialAction>(Global.store.actions);

                Global.store.m_Actions = ActionsPath;
                Global.store.actions.Clear();
                // 空の Actions.xml から始める（存在しない場合は既定のアクションが作られるため）
                TestRepositoryFactory.CreateActionsXml().SaveActions();
            }

            public void Dispose()
            {
                Global.store.m_Actions = _originalPath;
                var list = Global.store.actions;
                list.Clear();
                list.AddRange(_originalActions);
                try { Directory.Delete(Dir, true); } catch { }
            }
        }

        private static string[] ActionNamesInFile(string path)
        {
            var doc = new XmlDocument();
            doc.Load(path);
            return doc.SelectNodes("/Actions/Action")
                .Cast<XmlNode>()
                .Select(n => n.Attributes?["Name"]?.Value ?? n.SelectSingleNode("Name")?.InnerText)
                .Where(n => n != null)
                .ToArray();
        }

        [Fact]
        public void SaveAction_AddsNewAction_SavesFileAndNotifies()
        {
            using var scope = new ActionsStoreScope();
            var repo = TestRepositoryFactory.CreateSpecialActionRepository();
            int notified = 0;
            repo.ActionsChanged += (s, e) => notified++;

            repo.SaveAction("Step10_3_Macro", "Cross", 2, "C:/a.exe?0", false);

            Assert.True(repo.ActionExists("Step10_3_Macro"));
            SpecialAction action = repo.GetAction("Step10_3_Macro");
            Assert.Equal("Cross", action.controls);
            Assert.Equal("C:/a.exe", action.details);
            Assert.Contains("Step10_3_Macro", ActionNamesInFile(scope.ActionsPath));
            Assert.True(notified > 0);
        }

        [Fact]
        public void SaveAction_Edit_ReplacesExistingAction()
        {
            using var scope = new ActionsStoreScope();
            var repo = TestRepositoryFactory.CreateSpecialActionRepository();
            repo.SaveAction("Step10_3_Edit", "Cross", 2, "C:/a.exe?0", false);

            repo.SaveAction("Step10_3_Edit", "Circle", 2, "C:/b.exe?0", true);

            Assert.Equal(1, repo.ActionList.Count(a => a.name == "Step10_3_Edit"));
            SpecialAction action = repo.GetAction("Step10_3_Edit");
            Assert.Equal("Circle", action.controls);
            Assert.Equal("C:/b.exe", action.details);
        }

        // Phase7-Step10c W2: SaveAction_MatchesGlobalSaveAction は削除した（比較対象の Global.SaveAction を、SpecialActionRepository の
        // プライベートメソッドへ移したため）。保存→読み直しの結果は、上の SaveAction_AddsNewAction_SavesFileAndNotifies ほかが確認する。

        [Fact]
        public void RemoveActionAndSave_RemovesFromListAndFile()
        {
            using var scope = new ActionsStoreScope();
            var repo = TestRepositoryFactory.CreateSpecialActionRepository();
            repo.SaveAction("Step10_3_Remove", "Cross", 2, "C:/a.exe?0", false);
            repo.SaveAction("Step10_3_Keep", "Circle", 2, "C:/d.exe?0", false);
            Assert.Contains("Step10_3_Remove", ActionNamesInFile(scope.ActionsPath));

            bool existed = repo.RemoveActionAndSave("Step10_3_Remove");

            Assert.True(existed);
            Assert.False(repo.ActionExists("Step10_3_Remove"));
            Assert.True(repo.ActionExists("Step10_3_Keep"));
            string[] names = ActionNamesInFile(scope.ActionsPath);
            Assert.DoesNotContain("Step10_3_Remove", names);
            Assert.Contains("Step10_3_Keep", names);
        }

        [Fact]
        public void RemoveActionAndSave_ReturnsFalseForUnknownAction()
        {
            using var scope = new ActionsStoreScope();
            var repo = TestRepositoryFactory.CreateSpecialActionRepository();
            repo.SaveAction("Step10_3_Keep", "Circle", 2, "C:/d.exe?0", false);

            Assert.False(repo.RemoveActionAndSave("Step10_3_Missing"));
            Assert.True(repo.ActionExists("Step10_3_Keep"));
        }

        // ---- Phase6-Step10-3c: ボタン（Button）の割り当てでの SwitchMode の保存・読込、戻った直後の表示 ----

        private static DS4ControlSettings ButtonSettings(X360Controls button)
        {
            var settings = new DS4ControlSettings(DS4Controls.None);
            settings.actionType = DS4ControlSettings.ActionType.Button;
            settings.action.actionBtn = button;
            return settings;
        }

        private static PressKeyViewModel CreatePressKeyViewModel(ISpecialActionRepository repo) =>
            new PressKeyViewModel(repo, TestRepositoryFactory.CreateProfileSettingsService(), TestRepositoryFactory.CreateOutputSlotService());

        [Theory]
        [InlineData("Toggle", SpecialAction.KeyButtonSwitchModeEnum.Toggle)]
        [InlineData("Press", SpecialAction.KeyButtonSwitchModeEnum.Press)]
        public void ButtonAction_SwitchMode_SurvivesSaveAndReload(string mode, SpecialAction.KeyButtonSwitchModeEnum expected)
        {
            using var scope = new ActionsStoreScope();
            var repo = TestRepositoryFactory.CreateSpecialActionRepository();
            var vm = CreatePressKeyViewModel(repo);
            vm.SwitchMode = mode;
            vm.ReadSettings(ButtonSettings(X360Controls.A));

            vm.SaveAction(new SpecialAction("Step10_3c_Btn", "Cross", "Program", "C:/a.exe"), false);

            // SaveAction は Actions.xml の読み直しまで行うため、取得できる内容は、保存・読み込みを経たもの
            SpecialAction saved = repo.GetAction("Step10_3c_Btn");
            Assert.Equal(SpecialAction.ActionTypeId.Button, saved.typeID);
            Assert.Equal(((int)X360Controls.A).ToString(), saved.details);
            Assert.True(saved.KeyButtonSwitchMode.HasValue);
            Assert.Equal(expected, saved.KeyButtonSwitchMode.Value);

            // 編集画面を開き直したときの SwitchMode
            var reopened = CreatePressKeyViewModel(repo);
            reopened.LoadAction(saved);
            Assert.Equal(mode, reopened.SwitchMode);
        }

        [Fact]
        public void ButtonAction_WithoutSwitchModeInFile_LoadsAsPress()
        {
            // SwitchMode を持たない従来の Actions.xml（Button）は、従来どおり Press として読み込む
            using var scope = new ActionsStoreScope();
            var repo = TestRepositoryFactory.CreateSpecialActionRepository();
            File.WriteAllText(scope.ActionsPath,
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><Actions>" +
                "<Action Name=\"Legacy\"><Trigger>Cross</Trigger><Type>Button</Type><Details>1</Details></Action></Actions>");
            TestRepositoryFactory.CreateActionsXml().LoadActions();

            SpecialAction legacy = repo.GetAction("Legacy");
            Assert.Equal(SpecialAction.ActionTypeId.Button, legacy.typeID);

            var vm = CreatePressKeyViewModel(repo);
            vm.LoadAction(legacy);
            Assert.Equal("Press", vm.SwitchMode);
        }

        [Fact]
        public void ReadSettings_ForButton_RaisesDescribeTextPropertyChanged()
        {
            var vm = CreatePressKeyViewModel(TestRepositoryFactory.CreateSpecialActionRepository());
            var changed = new List<string>();
            vm.PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            vm.ReadSettings(ButtonSettings(X360Controls.A));

            Assert.Contains(nameof(PressKeyViewModel.DescribeText), changed);
            Assert.NotEqual("Select a Key", vm.DescribeText);
        }
    }
}
