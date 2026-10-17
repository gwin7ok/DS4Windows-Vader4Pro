using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    public class AboutViewModel : INotifyPropertyChanged
    {
        // TODO(技術的負債・Phase6-Step10-1-3 の判断［§2.4］): 本クラスは Phase4-Step7 で「全画面の MVVM 統一」のために新設されたが、
        // About 画面（About.xaml／About.xaml.cs）は本クラスへ切り替えられておらず、呼出元は DI 登録（ServiceRegistration）と
        // PatternAViewModelTests のみ（Phase4-Step7-Completion-Report.md）。本来使うべきものと判断して削除しない。
        // About 画面の DataContext への接続は、Phase6-Step12 で判断する（UI の変更を伴うため、本 Step の範囲に混ぜない）。
        // 本 Step では、Global.exeversion の直接参照と AppHost のフォールバック（未使用の IPathService）のみ是正した。
        private readonly IEnvironmentService _environmentService;

        public event PropertyChangedEventHandler PropertyChanged;

        public string VersionText => $"v{_environmentService.ApplicationVersion}";
        public string AppTitle => "DS4Windows - Vader 4 Pro Edition";
        public string GithubUrl => "https://github.com/gwin7ok/DS4Windows-Vader4Pro";
        public string ChangelogUrl => "https://github.com/gwin7ok/DS4Windows-Vader4Pro/releases";

        public AboutViewModel(IEnvironmentService environmentService)
        {
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
