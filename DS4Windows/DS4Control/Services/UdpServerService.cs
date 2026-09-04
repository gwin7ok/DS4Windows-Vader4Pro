using System;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    public class UdpServerService : IUdpServerService
    {
        private readonly object _lock = new object();
        private UdpServer _server;
        private bool _isRunning;
        private int _port = 26760;
        private string _listenAddress = "127.0.0.1";

        public bool IsRunning
        {
            get
            {
                lock (_lock)
                {
                    return _isRunning;
                }
            }
        }

        public int Port => _port;
        public string ListenAddress => _listenAddress;

        public bool Start(ControlService control, int port = 26760, string listenAddress = "127.0.0.1")
        {
            lock (_lock)
            {
                if (_isRunning)
                {
                    Stop();
                }

                if (control == null)
                    return false;

                try
                {
                    _port = port;
                    _listenAddress = string.IsNullOrWhiteSpace(listenAddress) ? "127.0.0.1" : listenAddress;
                    _server = new UdpServer(control, _port, _listenAddress);
                    _server.Start();
                    _isRunning = true;

                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace($"[DI] UdpServerService started on {_listenAddress}:{_port}");

                    return true;
                }
                catch (Exception ex)
                {
                    AppLogger.LogToGui($"Failed to start UDP Server on {listenAddress}:{port}: {ex.Message}", true);
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace($"[DI] UdpServerService start failed: {ex}");
                    _isRunning = false;
                    _server = null;
                    return false;
                }
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (!_isRunning && _server == null)
                    return;

                try
                {
                    _server?.Stop();
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace("[DI] UdpServerService stopped");
                }
                catch (Exception ex)
                {
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace($"[DI] UdpServerService stop exception: {ex}");
                }
                finally
                {
                    _server = null;
                    _isRunning = false;
                }
            }
        }
    }
}
