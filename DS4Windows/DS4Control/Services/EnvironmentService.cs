using System;
using DS4Windows.DI;

namespace DS4Windows
{
    public class EnvironmentService : IEnvironmentService
    {
        private readonly object _syncLock = new object();

        private bool _runAtStartup = false;
        private bool _startMinimized = false;
        private bool _closeMinimizes = false;
        private string _useLang = string.Empty;

        private int _formWidth = 782;
        private int _formHeight = 550;
        private int _formLocationX = 0;
        private int _formLocationY = 0;

        public event EventHandler EnvironmentSettingChanged;

        public bool RunAtStartup
        {
            get => _runAtStartup;
            set { lock (_syncLock) { _runAtStartup = value; OnSettingChanged(); } }
        }

        public bool StartMinimized
        {
            get => _startMinimized;
            set { lock (_syncLock) { _startMinimized = value; OnSettingChanged(); } }
        }

        public bool CloseMinimizes
        {
            get => _closeMinimizes;
            set { lock (_syncLock) { _closeMinimizes = value; OnSettingChanged(); } }
        }

        public string UseLang
        {
            get => _useLang;
            set { lock (_syncLock) { _useLang = value ?? string.Empty; OnSettingChanged(); } }
        }

        public int FormWidth
        {
            get => _formWidth;
            set { lock (_syncLock) { _formWidth = value; OnSettingChanged(); } }
        }

        public int FormHeight
        {
            get => _formHeight;
            set { lock (_syncLock) { _formHeight = value; OnSettingChanged(); } }
        }

        public int FormLocationX
        {
            get => _formLocationX;
            set { lock (_syncLock) { _formLocationX = value; OnSettingChanged(); } }
        }

        public int FormLocationY
        {
            get => _formLocationY;
            set { lock (_syncLock) { _formLocationY = value; OnSettingChanged(); } }
        }

        protected virtual void OnSettingChanged()
        {
            EnvironmentSettingChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
