/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    public class BindingWindowViewModel
    {
        private int deviceNum;
        private bool use360Mode;
        private DS4ControlSettings settings;
        private OutBinding currentOutBind;
        private OutBinding shiftOutBind;
        private bool showShift;
        private bool rumbleActive;

        public bool Using360Mode
        {
            get => use360Mode;
        }
        public int DeviceNum { get => deviceNum; }
        public OutBinding CurrentOutBind { get => currentOutBind; }
        public OutBinding ShiftOutBind { get => shiftOutBind; }
        public OutBinding ActionBinding { get; set; }

        public bool ShowShift { get => showShift; set => showShift = value; }
        public bool RumbleActive { get => rumbleActive; set => rumbleActive = value; }
        public DS4ControlSettings Settings { get => settings; }

        // Phase6-Step7b: 設定の読み書き先（DeviceNum＝編集スロット）とは別に、ランブルテスト・ライトバーの
        // プレビューで実際に使うコントローラーのスロット番号を持つ。-1 は「実機なし」
        // （プロファイル一覧から開いた場合）。詳細は docs-forDIMG/MadeByAgent/Phase6-Step7b-Plan.md §2.1
        // Phase6-Step10-2-4b: Global.RefreshActionAlias の置換先（必須引数）
        private readonly IProfileSettingsService profileSettingsService;
        private readonly int targetDevice;
        public int TargetDevice { get => targetDevice; }

        /// <summary>
        /// 実機（targetDevice）が指定され、かつ有効なコントローラースロットの範囲内であるか。
        /// Phase6-Step7b: 実機に触れる処理はこの判定を使う（編集スロット DeviceNum ではない）。
        /// </summary>
        public bool HasTargetDevice =>
            targetDevice >= 0 && targetDevice < ControlService.CURRENT_DS4_CONTROLLER_LIMIT;

        public BindingWindowViewModel(int deviceNum, DS4ControlSettings settings,
            IProfileSettingsService profileSettingsService, IOutputSlotService outputSlotService,
            int targetDevice = -1)
        {
            this.profileSettingsService = profileSettingsService ?? throw new ArgumentNullException(nameof(profileSettingsService));
            if (outputSlotService == null) throw new ArgumentNullException(nameof(outputSlotService));
            this.deviceNum = deviceNum;
            this.targetDevice = targetDevice;
            use360Mode = outputSlotService.OutDevTypeTemp[deviceNum] == OutContType.X360;
            this.settings = settings;
            currentOutBind = new OutBinding();
            shiftOutBind = new OutBinding();
            shiftOutBind.shiftBind = true;
            PopulateCurrentBinds();
        }

        public void PopulateCurrentBinds()
        {
            DS4ControlSettings setting = settings;
            bool sc = setting.keyType.HasFlag(DS4KeyType.ScanCode);
            bool toggle = setting.keyType.HasFlag(DS4KeyType.Toggle);
            currentOutBind.input = setting.control;
            shiftOutBind.input = setting.control;
            if (setting.actionType != DS4ControlSettings.ActionType.Default)
            {
                switch(setting.actionType)
                {
                    case DS4ControlSettings.ActionType.Button:
                        currentOutBind.outputType = OutBinding.OutType.Button;
                        currentOutBind.control = (X360Controls)setting.action.actionBtn;
                        break;
                    case DS4ControlSettings.ActionType.Default:
                        currentOutBind.outputType = OutBinding.OutType.Default;
                        break;
                    case DS4ControlSettings.ActionType.Key:
                        currentOutBind.outputType = OutBinding.OutType.Key;
                        currentOutBind.outkey = setting.action.actionKey;
                        currentOutBind.hasScanCode = sc;
                        currentOutBind.toggle = toggle;
                        break;
                    case DS4ControlSettings.ActionType.Macro:
                        currentOutBind.outputType = OutBinding.OutType.Macro;
                        currentOutBind.macro = (int[])setting.action.actionMacro;
                        currentOutBind.macroType = settings.keyType;
                        currentOutBind.hasScanCode = sc;
                        break;
                }
            }
            else
            {
                currentOutBind.outputType = OutBinding.OutType.Default;
            }

            if (!string.IsNullOrEmpty(setting.extras))
            {
                currentOutBind.ParseExtras(setting.extras);
            }

            if (setting.shiftActionType != DS4ControlSettings.ActionType.Default)
            {
                sc = setting.shiftKeyType.HasFlag(DS4KeyType.ScanCode);
                toggle = setting.shiftKeyType.HasFlag(DS4KeyType.Toggle);
                shiftOutBind.shiftTrigger = setting.shiftTrigger;
                switch (setting.shiftActionType)
                {
                    case DS4ControlSettings.ActionType.Button:
                        shiftOutBind.outputType = OutBinding.OutType.Button;
                        shiftOutBind.control = (X360Controls)setting.shiftAction.actionBtn;
                        break;
                    case DS4ControlSettings.ActionType.Default:
                        shiftOutBind.outputType = OutBinding.OutType.Default;
                        break;
                    case DS4ControlSettings.ActionType.Key:
                        shiftOutBind.outputType = OutBinding.OutType.Key;
                        shiftOutBind.outkey = setting.shiftAction.actionKey;
                        shiftOutBind.hasScanCode = sc;
                        shiftOutBind.toggle = toggle;
                        break;
                    case DS4ControlSettings.ActionType.Macro:
                        shiftOutBind.outputType = OutBinding.OutType.Macro;
                        shiftOutBind.macro = (int[])setting.shiftAction.actionMacro;
                        shiftOutBind.macroType = setting.shiftKeyType;
                        shiftOutBind.hasScanCode = sc;
                        break;
                }
            }

            if (!string.IsNullOrEmpty(setting.shiftExtras))
            {
                shiftOutBind.ParseExtras(setting.shiftExtras);
            }

            if (settings.LightbarMacro is not null) currentOutBind.LightbarMacro = settings.LightbarMacro;
        }

        public void PrepareSaveMacro(OutBinding bind, bool shiftBind=false)
        {
            DS4ControlSettings setting = settings;

            if (!shiftBind)
            {
                bind.outputType = OutBinding.OutType.Macro;
                bind.macro = (int[])setting.action.actionMacro;
                bind.macroType = settings.keyType;
            }
            else
            {
                bind.outputType = OutBinding.OutType.Macro;
                bind.macro = (int[])setting.shiftAction.actionMacro;
                bind.macroType = setting.shiftKeyType;
            }
        }

        public void WriteBinds()
        {
            currentOutBind.WriteBind(settings, profileSettingsService);
            shiftOutBind.WriteBind(settings, profileSettingsService);
        }

        // Phase6-Step7b: ライトバー強制色のプレビューは、編集スロット（DeviceNum）ではなく実機（targetDevice）に対して行う
        public void StartForcedColor(Color color)
        {
            if (HasTargetDevice)
            {
                DS4Color dcolor = new DS4Color() { red = color.R, green = color.G, blue = color.B };
                DS4LightBar.forcedColor[targetDevice] = dcolor;
                DS4LightBar.forcedFlash[targetDevice] = 0;
                DS4LightBar.forcelight[targetDevice] = true;
            }
        }

        public void EndForcedColor()
        {
            if (HasTargetDevice)
            {
                DS4LightBar.forcedColor[targetDevice] = new DS4Color(0, 0, 0);
                DS4LightBar.forcedFlash[targetDevice] = 0;
                DS4LightBar.forcelight[targetDevice] = false;
            }
        }

        public void UpdateForcedColor(Color color)
        {
            if (HasTargetDevice)
            {
                DS4Color dcolor = new DS4Color() { red = color.R, green = color.G, blue = color.B };
                DS4LightBar.forcedColor[targetDevice] = dcolor;
                DS4LightBar.forcedFlash[targetDevice] = 0;
                DS4LightBar.forcelight[targetDevice] = true;
            }
        }
    }
}
