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

using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF.DS4Forms.ViewModels.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DS4WinWPF.DS4Forms.ViewModels
{

    public class ProgramItem
    {
        private string path;
        private string path_lowercase;
        private string filename;
        private string title;
        private string title_lowercase;
        private AutoProfileEntity matchedAutoProfile;
        private ImageSource exeicon;
        private bool turnoff;

        public string Path
        {
            get => path;
            set
            {
                if (path == value) return;
                path = value;
                if (matchedAutoProfile != null)
                {
                    matchedAutoProfile.Path = value;
                }
            }
        }

        public string Title
        {
            get => title;
            set
            {
                if (title == value) return;
                title = value;
                if (matchedAutoProfile != null)
                {
                    matchedAutoProfile.Title = value;
                }

                TitleChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler TitleChanged;
        public AutoProfileEntity MatchedAutoProfile
        {
            get => matchedAutoProfile;
            set
            {
                matchedAutoProfile = value;
                if (matchedAutoProfile != null)
                {
                    title = matchedAutoProfile.Title ?? string.Empty;
                    title_lowercase = title.ToLower();
                }

                MatchedAutoProfileChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler MatchedAutoProfileChanged;
        //public delegate void AutoProfileHandler(ProgramItem sender, bool added);
        //public event AutoProfileHandler AutoProfileAction;
        public string Filename { get => filename; }
        public ImageSource Exeicon { get => exeicon; }

        public bool Turnoff
        {
            get
            {
                bool result = turnoff;
                if (matchedAutoProfile != null)
                {
                    result = matchedAutoProfile.Turnoff;
                }

                return result;
            }
            set
            {
                turnoff = value;
                if (matchedAutoProfile != null)
                {
                    matchedAutoProfile.Turnoff = value;
                }

                TurnoffChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler TurnoffChanged;

        public bool Exists
        {
            get => matchedAutoProfile != null;
        }
        public event EventHandler ExistsChanged;

        private int selectedIndexCon1 = 0;
        private int selectedIndexCon2 = 0;
        private int selectedIndexCon3 = 0;
        private int selectedIndexCon4 = 0;
        private int selectedIndexCon5 = 0;
        private int selectedIndexCon6 = 0;
        private int selectedIndexCon7 = 0;
        private int selectedIndexCon8 = 0;

        public int SelectedIndexCon1
        {
            get => selectedIndexCon1;
            set
            {
                if (selectedIndexCon1 == value) return;
                selectedIndexCon1 = value;
            }
        }

        public int SelectedIndexCon2
        {
            get => selectedIndexCon2;
            set
            {
                if (selectedIndexCon2 == value) return;
                selectedIndexCon2 = value;
            }
        }
        public int SelectedIndexCon3
        {
            get => selectedIndexCon3;
            set
            {
                if (selectedIndexCon3 == value) return;
                selectedIndexCon3 = value;
            }
        }
        public int SelectedIndexCon4
        {
            get => selectedIndexCon4;
            set
            {
                if (selectedIndexCon4 == value) return;
                selectedIndexCon4 = value;
            }
        }

        public int SelectedIndexCon5
        {
            get => selectedIndexCon5;
            set
            {
                if (selectedIndexCon5 == value) return;
                selectedIndexCon5 = value;
            }
        }

        public int SelectedIndexCon6
        {
            get => selectedIndexCon6;
            set
            {
                if (selectedIndexCon6 == value) return;
                selectedIndexCon6 = value;
            }
        }

        public int SelectedIndexCon7
        {
            get => selectedIndexCon7;
            set
            {
                if (selectedIndexCon7 == value) return;
                selectedIndexCon7 = value;
            }
        }

        public int SelectedIndexCon8
        {
            get => selectedIndexCon8;
            set
            {
                if (selectedIndexCon8 == value) return;
                selectedIndexCon8 = value;
            }
        }

        public Visibility ExpandedControllersVisible
        {
            get
            {
                Visibility temp = Visibility.Visible;
                if (!usingExpandedControllers)
                {
                    temp = Visibility.Collapsed;
                }

                return temp;
            }
        }

        // Phase7-Step10b W4: 旧 ControlService.USING_MAX_CONTROLLERS（静的シム）の置換。拡張コントローラー（8 台）の行を表示するかは、
        // 生成元（AutoProfilesViewModel）が IEnvironmentService.UsingMaxControllers から得た値を渡す。
        private readonly bool usingExpandedControllers;

        public ProgramItem(string path, bool usingExpandedControllers, AutoProfileEntity autoProfileEntity = null)
        {
            this.path = path;
            this.usingExpandedControllers = usingExpandedControllers;
            this.path_lowercase = path.ToLower();
            filename = System.IO.Path.GetFileNameWithoutExtension(path);
            this.matchedAutoProfile = autoProfileEntity;
            if (autoProfileEntity != null)
            {
                title = autoProfileEntity.Title;
                title_lowercase = title.ToLower();
                turnoff = autoProfileEntity.Turnoff;
            }

            if (File.Exists(path))
            {
                using (Icon ico = Icon.ExtractAssociatedIcon(path))
                {
                    exeicon = Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    exeicon.Freeze();
                }
            }

            MatchedAutoProfileChanged += ProgramItem_MatchedAutoProfileChanged;
        }

        private void ProgramItem_MatchedAutoProfileChanged(object sender, EventArgs e)
        {
            if (matchedAutoProfile == null)
            {
                selectedIndexCon1 = 0;
                selectedIndexCon2 = 0;
                selectedIndexCon3 = 0;
                selectedIndexCon4 = 0;
                selectedIndexCon5 = 0;
                selectedIndexCon6 = 0;
                selectedIndexCon7 = 0;
                selectedIndexCon8 = 0;
            }

            ExistsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}