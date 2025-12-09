# DS4Windows IPC Test Script
# PowerShellからDS4WindowsにIPCコマンドを送信してプロファイルを切り替える

param(
    [Parameter(Mandatory=$false)]
    [ValidateRange(1,4)]
    [int]$Device = 1,
    
    [Parameter(Mandatory=$false)]
    [string]$ProfileName = "",
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("loadprofile", "query", "start", "stop", "disconnect")]
    [string]$Command = "loadprofile"
)

Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.IO.MemoryMappedFiles;

public class DS4IPC {
    [DllImport("user32.dll", EntryPoint = "FindWindow", CharSet = CharSet.Auto)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct COPYDATASTRUCT {
        public IntPtr dwData;
        public int cbData;
        public IntPtr lpData;
    }

    private const uint WM_COPYDATA = 0x004A;

    private static string ReadIPCClassNameMMF() {
        try {
            using (var mmf = MemoryMappedFile.OpenExisting("DS4Windows_IPCClassName.dat")) {
                using (var accessor = mmf.CreateViewAccessor(0, 128)) {
                    byte[] buffer = new byte[128];
                    accessor.ReadArray(0, buffer, 0, buffer.Length);
                    string result = Encoding.ASCII.GetString(buffer);
                    int nullIndex = result.IndexOf('\0');
                    if (nullIndex >= 0) {
                        result = result.Substring(0, nullIndex);
                    }
                    return result;
                }
            }
        } catch (Exception ex) {
            Console.WriteLine("Error reading IPC class name: " + ex.Message);
            return null;
        }
    }

    public static bool SendCommand(string command) {
        // DS4Windowsのウィンドウクラス名を取得
        string className = ReadIPCClassNameMMF();
        if (string.IsNullOrEmpty(className)) {
            Console.WriteLine("DS4Windows is not running or IPC is not available.");
            return false;
        }

        Console.WriteLine("DS4Windows window class: " + className);

        // ウィンドウハンドルを取得
        IntPtr hWnd = FindWindow(className, "DS4Windows");
        if (hWnd == IntPtr.Zero) {
            Console.WriteLine("DS4Windows window not found.");
            return false;
        }

        Console.WriteLine("DS4Windows window handle: 0x" + hWnd.ToString("X"));

        // コマンドをバイト配列に変換
        byte[] buffer = Encoding.ASCII.GetBytes(command);
        
        // COPYDATASTRUCT を構築
        COPYDATASTRUCT cds = new COPYDATASTRUCT();
        cds.dwData = IntPtr.Zero;
        cds.cbData = buffer.Length;
        cds.lpData = Marshal.AllocHGlobal(buffer.Length);

        try {
            Marshal.Copy(buffer, 0, cds.lpData, buffer.Length);
            
            // WM_COPYDATAメッセージを送信
            Console.WriteLine("Sending command: " + command);
            IntPtr result = SendMessage(hWnd, WM_COPYDATA, IntPtr.Zero, ref cds);
            
            if (result == IntPtr.Zero) {
                Console.WriteLine("Command sent successfully.");
                return true;
            } else {
                Console.WriteLine("Command sent, result: " + result);
                return true;
            }
        } finally {
            Marshal.FreeHGlobal(cds.lpData);
        }
    }
}
"@

# コマンドを構築
$commandStr = ""

switch ($Command) {
    "loadprofile" {
        if ([string]::IsNullOrEmpty($ProfileName)) {
            Write-Host "Error: ProfileName is required for loadprofile command." -ForegroundColor Red
            Write-Host ""
            Write-Host "Usage examples:" -ForegroundColor Yellow
            Write-Host "  .\test-ipc.ps1 -Device 1 -ProfileName 'Default'"
            Write-Host "  .\test-ipc.ps1 -Device 2 -ProfileName 'CoreKeeperDS4'"
            Write-Host "  .\test-ipc.ps1 -Command query -Device 1"
            Write-Host "  .\test-ipc.ps1 -Command start"
            Write-Host "  .\test-ipc.ps1 -Command stop"
            Write-Host "  .\test-ipc.ps1 -Command disconnect -Device 1"
            exit 1
        }
        $commandStr = "loadprofile.$Device.$ProfileName"
    }
    "query" {
        # プロファイル名をクエリ
        $commandStr = "query.$Device.profilename"
    }
    "start" {
        $commandStr = "start"
    }
    "stop" {
        $commandStr = "stop"
    }
    "disconnect" {
        if ($Device -gt 0) {
            $commandStr = "disconnect.$Device"
        } else {
            $commandStr = "disconnect"
        }
    }
}

Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "DS4Windows IPC Test" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "Command: $Command" -ForegroundColor Green
Write-Host "Device: $Device" -ForegroundColor Green
if (-not [string]::IsNullOrEmpty($ProfileName)) {
    Write-Host "Profile: $ProfileName" -ForegroundColor Green
}
Write-Host "Full command string: $commandStr" -ForegroundColor Green
Write-Host ""

# コマンドを送信
$success = [DS4IPC]::SendCommand($commandStr)

if ($success) {
    Write-Host ""
    Write-Host "✓ IPC command sent successfully!" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "✗ Failed to send IPC command." -ForegroundColor Red
    exit 1
}
