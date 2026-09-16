using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xunit;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    public class ProfileActionsSyncTests
    {
        // テスト用のダミーサブ設定クラス
        private class DummyStickSetting : ProfileSubSettingBase
        {
            private int _deadZone;
            public int DeadZone
            {
                get => _deadZone;
                set
                {
                    if (_deadZone != value)
                    {
                        _deadZone = value;
                        RaisePropertyChanged();
                    }
                }
            }
        }

        [Fact]
        public void ProfileSubSettingBase_PropertyChange_ShouldBubbleUpEvent()
        {
            var setting = new DummyStickSetting();
            string reportedProperty = null;
            bool propertyChangedFired = false;

            setting.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(DummyStickSetting.DeadZone))
                    propertyChangedFired = true;
            };

            setting.OnSubPropertyChanged += (propName) =>
            {
                reportedProperty = propName;
            };

            setting.DeadZone = 15;

            Assert.True(propertyChangedFired);
            Assert.Equal(nameof(DummyStickSetting.DeadZone), reportedProperty);
        }

        [Fact]
        public void SpecialActionsListViewModel_ItemActiveChange_ShouldAutoUpdateProfileActionsString()
        {
            var vm = new SpecialActionsListViewModel(0);

            var act1 = new SpecialAction("ActA", "Cross", "Macro", "");
            var act2 = new SpecialAction("ActB", "Circle", "Macro", "");
            var act3 = new SpecialAction("ActC", "Square", "Macro", "");

            var item1 = vm.CreateActionItem(act1, "ActA");
            var item2 = vm.CreateActionItem(act2, "ActB");
            var item3 = vm.CreateActionItem(act3, "ActC");

            vm.ActionCol.Add(item1);
            vm.ActionCol.Add(item2);
            vm.ActionCol.Add(item3);

            // 初期状態はすべて未チェック
            Assert.Equal(string.Empty, vm.ProfileActions);

            // ActA をチェック
            item1.Active = true;
            Assert.Equal("ActA", vm.ProfileActions);

            // ActC をチェック -> スラッシュ区切りで結合されること
            item3.Active = true;
            Assert.Equal("ActA/ActC", vm.ProfileActions);

            // ActA のチェックを解除
            item1.Active = false;
            Assert.Equal("ActC", vm.ProfileActions);
        }

        [Fact]
        public void SpecialActionsListViewModel_LoadProfileActionsString_ShouldRestoreCheckStates()
        {
            var vm = new SpecialActionsListViewModel(0);

            var act1 = new SpecialAction("Jump", "Cross", "Key", "");
            var act2 = new SpecialAction("Fire", "R2", "Key", "");
            var act3 = new SpecialAction("Reload", "Square", "Key", "");

            // ▼ ここでアイテムをコレクションに追加する処理を追加
            vm.ActionCol.Add(vm.CreateActionItem(act1, "Jump"));
            vm.ActionCol.Add(vm.CreateActionItem(act2, "Fire"));
            vm.ActionCol.Add(vm.CreateActionItem(act3, "Reload"));

            // XML から読み込まれた文字列 "Jump/Reload" を反映
            vm.LoadProfileActionsString("Jump/Reload");

            Assert.True(vm.ActionCol[0].Active); // Jump
            Assert.False(vm.ActionCol[1].Active); // Fire
            Assert.True(vm.ActionCol[2].Active); // Reload
            Assert.Equal("Jump/Reload", vm.ProfileActions);
        }
    }
}