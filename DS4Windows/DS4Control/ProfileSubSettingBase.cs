using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DS4Windows
{
    /// <summary>
    /// プロファイルのネストされたサブ設定クラス（スティック詳細、トリガー、ジャイロなど）向けの抽象基底クラス。
    /// INotifyPropertyChanged を実装します。
    /// (Phase 5 Step 14: Profile Configuration Synchronization and Unified Persistence)
    /// Phase7-Step10b W3: 親のプロファイルコンテナへバブリング通知する OnSubPropertyChanged イベントは、
    /// 唯一の購読の仕組み（IProfileSettingsService.WireSubSettingsEvents。Phase7-Step2 W2 で削除）がなくなり、購読者が 0 件のため削除した。
    /// </summary>
    public abstract class ProfileSubSettingBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 全プロパティの変更通知を一括発火します（Reset 処理時など）
        /// </summary>
        protected virtual void RaiseAllPropertiesChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }
}