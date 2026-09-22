namespace DS4Windows.DI
{
    /// <summary>
    /// 絶対マウス出力で使用する画面座標系（使用モニター境界・デスクトップ全体範囲）の
    /// 保持と、入力座標から画面座標への変換を提供するサービス。
    ///
    /// 【Phase6-Step3-1】`Mapping.cs` が `using static DS4Windows.Global;` 経由で直接参照していた
    /// `absUseAllMonitors` / `TranslateCoorToAbsDisplay` の移行先として新設（論点3、
    /// `Phase6-Step3-Plan.md` 参照）。`PrepareAbsMonitorBounds` は従来 <see cref="IEnvironmentService"/>
    /// にあったが、同じ画面座標状態（absDisplayBounds/fullDesktopBounds/absUseAllMonitors）を
    /// 扱う処理のため本サービスへ集約した（決定D1）。唯一の呼び出し元だった
    /// <c>ControlService</c> 側も本サービス経由に更新済み。
    /// </summary>
    public interface IDisplayCoordinateService
    {
        /// <summary>すべての接続モニターを1つの仮想デスクトップとして扱うか（プロファイル設定に連動）。</summary>
        bool UseAllMonitors { get; }

        /// <summary>接続モニター構成の変化を受けて画面座標系（境界情報）を再取得する。</summary>
        void PrepareAbsMonitorBounds(string edid);

        /// <summary>DS4パッドの相対入力座標を、現在の画面座標系上の絶対座標に変換する。</summary>
        void TranslateCoorToAbsDisplay(double inX, double inY, out double outX, out double outY);
    }
}