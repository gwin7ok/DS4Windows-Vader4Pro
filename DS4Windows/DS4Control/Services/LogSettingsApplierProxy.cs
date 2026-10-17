using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// <see cref="ILogSettingsApplier"/> の DI 登録用の窓口（遅延接続）。
    /// LoggerHolder は、設定（Profiles.xml）の読み込みと NLog の構成の読み込みが終わった後でなければ生成できず
    /// （コンストラクタが <c>InvalidOperationException</c> を投げる）、DI コンテナから生成させられない。
    /// そのため App が LoggerHolder を生成した直後に <see cref="Attach"/> で接続する。
    /// 接続前の呼び出しは何もしない（置換前の <c>App.logHolder?.…</c> と同じ動作）。
    /// </summary>
    public class LogSettingsApplierProxy : ILogSettingsApplier
    {
        private volatile ILogSettingsApplier target;

        /// <summary>実際に反映を行う実装（LoggerHolder）を接続する。</summary>
        public void Attach(ILogSettingsApplier applier)
        {
            target = applier;
        }

        public void UpdateLogSettings() => target?.UpdateLogSettings();

        public void UpdateNLogConfig(int maxArchiveFiles) => target?.UpdateNLogConfig(maxArchiveFiles);

        public void RestoreArchiveSetting() => target?.RestoreArchiveSetting();
    }
}
