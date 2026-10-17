using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using NonFormTimer = System.Timers.Timer;
using DS4WinWPF.DS4Forms.ViewModels;
using DS4Windows;
using DS4Windows.DI;
using System.ComponentModel;
using System.Windows.Forms;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using UserControl = System.Windows.Controls.UserControl;
using DS4Windows.Actions;

namespace DS4WinWPF.DS4Forms
{

    public class ResourcePaths
    {
        // Phase7-Step10a W3（Q1＝A）: 旧 Global.RESOURCES_PREFIX／ASSEMBLY_RESOURCE_PREFIX（値は同じ）。画面側の資源パスの組み立てに使う。
        public const string ResourcesPrefix = "/DS4Windows;component/Resources";
        public const string AssemblyResourcePrefix = "pack://application:,,,/DS4Windows;";

        public string SizePNG { get => $"{ResourcePaths.ResourcesPrefix}/size.png"; }
        public string DS4ConfigPNG { get => $"{ResourcePaths.ResourcesPrefix}/DS4 Config.png"; }
        public string DS4LightbarPNG { get => $"{ResourcePaths.ResourcesPrefix}/DS4 lightbar.png"; }
        public string DS4ConfigRSPNG { get => $"{ResourcePaths.ResourcesPrefix}/DS4-Config_RS.png"; }
        public string RainbowPNG { get => $"{ResourcePaths.ResourcesPrefix}/rainbow.png"; }
    }
}