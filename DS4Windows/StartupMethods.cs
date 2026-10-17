/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.TaskScheduler;
using Task = Microsoft.Win32.TaskScheduler.Task;

namespace DS4WinWPF
{
    [System.Security.SuppressUnmanagedCodeSecurity]
    public static class StartupMethods
    {
        public static string lnkpath = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\DS4Windows.lnk";
        // Phase6-Step10-1-5（決定4＝A）: 静的クラスのまま、プログラムフォルダと実行ファイルの場所は引数で受け取る
        // （呼び出し側が IPathService.ProgramDirectory／ExecutablePath を渡す。置換前は Global.exedirpath／exelocation／exeFileName を直接参照していた）。
        private static string GetTaskBatPath(string programDirectory) => Path.Combine(programDirectory, "task.bat");

        public static bool HasStartProgEntry()
        {
            // Exception handling should not be needed here. Method handles most cases
            bool exists = File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\DS4Windows.lnk");
            return exists;
        }

        public static bool HasTaskEntry()
        {
            TaskService ts = new TaskService();
            Task tasker = ts.FindTask("RunDS4Windows");
            return tasker != null;
        }

        /// <param name="exeLocation">実行ファイルのフルパス（IPathService.ExecutablePath）</param>
        /// <param name="programDirectory">プログラムフォルダ（IPathService.ProgramDirectory）</param>
        public static void WriteStartProgEntry(string exeLocation, string programDirectory)
        {
            Type t = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")); // Windows Script Host Shell Object
            dynamic shell = Activator.CreateInstance(t);
            try
            {
                var lnk = shell.CreateShortcut(lnkpath);
                try
                {
                    string app = exeLocation;
                    lnk.TargetPath = exeLocation;
                    lnk.Arguments = "-m";
                    // Need to add the DS4Windows directory as cwd or
                    // language assemblies cannot be discovered
                    lnk.WorkingDirectory = programDirectory;

                    //lnk.TargetPath = Assembly.GetExecutingAssembly().Location;
                    //lnk.Arguments = "-m";
                    lnk.IconLocation = app.Replace('\\', '/');
                    lnk.Save();
                }
                finally
                {
                    Marshal.FinalReleaseComObject(lnk);
                }
            }
            finally
            {
                Marshal.FinalReleaseComObject(shell);
            }
        }

        public static void DeleteStartProgEntry()
        {
            if (File.Exists(lnkpath) && !new FileInfo(lnkpath).IsReadOnly)
            {
                File.Delete(lnkpath);
            }
        }

        /// <param name="programDirectory">プログラムフォルダ（IPathService.ProgramDirectory）</param>
        public static void DeleteOldTaskEntry(string programDirectory)
        {
            string taskBatPath = GetTaskBatPath(programDirectory);
            TaskService ts = new TaskService();
            Task tasker = ts.FindTask("RunDS4Windows");
            if (tasker != null)
            {
                foreach(Microsoft.Win32.TaskScheduler.Action act in tasker.Definition.Actions)
                {
                    if (act.ActionType == TaskActionType.Execute)
                    {
                        ExecAction temp = act as ExecAction;
                        if (temp.Path != taskBatPath)
                        {
                            ts.RootFolder.DeleteTask("RunDS4Windows");
                            break;
                        }
                    }
                }
            }
        }

        public static bool CanWriteStartEntry()
        {
            bool result = false;
            if (!new FileInfo(lnkpath).IsReadOnly)
            {
                result = true;
            }

            return result;
        }

        /// <param name="programDirectory">プログラムフォルダ（IPathService.ProgramDirectory）</param>
        /// <param name="exeFileName">実行ファイル名（Path.GetFileName(IPathService.ExecutablePath)）</param>
        public static void WriteTaskEntry(string programDirectory, string exeFileName)
        {
            DeleteTaskEntry();

            // Create new version of task.bat file using current exe
            // filename. Allow dynamic file
            RefreshTaskBat(programDirectory, exeFileName);

            TaskService ts = new TaskService();
            TaskDefinition td = ts.NewTask();
            td.Triggers.Add(new LogonTrigger());
            string dir = programDirectory;
            td.Actions.Add(new ExecAction($@"{dir}\task.bat",
                "",
                dir));

            td.Principal.RunLevel = TaskRunLevel.Highest;
            td.Settings.StopIfGoingOnBatteries = false;
            td.Settings.DisallowStartIfOnBatteries = false;
            ts.RootFolder.RegisterTaskDefinition("RunDS4Windows", td);
        }

        public static void DeleteTaskEntry()
        {
            TaskService ts = new TaskService();
            Task tasker = ts.FindTask("RunDS4Windows");
            if (tasker != null)
            {
                ts.RootFolder.DeleteTask("RunDS4Windows");
            }
        }

        /// <param name="exeLocation">実行ファイルのフルパス（IPathService.ExecutablePath）</param>
        public static bool CheckStartupExeLocation(string exeLocation)
        {
            string lnkprogpath = ResolveShortcut(lnkpath);
            return lnkprogpath != exeLocation;
        }

        public static void LaunchOldTask()
        {
            TaskService ts = new TaskService();
            Task tasker = ts.FindTask("RunDS4Windows");
            if (tasker != null)
            {
                tasker.Run("");
            }
        }

        private static string ResolveShortcut(string filePath)
        {
            Type t = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")); // Windows Script Host Shell Object
            dynamic shell = Activator.CreateInstance(t);
            string result;

            try
            {
                var shortcut = shell.CreateShortcut(filePath);
                result = shortcut.TargetPath;
                Marshal.FinalReleaseComObject(shortcut);
            }
            catch (COMException)
            {
                // A COMException is thrown if the file is not a valid shortcut (.lnk) file 
                result = null;
            }
            finally
            {
                Marshal.FinalReleaseComObject(shell);
            }

            return result;
        }

        private static void RefreshTaskBat(string programDirectory, string exeFileName)
        {
            string dir = programDirectory;
            string path = $@"{dir}\task.bat";
            FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
            using (StreamWriter w = new StreamWriter(fileStream))
            {
                string temp = string.Empty;
                w.WriteLine("@echo off"); // Turn off echo
                w.WriteLine("SET mypath=\"%~dp0\"");
                temp = $"cmd.exe /c start \"RunDS4Windows\" %mypath%\\{exeFileName} -m";
                w.WriteLine(temp);
                w.WriteLine("exit");
            }
        }
    }
}
