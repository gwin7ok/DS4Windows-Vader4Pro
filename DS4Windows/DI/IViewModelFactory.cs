using System;
using DS4Windows;
using DS4WinWPF;
using DS4WinWPF.DS4Forms;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4Windows.DI
{
    public interface IViewModelFactory
    {
        ProfileSettingsViewModel CreateProfileSettingsViewModel(int device);
        RecordBoxViewModel CreateRecordBoxViewModel(int device, DS4ControlSettings controlSettings, bool recordMacro = true, bool extraHold = false);
        SpecialActEditorViewModel CreateSpecialActEditorViewModel(int device, SpecialAction action = null);
        AutoProfilesViewModel CreateAutoProfilesViewModel(AutoProfileHolder autoProfileHolder, ProfileList profileList);
    }
}
