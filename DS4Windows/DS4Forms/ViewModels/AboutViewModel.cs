using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    public class AboutViewModel : INotifyPropertyChanged
    {
        private readonly IPathService _pathService;

        public event PropertyChangedEventHandler PropertyChanged;

        public string VersionText => $"v{Global.exeversion}";
        public string AppTitle => "DS4Windows - Vader 4 Pro Edition";
        public string GithubUrl => "https://github.com/gwin7ok/DS4Windows-Vader4Pro";
        public string ChangelogUrl => "https://github.com/gwin7ok/DS4Windows-Vader4Pro/releases";

        public AboutViewModel(IPathService pathService = null)
        {
            _pathService = pathService ?? AppHost.GetService<IPathService>();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
