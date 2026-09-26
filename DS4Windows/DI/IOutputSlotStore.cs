using DS4Windows;

namespace DS4Windows.DI
{
    /// <summary>
    /// 出力スロット割り当て（OutputSlots.xml）の永続化を抽象化するインターフェース。
    /// </summary>
    public interface IOutputSlotStore
    {
        bool Load(OutputSlotManager slotManager);
        bool Save(OutputSlotManager slotManager);
    }
}
