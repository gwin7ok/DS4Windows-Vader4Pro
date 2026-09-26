using DS4Windows;

namespace DS4Windows.DI
{
    /// <summary>
    /// Mapping サブシステムへのアクション発火・エッジディスパッチを抽象化するインターフェース。
    /// 巨大ファイル Mapping.cs への直接結合を遮断し、連鎖処理のテスト容易性と責務境界を確立します。
    /// </summary>
    public interface IMappingActionDispatcher
    {
        void DispatchProfileActionEdge(SpecialAction action, int deviceIndex, bool start);
    }
}
