using System.Collections.Generic;
using DS4Windows;

namespace DS4Windows.DI
{
    public interface IProfileActionProvider
    {
        IReadOnlyList<string> GetProfileActionNames(int deviceIndex);
        SpecialAction GetProfileAction(int deviceIndex, string actionName);

        // ---- Phase6-Step2-5 (PR-5): プロファイルに設定されたスペシャルアクション数（毎レポート評価。割り当てなし）----
        int GetProfileActionCount(int deviceIndex);

        // ---- Phase6-Step3-1: プロファイルに設定されたスペシャルアクション名から定義済みインデックスを引く（Global への薄い委譲）----
        int GetProfileActionIndexOf(int deviceIndex, string actionName);

        /// <summary>
        /// プロファイルに設定されたスペシャルアクション名の一覧を、コピーを作らずそのまま返す
        /// （<see cref="GetProfileActionNames"/> は毎回 <c>ToArray()</c> するため、ホットパスでの
        /// 割り当てを避けたい呼び出し元は本メンバを使う）。呼び出し元は返された参照を変更しないこと。
        /// </summary>
        IReadOnlyList<string> GetProfileActionsRaw(int deviceIndex);
    }
}