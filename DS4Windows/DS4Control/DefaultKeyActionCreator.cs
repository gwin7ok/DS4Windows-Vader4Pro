using System;

namespace DS4Windows.Actions
{
    public class DefaultKeyActionCreator : IKeyActionCreator
    {
        public DS4Windows.KeyAction CreateKeyAction(DS4Windows.SpecialAction sa, int index)
        {
            return new DS4Windows.KeyAction(sa, index);
        }
    }
}
