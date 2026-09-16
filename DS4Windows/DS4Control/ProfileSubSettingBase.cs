using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DS4Windows
{
    /// <summary>
    /// プロファイルのネストされたサブ設定クラス（スティック詳細、トリガー、ジャイロなど）向けの抽象基底クラス。
    /// INotifyPropertyChanged を実装し、子プロパティの変更を親のプロファイルコンテナへバブリング通知する
    /// OnSubPropertyChanged イベントを提供します。
    /// (Phase 5 Step 14: Profile Configuration Synchronization and Unified Persistence)
    /// </summary>
    public abstract class ProfileSubSettingBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// サブオブジェクト内のプロパティが変更された際に親へバブリングするためのイベント
        /// </summary>
        public event Action<string> OnSubPropertyChanged;

        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            OnSubPropertyChanged?.Invoke(propertyName);
        }

        /// <summary>
        /// 全プロパティの変更通知を一括発火します（Reset 処理時など）
        /// </summary>
        protected virtual void RaiseAllPropertiesChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
            OnSubPropertyChanged?.Invoke(string.Empty);
        }
    }
}