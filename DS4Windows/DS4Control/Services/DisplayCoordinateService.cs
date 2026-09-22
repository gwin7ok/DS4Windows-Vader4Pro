using DS4Windows.DI;

namespace DS4Windows
{
    /// <summary>
    /// <see cref="IDisplayCoordinateService"/> の実装。
    ///
    /// Phase6-Step3-1 時点では、状態・ロジックの実体は引き続き <see cref="Global"/>
    /// （<c>absUseAllMonitors</c> / <c>TranslateCoorToAbsDisplay</c> / <c>PrepareAbsMonitorBounds</c>）
    /// が保持しており、本クラスはそこへの薄い委譲に留める
    /// （Strangler Fig パターン。DI-App-Wide-Migration-Plan.md §5.2 手順4 と同一方針）。
    /// </summary>
    public class DisplayCoordinateService : IDisplayCoordinateService
    {
        public bool UseAllMonitors => Global.absUseAllMonitors;

        public void PrepareAbsMonitorBounds(string edid) => Global.PrepareAbsMonitorBounds(edid);

        public void TranslateCoorToAbsDisplay(double inX, double inY, out double outX, out double outY)
            => Global.TranslateCoorToAbsDisplay(inX, inY, out outX, out outY);
    }
}