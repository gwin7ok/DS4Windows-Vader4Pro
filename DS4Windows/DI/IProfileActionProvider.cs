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
    }
}