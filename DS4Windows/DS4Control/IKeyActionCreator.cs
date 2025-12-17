using System;

namespace DS4Windows.Actions
{
    public interface IKeyActionCreator
    {
        DS4Windows.KeyAction CreateKeyAction(DS4Windows.SpecialAction sa, int index);
    }
}
