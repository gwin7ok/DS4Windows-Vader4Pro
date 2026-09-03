using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace DS4Windows.Services
{
    public class DefaultProcessInspector : IProcessInspector
    {
        private IntPtr _prevForegroundWnd = IntPtr.Zero;
        private uint _prevForegroundProcessId = 0;
        private string _prevForegroundProcessName = string.Empty;
        private string _prevForegroundWndTitleName = string.Empty;
        private readonly StringBuilder _textBuilder = new StringBuilder(1000);
        private readonly object _lock = new object();

        public bool IsProcessRunning(string exePath)
        {
            if (string.IsNullOrWhiteSpace(exePath))
                return false;

            Process[] localAll = Process.GetProcesses();
            bool procFound = false;
            for (int procInd = 0, procsLen = localAll.Length; !procFound && procInd < procsLen; procInd++)
            {
                try
                {
                    string temp = localAll[procInd].MainModule.FileName;
                    if (temp == exePath)
                    {
                        procFound = true;
                    }
                }
                catch { }
            }
            return procFound;
        }

        public bool GetForegroundProcessInfo(out string processPath, out string windowTitle)
        {
            lock (_lock)
            {
                IntPtr hWnd = GetForegroundWindow();
                if (hWnd == IntPtr.Zero)
                {
                    _prevForegroundWnd = IntPtr.Zero;
                    _prevForegroundProcessId = 0;
                    processPath = string.Empty;
                    windowTitle = string.Empty;
                    return false;
                }

                if (hWnd == _prevForegroundWnd)
                {
                    processPath = _prevForegroundProcessName;
                    _textBuilder.Clear();
                    GetWindowText(hWnd, _textBuilder, _textBuilder.Capacity);
                    string currentTitle = _textBuilder.ToString().ToLower();
                    if (currentTitle != _prevForegroundWndTitleName)
                    {
                        _prevForegroundWndTitleName = windowTitle = currentTitle;
                        return true;
                    }
                    windowTitle = _prevForegroundWndTitleName;
                    return false;
                }

                _prevForegroundWnd = hWnd;
                IntPtr hProcess = IntPtr.Zero;
                uint lpdwProcessId = 0;
                GetWindowThreadProcessId(hWnd, out lpdwProcessId);

                if (lpdwProcessId == _prevForegroundProcessId)
                {
                    processPath = _prevForegroundProcessName;
                }
                else
                {
                    _prevForegroundProcessId = lpdwProcessId;
                    hProcess = OpenProcess(0x0410, false, lpdwProcessId);
                    if (hProcess != IntPtr.Zero)
                    {
                        _textBuilder.Clear();
                        GetModuleFileNameEx(hProcess, IntPtr.Zero, _textBuilder, _textBuilder.Capacity);
                    }
                    else
                    {
                        _textBuilder.Clear();
                    }

                    _prevForegroundProcessName = processPath = _textBuilder.Replace('/', '\\').ToString().ToLower();
                }

                _textBuilder.Clear();
                GetWindowText(hWnd, _textBuilder, _textBuilder.Capacity);
                _prevForegroundWndTitleName = windowTitle = _textBuilder.ToString().ToLower();

                if (hProcess != IntPtr.Zero)
                {
                    CloseHandle(hProcess);
                }

                return true;
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("psapi.dll")]
        private static extern uint GetModuleFileNameEx(IntPtr hWnd, IntPtr hModule, StringBuilder lpFileName, int nSize);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nSize);
    }
}
