using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    public class ControllersViewModel : INotifyPropertyChanged
    {
        private readonly IDeviceStateService _deviceStateService;
        private readonly IProfileSettingsService _profileSettingsService;
        private readonly IProfileRepository _profileRepository;

        public event PropertyChangedEventHandler PropertyChanged;

        public ControllersViewModel(
            IDeviceStateService deviceStateService = null,
            IProfileSettingsService profileSettingsService = null,
            IProfileRepository profileRepository = null)
        {
            _deviceStateService = deviceStateService ?? DS4WinWPF.AppHost.GetService<IDeviceStateService>();
            _profileSettingsService = profileSettingsService ?? DS4WinWPF.AppHost.GetService<IProfileSettingsService>();
            _profileRepository = profileRepository ?? DS4WinWPF.AppHost.GetService<IProfileRepository>();
        }

        public bool HasControllers => _deviceStateService != null && _deviceStateService.ConnectedControllersCount > 0;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
