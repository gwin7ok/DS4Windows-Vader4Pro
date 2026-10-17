using System;
using DS4Windows;

namespace DS4Windows
{
    /// <summary>
    /// 6つの感度値 (LS, RS, L2, R2, SX, SZ) とパイプ区切り文字列（SensitivityString）を
    /// 双方向に自動同期するサブ設定モデル
    /// </summary>
    public class SensitivitySettings : ProfileSubSettingBase
    {
        private int[] _values = new int[6] { 0, 0, 0, 0, 0, 0 };

        public int LS
        {
            get => _values[0];
            set { if (_values[0] != value) { _values[0] = value; RaisePropertyChanged(); UpdateSensitivityString(); } }
        }

        public int RS
        {
            get => _values[1];
            set { if (_values[1] != value) { _values[1] = value; RaisePropertyChanged(); UpdateSensitivityString(); } }
        }

        public int L2
        {
            get => _values[2];
            set { if (_values[2] != value) { _values[2] = value; RaisePropertyChanged(); UpdateSensitivityString(); } }
        }

        public int R2
        {
            get => _values[3];
            set { if (_values[3] != value) { _values[3] = value; RaisePropertyChanged(); UpdateSensitivityString(); } }
        }

        public int SX
        {
            get => _values[4];
            set { if (_values[4] != value) { _values[4] = value; RaisePropertyChanged(); UpdateSensitivityString(); } }
        }

        public int SZ
        {
            get => _values[5];
            set { if (_values[5] != value) { _values[5] = value; RaisePropertyChanged(); UpdateSensitivityString(); } }
        }

        private string _sensitivityString = "0|0|0|0|0|0";
        public string SensitivityString
        {
            get => _sensitivityString;
            set
            {
                if (_sensitivityString != value)
                {
                    _sensitivityString = value ?? "0|0|0|0|0|0";
                    ParseSensitivityString(_sensitivityString);
                    RaisePropertyChanged();
                }
            }
        }

        private void UpdateSensitivityString()
        {
            string newStr = $"{LS}|{RS}|{L2}|{R2}|{SX}|{SZ}";
            if (_sensitivityString != newStr)
            {
                _sensitivityString = newStr;
                // 個別プロパティ変更時は文字列の内部更新のみ行い、
                // ここでの不要なプロパティ変更通知の連鎖（上書き）を防ぐ
            }
        }

        private void ParseSensitivityString(string str)
        {
            if (string.IsNullOrEmpty(str)) return;
            var parts = str.Split('|');
            for (int i = 0; i < Math.Min(parts.Length, _values.Length); i++)
            {
                if (int.TryParse(parts[i], out int val))
                {
                    if (_values[i] != val)
                    {
                        _values[i] = val;
                        NotifyIndividualProperty(i);
                    }
                }
            }
        }

        private void NotifyIndividualProperty(int index)
        {
            switch (index)
            {
                case 0: RaisePropertyChanged(nameof(LS)); break;
                case 1: RaisePropertyChanged(nameof(RS)); break;
                case 2: RaisePropertyChanged(nameof(L2)); break;
                case 3: RaisePropertyChanged(nameof(R2)); break;
                case 4: RaisePropertyChanged(nameof(SX)); break;
                case 5: RaisePropertyChanged(nameof(SZ)); break;
            }
        }
    }
}