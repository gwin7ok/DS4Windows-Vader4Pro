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

namespace DS4Windows
{
    /// <summary>
    /// ウィンドウレイアウトとUI要素の初期サイズを一元管理するクラス
    /// すべてのウィンドウサイズ、ポジション、列幅などの初期値はここで定義する
    /// </summary>
    public static class WindowLayoutDefaults
    {
        // ==============================================
        // MainWindow 初期サイズ
        // ==============================================
        /// <summary>
        /// MainWindow (メインウィンドウ) の初期幅
        /// Profiles.xml が無い状態で最初に起動したときのウィンドウ幅
        /// 「ウィンドウサイズ初期化」ボタンでもこのサイズに戻る
        /// </summary>
        public const int MAIN_WINDOW_WIDTH = 1740;

        /// <summary>
        /// MainWindow (メインウィンドウ) の初期高さ
        /// Profiles.xml が無い状態で最初に起動したときのウィンドウ高さ
        /// 「ウィンドウサイズ初期化」ボタンでもこのサイズに戻る
        /// </summary>
        public const int MAIN_WINDOW_HEIGHT = 1270;

        /// <summary>
        /// MainWindow (メインウィンドウ) の初期X位置
        /// </summary>
        public const int MAIN_WINDOW_LOCATION_X = 0;

        /// <summary>
        /// MainWindow (メインウィンドウ) の初期Y位置
        /// </summary>
        public const int MAIN_WINDOW_LOCATION_Y = 0;

        // ==============================================
        // ProfileEditor ウィンドウサイズ
        // ==============================================
        /// <summary>
        /// ProfileEditor (プロファイル編集画面) を開く際のウィンドウ幅
        /// MainWindow が小さい場合に自動的にこのサイズまで拡張される
        /// </summary>
        public const int PROFILE_EDITOR_WIDTH = 1740;

        /// <summary>
        /// ProfileEditor (プロファイル編集画面) を開く際のウィンドウ高さ
        /// MainWindow が小さい場合に自動的にこのサイズまで拡張される
        /// </summary>
        public const int PROFILE_EDITOR_HEIGHT = 1270;

        // ==============================================
        // ProfileEditor レイアウト (左右分割、列幅など)
        // ==============================================
        /// <summary>
        /// ProfileEditor 左側領域の初期幅 (コントローラー設定側)
        /// </summary>
        public const int PROFILE_EDITOR_LEFT_WIDTH = 760;

        /// <summary>
        /// ProfileEditor 右側領域の初期幅 (Special Actions側)
        /// </summary>
        public const int PROFILE_EDITOR_RIGHT_WIDTH = 600;

        /// <summary>
        /// Special Actions ListView の Name列の初期幅
        /// </summary>
        public const int SPECIAL_ACTION_NAME_COL_WIDTH = 310;

        /// <summary>
        /// Special Actions ListView の Trigger列の初期幅
        /// </summary>
        public const int SPECIAL_ACTION_TRIGGER_COL_WIDTH = 150;

        /// <summary>
        /// Special Actions ListView の Detail列の初期幅
        /// </summary>
        public const int SPECIAL_ACTION_DETAIL_COL_WIDTH = 220;

        /// <summary>
        /// Special Actions ListView の Active列の初期幅
        /// (プロファイルに永続化しない列)
        /// </summary>
        public const int SPECIAL_ACTION_ACTIVE_COL_WIDTH = 32;

        // ==============================================
        // Controller タブの ListView 列幅
        // ==============================================
        /// <summary>
        /// Controller タブの Index列 初期幅
        /// </summary>
        public const int CONTROLLER_INDEX_COL_WIDTH = 30;

        /// <summary>
        /// Controller タブの ID列 初期幅
        /// </summary>
        public const int CONTROLLER_ID_COL_WIDTH = 250;

        /// <summary>
        /// Controller タブの Status列 初期幅
        /// </summary>
        public const int CONTROLLER_STATUS_COL_WIDTH = 65;

        /// <summary>
        /// Controller タブの Exclusive列 初期幅
        /// </summary>
        public const int CONTROLLER_EXCLUSIVE_COL_WIDTH = 65;

        /// <summary>
        /// Controller タブの Battery列 初期幅
        /// </summary>
        public const int CONTROLLER_BATTERY_COL_WIDTH = 100;

        /// <summary>
        /// Controller タブの Select Profile列 初期幅
        /// </summary>
        public const int CONTROLLER_SELECTPROFILE_COL_WIDTH = 220;

        /// <summary>
        /// Controller タブの Edit列 初期幅
        /// </summary>
        public const int CONTROLLER_EDIT_COL_WIDTH = 95;

        /// <summary>
        /// Controller タブの Linked Profile列 初期幅
        /// </summary>
        public const int CONTROLLER_LINKED_PROFILE_COL_WIDTH = 220;

        /// <summary>
        /// Controller タブの Link Prof ID列 初期幅
        /// </summary>
        public const int CONTROLLER_LINK_PROF_ID_COL_WIDTH = 130;

        /// <summary>
        /// Controller タブの Custom Color列 初期幅
        /// </summary>
        public const int CONTROLLER_CUSTOMCOLOR_COL_WIDTH = 160;
    }
}
