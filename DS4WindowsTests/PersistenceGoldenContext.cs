using System;
using System.IO;
using System.Text.RegularExpressions;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.DS4Control;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10c W1: ゴールデンテストの実行環境。設定の保存場所（<c>Global.appdatapath</c>）と実行ファイルの場所
    /// （<c>Global.exedirpath</c>）を一時フォルダーへ向け、DI のホストと <see cref="ControlService"/> を用意する。破棄時に元へ戻す。
    /// 並列実行は xunit.runner.json で無効のため、静的状態の書き換えは他のテストと競合しない。
    /// </summary>
    internal sealed class PersistenceGoldenContext : IDisposable
    {
        private readonly string _originalAppData;
        private readonly string _originalExeDir;
        private readonly IProfileSettingsService _settings;
        private readonly VirtualKBMMapping _originalMapping;

        public string Root { get; }
        public string AppData { get; }
        public string ExeDir { get; }
        public ControlService Control { get; }

        public PersistenceGoldenContext()
        {
            Root = Path.Combine(Path.GetTempPath(), "ds4w_p7s10c_" + Guid.NewGuid().ToString("N"));
            AppData = Path.Combine(Root, "AppData");
            ExeDir = Path.Combine(Root, "Exe");
            Directory.CreateDirectory(Path.Combine(AppData, "Profiles"));
            Directory.CreateDirectory(ExeDir);

            _originalAppData = Global.appdatapath;
            _originalExeDir = Global.exedirpath;
            Global.appdatapath = AppData;
            Global.exedirpath = ExeDir;

            DS4WinWPF.AppHost.CreateHost();
            Control = DS4WinWPF.AppHost.GetService<ControlService>();

            // プロファイルの読込（LoadProfile → UpdateDS4CSetting → Global.RefreshActionAlias）は、出力 KBM のマッピングが必要。
            // 本体では ControlService の起動時に設定される。RefreshActionAlias は Global.ProfileSettingsServiceInstance（最初に解決した
            // コンテナの instance を静的に保持する）を読むため、テストの実行順によらず、その instance に設定する。
            // 既定の方式（SendInput）で設定し、破棄時に元へ戻す。
            _settings = Global.ProfileSettingsServiceInstance;
            _originalMapping = _settings.OutputKBMMapping;
            _settings.OutputKBMMapping = VirtualKBMFactory.GetMappingInstance(SendInputHandler.IDENTIFIER);
        }

        /// <summary>揺れる値（一時フォルダーのパス、バージョン、日時のコメント）を固定の文字列にする。</summary>
        public string Normalize(string text)
        {
            if (text == null) return string.Empty;
            text = text.Replace(Root, "<TMP>").Replace(Root.Replace('\\', '/'), "<TMP>");
            text = Regex.Replace(text, "<!--.*?-->", "<!--C-->", RegexOptions.Singleline);
            // 読込の入力にもゴールデンを使うため、置換後も妥当な XML でなければならない（属性値に < を含めない）
            text = Regex.Replace(text, "app_version=\"[^\"]*\"", "app_version=\"0.0.0.0\"");
            return text;
        }

        public void Dispose()
        {
            _settings.OutputKBMMapping = _originalMapping;
            Global.appdatapath = _originalAppData;
            Global.exedirpath = _originalExeDir;
            try { Directory.Delete(Root, true); } catch { }
        }
    }
}
