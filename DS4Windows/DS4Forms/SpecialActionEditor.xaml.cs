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

using DS4WinWPF.DS4Forms.ViewModels;
using DS4WinWPF.DS4Forms.ViewModels.SpecialActions;
using Microsoft.Win32;
using DS4Windows;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DS4WinWPF.DS4Forms
{
    /// <summary>
    /// Interaction logic for SpecialActionEditor.xaml
    /// </summary>
    public partial class SpecialActionEditor : UserControl
    {
        private List<CheckBox> triggerBoxes;
        private List<CheckBox> unloadTriggerBoxes;

        private SpecialActEditorViewModel specialActVM;
        private MacroViewModel macroActVM;
        private LaunchProgramViewModel launchProgVM;
        private LoadProfileViewModel loadProfileVM;
        private PressKeyViewModel pressKeyVM;
        private SpecialActionViewModel disconnectBtVM;
        private CheckBatteryViewModel checkBatteryVM;
        private MultiActButtonViewModel multiActButtonVM;
        private SpecialActionViewModel saSteeringWheelVM;
        private SpecialActionViewModel calibrateGyroVM;

        public event EventHandler Cancel;
        public delegate void SaveHandler(object sender, string actionName);
        public event SaveHandler Saved;

        public SpecialActionEditor(int deviceNum, ProfileList profileList,
            DS4Windows.SpecialAction specialAction = null)
        {
            InitializeComponent();

            triggerBoxes = new List<CheckBox>()
            {
                crossTrigCk, circleTrigCk, squareTrigCk, triangleTrigCk,
                optionsTrigCk, shareTrigCk, upTrigCk, downTrigCk,
                leftTrigCk, rightTrigCk, psTrigCk, muteTrigCk, l1TrigCk,
                r1TrigCk, l2TrigCk, l2FullPullTrigCk, r2TrigCk, r2TrigFullPullCk, l3TrigCk,
                r3TrigCk, fnLTrigCk, fnRTrigCk, bLPTrigCk, bRPTrigCk,
                leftTouchTrigCk, upperTouchTrigCk, multitouchTrigCk,
                rightTouchTrigCk, lsuTrigCk, lsdTrigCk, lslTrigCk,
                lsrTrigCk, rsuTrigCk, rsdTrigCk, rslTrigCk,
                rsrTrigCk, swipeUpTrigCk, swipeDownTrigCk, swipeLeftTrigCk,
                swipeRightTrigCk, tiltUpTrigCk, tiltDownTrigCk, tiltLeftTrigCk,
                tiltRightTrigCk,touchStartedTrigCk,touchEndedTrigCk
            };

            unloadTriggerBoxes = new List<CheckBox>()
            {
                unloadCrossTrigCk, unloadCircleTrigCk, unloadSquareTrigCk, unloadTriangleTrigCk,
                unloadOptionsTrigCk, unloadShareTrigCk, unloadUpTrigCk, unloadDownTrigCk,
                unloadLeftTrigCk, unloadRightTrigCk, unloadPsTrigCk, unloadMuteTrigCk, unloadL1TrigCk,
                unloadR1TrigCk, unloadL2TrigCk, unloadL2FullPullTrigCk, unloadR2TrigCk, unloadR2FullPullTrigCk, unloadL3TrigCk,
                unloadR3TrigCk, unloadfnLTrigCk, unloadfnRTrigCk, unloadbLPTrigCk, unloadbRPTrigCk,
                unloadLeftTouchTrigCk, unloadUpperTouchTrigCk, unloadMultitouchTrigCk,
                unloadRightTouchTrigCk, unloadLsuTrigCk, unloadLsdTrigCk, unloadLslTrigCk,
                unloadLsrTrigCk, unloadRsuTrigCk, unloadRsdTrigCk, unloadRslTrigCk,
                unloadRsrTrigCk, unloadSwipeUpTrigCk, unloadSwipeDownTrigCk, unloadSwipeLeftTrigCk,
                unloadSwipeRightTrigCk, unloadTiltUpTrigCk, unloadTiltDownTrigCk, unloadTiltLeftTrigCk,
                unloadTiltRightTrigCk,unloadTouchStartedTrigCk, unloadTouchEndedTrigCk,
            };

            specialActVM = new SpecialActEditorViewModel(deviceNum, specialAction);
            macroActVM = new MacroViewModel();
            launchProgVM = new LaunchProgramViewModel();
            loadProfileVM = new LoadProfileViewModel(profileList);
            pressKeyVM = new PressKeyViewModel();
            disconnectBtVM = new SpecialActionViewModel(5);
            checkBatteryVM = new CheckBatteryViewModel();
            multiActButtonVM = new MultiActButtonViewModel();
            saSteeringWheelVM = new SpecialActionViewModel(8);
            calibrateGyroVM = new SpecialActionViewModel(9);

            // Hide tab headers. Tab content will still be visible
            blankActTab.Visibility = Visibility.Collapsed;
            macroActTab.Visibility = Visibility.Collapsed;
            launchProgActTab.Visibility = Visibility.Collapsed;
            loadProfileTab.Visibility = Visibility.Collapsed;
            pressKetActTab.Visibility = Visibility.Collapsed;
            disconnectBTTab.Visibility = Visibility.Collapsed;
            checkBatteryTab.Visibility = Visibility.Collapsed;
            multiActTab.Visibility = Visibility.Collapsed;
            sixaxisWheelCalibrateTab.Visibility = Visibility.Collapsed;
            gyroCalibrateTab.Visibility = Visibility.Collapsed;

            if (specialAction != null)
            {
                LoadAction(specialAction);
            }

            actionTypeTabControl.DataContext = specialActVM;
            actionTypeCombo.DataContext = specialActVM;
            actionNameTxt.DataContext = specialActVM;
            triggersListView.DataContext = specialActVM;

            macroActTab.DataContext = macroActVM;
            launchProgActTab.DataContext = launchProgVM;
            loadProfileTab.DataContext = loadProfileVM;
            pressKetActTab.DataContext = pressKeyVM;
            disconnectBTTab.DataContext = disconnectBtVM;
            checkBatteryTab.DataContext = checkBatteryVM;
            multiActTab.DataContext = multiActButtonVM;
            sixaxisWheelCalibrateTab.DataContext = saSteeringWheelVM;
            gyroCalibrateTab.DataContext = calibrateGyroVM;

            SetupLateEvents();
        }

        private void SetupLateEvents()
        {
            actionTypeCombo.SelectionChanged += ActionTypeCombo_SelectionChanged;
        }

        private void UnregisterDataContext()
        {
            actionTypeTabControl.DataContext = null;
            actionTypeCombo.DataContext = null;
            actionNameTxt.DataContext = null;
            triggersListView.DataContext = null;

            macroActTab.DataContext = null;
            launchProgActTab.DataContext = null;
            loadProfileTab.DataContext = null;
            pressKetActTab.DataContext = null;
            disconnectBTTab.DataContext = null;
            checkBatteryTab.DataContext = null;
            multiActTab.DataContext = null;
            sixaxisWheelCalibrateTab.DataContext = null;
            gyroCalibrateTab.DataContext = null;
        }

        private void LoadAction(DS4Windows.SpecialAction specialAction)
        {
            specialActVM.LoadAction(specialAction);
            string[] tempTriggers = specialActVM.ControlTriggerList.ToArray();
            foreach (string control in tempTriggers)
            {
                bool found = false;
                foreach (CheckBox box in triggerBoxes)
                {
                    if (box.Tag.ToString() == control)
                    {
                        box.IsChecked = true;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    specialActVM.ControlTriggerList.Remove(control);
                }
            }

            tempTriggers = specialActVM.ControlUnloadTriggerList.ToArray();
            foreach (string control in tempTriggers)
            {
                bool found = false;
                foreach (CheckBox box in unloadTriggerBoxes)
                {
                    if (box.Tag.ToString() == control)
                    {
                        box.IsChecked = true;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    specialActVM.ControlUnloadTriggerList.Remove(control);
                }
            }

            switch (specialAction.typeID)
            {
                case DS4Windows.SpecialAction.ActionTypeId.Macro:
                    macroActVM.LoadAction(specialAction);
                    break;
                case DS4Windows.SpecialAction.ActionTypeId.Program:
                    launchProgVM.LoadAction(specialAction);
                    break;
                case DS4Windows.SpecialAction.ActionTypeId.Profile:
                    loadProfileVM.LoadAction(specialAction);
                    break;
                case DS4Windows.SpecialAction.ActionTypeId.Key:
                    pressKeyVM.LoadAction(specialAction);
                    break;
                case DS4Windows.SpecialAction.ActionTypeId.DisconnectBT:
                    disconnectBtVM.LoadAction(specialAction);
                    break;
                case DS4Windows.SpecialAction.ActionTypeId.BatteryCheck:
                    checkBatteryVM.LoadAction(specialAction);
                    break;
                case DS4Windows.SpecialAction.ActionTypeId.MultiAction:
                    multiActButtonVM.LoadAction(specialAction);
                    break;
                case DS4Windows.SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate:
                    saSteeringWheelVM.LoadAction(specialAction);
                    break;
                case DS4Windows.SpecialAction.ActionTypeId.GyroCalibrate:
                    calibrateGyroVM.LoadAction(specialAction);
                    break;
            }
        }

        private void ActionTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (specialActVM.ActionTypeIndex <= 0)
            {
                saveBtn.IsEnabled = false;
            }
            else
            {
                saveBtn.IsEnabled = true;
            }

            triggersListView.Visibility = Visibility.Visible;
            unloadTriggersListView.Visibility = Visibility.Collapsed;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            UnregisterDataContext();

            Cancel?.Invoke(this, EventArgs.Empty);
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            DS4Windows.SpecialAction.ActionTypeId typeId = specialActVM.TypeAssoc[specialActVM.ActionTypeIndex];
            DS4Windows.SpecialAction tempAct = new DS4Windows.SpecialAction("null", "null", "null", "null");
            bool valid = specialActVM.IsValid(tempAct);
            if (valid)
            {
                specialActVM.SetAction(tempAct);
                // Duplicate-name validation is handled by the ViewModel (normalized
                // comparison). No additional re-check here to avoid duplication
                // and potential race conditions.
                valid = CheckActionValid(tempAct, typeId);
            }
            else if (specialActVM.ExistingName)
            {
                // If the ViewModel already detected an existing name error,
                // show the message near the Save button and bail out *before*
                // we detach data contexts. This keeps the editor state intact
                // so the user can correct the name. Use a lightweight custom
                // window so we can position it precisely.
                try
                {
                    // Use the custom-positioned window but styled to look like
                    // a native MessageBox so we can control exact placement.
                    ShowMessageWindowAt(Properties.Resources.ActionExists, saveBtn);
                }
                catch
                {
                    // Fallback to MessageBox if positioning fails for any reason
                    MessageBox.Show(Properties.Resources.ActionExists);
                }
                return;
            }

            UnregisterDataContext();
            if (valid)
            {
                bool editMode = specialActVM.EditMode;
                if (editMode && specialActVM.SavedAction.name != specialActVM.ActionName)
                {
                    DS4Windows.Global.RemoveAction(specialActVM.SavedAction.name);
                    editMode = false;
                }

                switch (typeId)
                {
                    case DS4Windows.SpecialAction.ActionTypeId.Macro:
                        macroActVM.SaveAction(tempAct, editMode);
                        break;
                    case DS4Windows.SpecialAction.ActionTypeId.Program:
                        launchProgVM.SaveAction(tempAct, editMode);
                        break;
                    case DS4Windows.SpecialAction.ActionTypeId.Profile:
                        loadProfileVM.SaveAction(tempAct, editMode);
                        break;
                    case DS4Windows.SpecialAction.ActionTypeId.Key:
                        pressKeyVM.SaveAction(tempAct, editMode);
                        break;
                    case DS4Windows.SpecialAction.ActionTypeId.DisconnectBT:
                        disconnectBtVM.SaveAction(tempAct, editMode);
                        break;
                    case DS4Windows.SpecialAction.ActionTypeId.BatteryCheck:
                        checkBatteryVM.SaveAction(tempAct, editMode);
                        break;
                    case DS4Windows.SpecialAction.ActionTypeId.MultiAction:
                        multiActButtonVM.SaveAction(tempAct, editMode);
                        break;
                    case DS4Windows.SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate:
                        saSteeringWheelVM.SaveAction(tempAct, editMode);
                        break;
                    case DS4Windows.SpecialAction.ActionTypeId.GyroCalibrate:
                        calibrateGyroVM.SaveAction(tempAct, editMode);
                        break;
                }

                Saved?.Invoke(this, tempAct.name);
            }
        }

        private bool CheckActionValid(DS4Windows.SpecialAction action,
            DS4Windows.SpecialAction.ActionTypeId typeId)
        {
            bool valid = false;
            switch (typeId)
            {
                case DS4Windows.SpecialAction.ActionTypeId.Macro:
                    valid = macroActVM.IsValid(action);
                    break;
                case DS4Windows.SpecialAction.ActionTypeId.Program:
                    valid = launchProgVM.IsValid(action);
                    break;
                case DS4Windows.SpecialAction.ActionTypeId.Profile:
                    valid = loadProfileVM.IsValid(action);
                    break;
                case DS4Windows.SpecialAction.ActionTypeId.Key:
                    valid = pressKeyVM.IsValid(action);
                    break;
                case DS4Windows.SpecialAction.ActionTypeId.DisconnectBT:
                    valid = disconnectBtVM.IsValid(action);
                    break;
                case DS4Windows.SpecialAction.ActionTypeId.BatteryCheck:
                    valid = checkBatteryVM.IsValid(action);
                    break;
                case DS4Windows.SpecialAction.ActionTypeId.MultiAction:
                    valid = multiActButtonVM.IsValid(action);
                    break;
                case DS4Windows.SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate:
                    valid = saSteeringWheelVM.IsValid(action);
                    break;
                case DS4Windows.SpecialAction.ActionTypeId.GyroCalibrate:
                    valid = calibrateGyroVM.IsValid(action);
                    break;
            }

            return valid;
        }

        private void ControlTriggerCheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox check = sender as CheckBox;
            string name = check.Tag.ToString();
            if (check.IsChecked == true)
            {
                specialActVM.ControlTriggerList.Add(name);
            }
            else
            {
                specialActVM.ControlTriggerList.Remove(name);
            }
        }

        private void ControlUnloadTriggerCheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox check = sender as CheckBox;
            string name = check.Tag.ToString();
            if (check.IsChecked == true)
            {
                specialActVM.ControlUnloadTriggerList.Add(name);
            }
            else
            {
                specialActVM.ControlUnloadTriggerList.Remove(name);
            }
        }

        private void RecordMacroBtn_Click(object sender, RoutedEventArgs e)
        {
            DS4Windows.DS4ControlSettings settings = macroActVM.PrepareSettings();
            RecordBoxWindow recordWin = new RecordBoxWindow(specialActVM.DeviceNum, settings);
            recordWin.Saved += (sender2, args) =>
            {
                macroActVM.Macro.Clear();
                macroActVM.Macro.AddRange((int[])settings.action.actionMacro);
                macroActVM.UpdateMacroString();
            };

            recordWin.ShowDialog();
        }

        private void PressKeyToggleTriggerBtn_Click(object sender, RoutedEventArgs e)
        {
            bool normalTrigger = pressKeyVM.NormalTrigger = !pressKeyVM.NormalTrigger;
            if (normalTrigger)
            {
                pressKeyToggleTriggerBtn.Content = "Set Unload Trigger";
                triggersListView.Visibility = Visibility.Visible;
                unloadTriggersListView.Visibility = Visibility.Collapsed;
            }
            else
            {
                pressKeyToggleTriggerBtn.Content = "Set Regular Trigger";
                triggersListView.Visibility = Visibility.Collapsed;
                unloadTriggersListView.Visibility = Visibility.Visible;
            }
        }

        private void LoadProfUnloadBtn_Click(object sender, RoutedEventArgs e)
        {
            bool normalTrigger = loadProfileVM.NormalTrigger = !loadProfileVM.NormalTrigger;
            if (normalTrigger)
            {
                loadProfUnloadBtn.Content = "Set Unload Trigger";
                triggersListView.Visibility = Visibility.Visible;
                unloadTriggersListView.Visibility = Visibility.Collapsed;
            }
            else
            {
                loadProfUnloadBtn.Content = "Set Regular Trigger";
                triggersListView.Visibility = Visibility.Collapsed;
                unloadTriggersListView.Visibility = Visibility.Visible;
            }
        }

        private void BatteryEmptyColorBtn_Click(object sender, RoutedEventArgs e)
        {
            ColorPickerWindow dialog = new ColorPickerWindow();
            dialog.Owner = Application.Current.MainWindow;
            Color tempcolor = checkBatteryVM.EmptyColor;
            dialog.colorPicker.SelectedColor = tempcolor;
            checkBatteryVM.StartForcedColor(tempcolor, specialActVM.DeviceNum);
            dialog.ColorChanged += (sender2, color) =>
            {
                checkBatteryVM.UpdateForcedColor(color, specialActVM.DeviceNum);
            };
            dialog.ShowDialog();
            checkBatteryVM.EndForcedColor(specialActVM.DeviceNum);
            checkBatteryVM.EmptyColor = dialog.colorPicker.SelectedColor.GetValueOrDefault();
        }

        private void BatteryFullColorBtn_Click(object sender, RoutedEventArgs e)
        {
            ColorPickerWindow dialog = new ColorPickerWindow();
            dialog.Owner = Application.Current.MainWindow;
            Color tempcolor = checkBatteryVM.FullColor;
            dialog.colorPicker.SelectedColor = tempcolor;
            checkBatteryVM.StartForcedColor(tempcolor, specialActVM.DeviceNum);
            dialog.ColorChanged += (sender2, color) =>
            {
                checkBatteryVM.UpdateForcedColor(color, specialActVM.DeviceNum);
            };
            dialog.ShowDialog();
            checkBatteryVM.EndForcedColor(specialActVM.DeviceNum);
            checkBatteryVM.FullColor = dialog.colorPicker.SelectedColor.GetValueOrDefault();
        }

        private void MultiTapTrigBtn_Click(object sender, RoutedEventArgs e)
        {
            DS4Windows.DS4ControlSettings settings = multiActButtonVM.PrepareTapSettings();
            RecordBoxWindow recordWin = new RecordBoxWindow(specialActVM.DeviceNum, settings, false);
            recordWin.Saved += (sender2, args) =>
            {
                multiActButtonVM.TapMacro.Clear();
                multiActButtonVM.TapMacro.AddRange((int[])settings.action.actionMacro);
                multiActButtonVM.UpdateTapDisplayText();
            };

            recordWin.ShowDialog();
        }

        private void MultiHoldTapTrigBtn_Click(object sender, RoutedEventArgs e)
        {
            DS4Windows.DS4ControlSettings settings = multiActButtonVM.PrepareHoldSettings();
            RecordBoxWindow recordWin = new RecordBoxWindow(specialActVM.DeviceNum, settings, false);
            recordWin.Saved += (sender2, args) =>
            {
                multiActButtonVM.HoldMacro.Clear();
                multiActButtonVM.HoldMacro.AddRange((int[])settings.action.actionMacro);
                multiActButtonVM.UpdateHoldDisplayText();
            };

            recordWin.ShowDialog();
        }

        private void MultiDoubleTapTrigBtn_Click(object sender, RoutedEventArgs e)
        {
            DS4Windows.DS4ControlSettings settings = multiActButtonVM.PrepareDoubleTapSettings();
            RecordBoxWindow recordWin = new RecordBoxWindow(specialActVM.DeviceNum, settings, false);
            recordWin.Saved += (sender2, args) =>
            {
                multiActButtonVM.DoubleTapMacro.Clear();
                multiActButtonVM.DoubleTapMacro.AddRange((int[])settings.action.actionMacro);
                multiActButtonVM.UpdateDoubleTapDisplayText();
            };

            recordWin.ShowDialog();
        }

        private void LaunchProgBrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            dialog.AddExtension = true;
            dialog.DefaultExt = ".exe";
            dialog.Filter = "Exe (*.exe)|*.exe|Batch (*.bat,*.cmd)|*.bat;*.cmd|All Files (*.*)|*.*";
            dialog.Title = "Select Program";

            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (dialog.ShowDialog() == true)
            {
                launchProgVM.Filepath = dialog.FileName;
            }
        }

        private void PressKeySelectBtn_Click(object sender, RoutedEventArgs e)
        {
            DS4Windows.DS4ControlSettings settings = pressKeyVM.PrepareSettings();
            BindingWindow window = new BindingWindow(specialActVM.DeviceNum, settings,
                BindingWindow.ExposeMode.Keyboard);
            window.Owner = App.Current.MainWindow;
            window.ShowDialog();
            pressKeyVM.ReadSettings(settings);
            pressKeyVM.UpdateDescribeText();
            pressKeyVM.UpdateToggleControls();
        }

        // Show a transient message window positioned near the given anchor element.
        // This creates a small modal window with an OK button and positions it
        // slightly below the anchor (e.g., the Save button).
        private void ShowMessageWindowAt(string message, FrameworkElement anchor)
        {
            // Keep the existing custom window implementation as a fallback.
            Window owner = Application.Current?.MainWindow;

            Window msgWin = new Window()
            {
                Title = string.Empty,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStyle = WindowStyle.SingleBorderWindow,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Topmost = true,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.Manual,
                FontFamily = SystemFonts.MessageFontFamily,
                FontSize = SystemFonts.MessageFontSize
            };

            var panel = new StackPanel() { Margin = new Thickness(16) };
            var tb = new TextBlock()
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12),
                MaxWidth = 420
            };
            var okBtn = new Button() { Content = "OK", IsDefault = true, HorizontalAlignment = HorizontalAlignment.Center, MinWidth = 80 };
            okBtn.Click += (s, e) => msgWin.Close();
            panel.Children.Add(tb);
            panel.Children.Add(okBtn);
            msgWin.Content = panel;

            // Compute screen position for anchor and horizontally center the dialog
            double centerX = 0, top = 0;
            try
            {
                if (anchor != null)
                {
                    var anchorPt = anchor.PointToScreen(new Point(0, 0));
                    var src = PresentationSource.FromVisual(this);
                    if (src != null && src.CompositionTarget != null)
                    {
                        var transform = src.CompositionTarget.TransformFromDevice;
                        var dpiPt = transform.Transform(anchorPt);
                        AppLogger.LogDebug($"SpecialActionEditor: anchorPt(physical)={anchorPt} dpiPt(logical)={dpiPt} anchorSize=({anchor.ActualWidth}x{anchor.ActualHeight})");
                        centerX = dpiPt.X + anchor.ActualWidth / 2.0;
                        top = dpiPt.Y + anchor.ActualHeight + 8; // slightly below anchor
                    }
                    else
                    {
                        centerX = anchorPt.X + anchor.ActualWidth / 2.0;
                        top = anchorPt.Y + anchor.ActualHeight + 8;
                    }
                }
                else if (owner != null)
                {
                    centerX = owner.Left + 200;
                    top = owner.Top + 100;
                }
            }
            catch { }

            // Measure content to determine width, then position so dialog is centered
            panel.Measure(new Size(600, 400));
            double contentW = panel.DesiredSize.Width;
            double windowW = contentW + 32; // include margins/chrome approximation
            msgWin.Left = centerX - windowW / 2.0;
            msgWin.Top = top;

            msgWin.ShowDialog();
        }

        // Show a native MessageBox positioned by creating an invisible tiny owner
        // window located at the desired anchor point. MessageBox centers itself
        // over its owner, so placing the owner at the anchor makes the MessageBox
        // appear at that spot while preserving native look-and-feel.
        private void ShowPositionedMessageBox(string message, FrameworkElement anchor)
        {
            // Compute target point (screen coordinates in WPF units)
            double left = 0, top = 0;
            try
            {
                if (anchor != null)
                {
                    var anchorPt = anchor.PointToScreen(new Point(0, 0));
                    var src = PresentationSource.FromVisual(this);
                    if (src != null && src.CompositionTarget != null)
                    {
                            var transform = src.CompositionTarget.TransformFromDevice;
                            var dpiPt = transform.Transform(anchorPt);
                            AppLogger.LogDebug($"SpecialActionEditor.ShowPositionedMessageBox: anchorPt(physical)={anchorPt} dpiPt(logical)={dpiPt} anchorSize=({anchor.ActualWidth}x{anchor.ActualHeight})");
                            left = dpiPt.X + anchor.ActualWidth / 2.0; // center anchor horizontally
                            top = dpiPt.Y + anchor.ActualHeight + 8; // slightly below anchor
                    }
                    else
                    {
                        left = anchorPt.X + anchor.ActualWidth / 2.0;
                        top = anchorPt.Y + anchor.ActualHeight + 8;
                    }
                }
                else
                {
                    var owner = Application.Current?.MainWindow;
                    if (owner != null)
                    {
                        left = owner.Left + 100;
                        top = owner.Top + 100;
                    }
                }
            }
            catch { }

            // Create tiny transparent owner window at the computed point
            var ownerWindow = new Window()
            {
                Width = 1,
                Height = 1,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                ShowInTaskbar = false,
                ResizeMode = ResizeMode.NoResize,
                Topmost = true,
                WindowStartupLocation = WindowStartupLocation.Manual
            };

            // Place owner so its center is at (left, top). Adjust to account for 1x1 size.
            ownerWindow.Left = left - 0.5;
            ownerWindow.Top = top - 0.5;

            try
            {
                ownerWindow.Show();
                // Use ownerWindow as owner for native MessageBox
                MessageBox.Show(ownerWindow, message);
            }
            finally
            {
                try { ownerWindow.Close(); } catch { }
            }
        }
    }
}
