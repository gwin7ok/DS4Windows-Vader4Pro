using System;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF;
using DS4WinWPF.DS4Forms;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4Windows
{
    public class ViewModelFactory : IViewModelFactory
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly IProfileRepository _profileRepo;
        private readonly ISpecialActionRepository _actionRepo;

        public ViewModelFactory(
            IProfileSettingsService profileSettings = null,
            IProfileRepository profileRepo = null,
            ISpecialActionRepository actionRepo = null)
        {
            _profileSettings = profileSettings ?? DS4WinWPF.AppHost.GetService<IProfileSettingsService>();
            _profileRepo = profileRepo ?? DS4WinWPF.AppHost.GetService<IProfileRepository>();
            _actionRepo = actionRepo ?? DS4WinWPF.AppHost.GetService<ISpecialActionRepository>();
        }

        public ProfileSettingsViewModel CreateProfileSettingsViewModel(int device)
        {
            AppLogger.LogToGui($"[DI] ViewModelFactory: Created ProfileSettingsViewModel for Device {device}", false, true);
            return new ProfileSettingsViewModel(device);
        }

        public RecordBoxViewModel CreateRecordBoxViewModel(int device, DS4ControlSettings controlSettings, bool recordMacro = true, bool extraHold = false)
        {
            AppLogger.LogToGui($"[DI] ViewModelFactory: Created RecordBoxViewModel for Device {device}", false, true);
            return new RecordBoxViewModel(device, controlSettings, recordMacro, extraHold);
        }

        public SpecialActEditorViewModel CreateSpecialActEditorViewModel(int device, SpecialAction action = null)
        {
            AppLogger.LogToGui($"[DI] ViewModelFactory: Created SpecialActEditorViewModel for Device {device}", false, true);
            return new SpecialActEditorViewModel(device, action);
        }

        public AutoProfilesViewModel CreateAutoProfilesViewModel(AutoProfileHolder autoProfileHolder, ProfileList profileList)
        {
            AppLogger.LogToGui("[DI] ViewModelFactory: Created AutoProfilesViewModel", false, true);
            return new AutoProfilesViewModel(autoProfileHolder, profileList);
        }
    }
}
