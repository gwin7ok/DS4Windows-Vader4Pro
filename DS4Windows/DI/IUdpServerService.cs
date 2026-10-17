using System;
using DS4Windows;

namespace DS4Windows.DI
{
    /// <summary>
    /// Cemuhook モーションデータ UDP サーバーのライフサイクル管理を提供するサービス。
    /// 900行超の UdpServer.cs を解体せず境界化し、安全な起動・停止と状態確認を提供します。
    /// </summary>
    // TODO(Phase7-Step2 W7 の調査で「接続」と判断。契約全体を残す): Phase5-Step10（2026-09-04）で、UDP サーバーの安全な起動・停止の窓口として
    // 作った契約。DI に登録されているが、使う側が無い（本体の呼び出しが 0 件）。現状の差: ControlService が UdpServer を直接 new して
    // 起動・停止する（ControlService.cs:949 付近。GetPadDetailForIdx の受け渡しと、スムージング設定の反映も含む）。
    // 接続予定: Phase7-Step5 以降（ControlService の UDP の起動・停止を本サービス経由にする。UdpServer のコンストラクター引数の受け渡しを
    // 契約へ足す必要があるため、別 Step の計画書で扱う）。
    public interface IUdpServerService
    {
        bool IsRunning { get; }
        int Port { get; }

        bool Start(ControlService control, int port = 26760, string listenAddress = "127.0.0.1");
        void Stop();
    }
}
