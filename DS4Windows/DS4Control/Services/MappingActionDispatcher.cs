using DS4Windows.DI;

namespace DS4Windows
{
    /// <summary>
    /// IMappingActionDispatcher の標準実装。
    /// 既存の Mapping.DispatchProfileActionEdge static メソッドへ安全に中継します。
    /// </summary>
    public class MappingActionDispatcher : IMappingActionDispatcher
    {
        public void DispatchProfileActionEdge(SpecialAction action, int deviceIndex, bool start)
        {
            if (action == null || deviceIndex < 0 || deviceIndex >= 4)
                return;

            Mapping.DispatchProfileActionEdge(action, deviceIndex, start);
        }
    }
}
