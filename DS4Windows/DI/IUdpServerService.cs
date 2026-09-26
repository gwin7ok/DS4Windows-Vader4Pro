using System;
using DS4Windows;

namespace DS4Windows.DI
{
    /// <summary>
    /// Cemuhook モーションデータ UDP サーバーのライフサイクル管理を提供するサービス。
    /// 900行超の UdpServer.cs を解体せず境界化し、安全な起動・停止と状態確認を提供します。
    /// </summary>
    public interface IUdpServerService
    {
        bool IsRunning { get; }
        int Port { get; }
        string ListenAddress { get; }

        bool Start(ControlService control, int port = 26760, string listenAddress = "127.0.0.1");
        void Stop();
    }
}
