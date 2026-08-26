using System;
using DS4Windows;

namespace DS4Windows.Actions
{
    /// <summary>
    /// プロファイル切り替えおよび一時プロファイルからの復帰を抽象化するインターフェース
    /// </summary>
    public interface IProfileSwitcher
    {
        /// <summary>
        /// 指定されたデバイスに対して SpecialAction に基づくプロファイル切り替えを実行します。
        /// </summary>
        /// <param name="deviceIndex">コントローラーのデバイスインデックス（0〜3）</param>
        /// <param name="action">プロファイル設定を含む SpecialAction</param>
        void SwitchProfile(int deviceIndex, SpecialAction action);

        /// <summary>
        /// 一時プロファイルから元の通常プロファイルへの復帰を実行します。
        /// </summary>
        /// <param name="deviceIndex">コントローラーのデバイスインデックス（0〜3）</param>
        void RestoreProfile(int deviceIndex);
    }
}