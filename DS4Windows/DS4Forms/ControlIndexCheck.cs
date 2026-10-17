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

    public class ControlIndexCheck
    {
        public int TiltUp { get => (int)DS4Controls.GyroZNeg; }
        public int TiltDown { get => (int)DS4Controls.GyroZPos; }
        public int TiltLeft { get => (int)DS4Controls.GyroXPos; }
        public int TiltRight { get => (int)DS4Controls.GyroXNeg; }

        public int SwipeUp { get => (int)DS4Controls.SwipeUp; }
        public int SwipeDown { get => (int)DS4Controls.SwipeDown; }
        public int SwipeLeft { get => (int)DS4Controls.SwipeLeft; }
        public int SwipeRight { get => (int)DS4Controls.SwipeRight; }
        public int L2FullPull { get => (int)DS4Controls.L2FullPull; }
        public int R2FullPull { get => (int)DS4Controls.R2FullPull; }

        public int LSOuterBind { get => (int)DS4Controls.LSOuter; }
        public int RSOuterBind { get => (int)DS4Controls.RSOuter; }

        public int GyroSwipeLeft { get => (int)DS4Controls.GyroSwipeLeft; }
        public int GyroSwipeRight { get => (int)DS4Controls.GyroSwipeRight; }
        public int GyroSwipeUp { get => (int)DS4Controls.GyroSwipeUp; }
        public int GyroSwipeDown { get => (int)DS4Controls.GyroSwipeDown; }
    }
}