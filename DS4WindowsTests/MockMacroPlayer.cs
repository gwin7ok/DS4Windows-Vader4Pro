using System;
using System.Collections.Generic;
using System.Threading;
using DS4Windows;
using DS4Windows.Actions;

namespace DS4WindowsTests
{
    /// <summary>
    /// C3 MacroAction 単体テスト用の IMacroPlayer モック
    /// </summary>
    public class MockMacroPlayer : IMacroPlayer
    {
        public class PlayCall
        {
            public int DeviceIndex { get; set; }
            public SpecialAction Action { get; set; }
        }

        public List<PlayCall> PlayCalls { get; } = new List<PlayCall>();
        public List<int> StopCalls { get; } = new List<int>();
        public bool IsPlayingResult { get; set; } = false;

        public bool IsPlaying(int deviceIndex) => IsPlayingResult;

        public void Play(int deviceIndex, SpecialAction action, CancellationToken cancellationToken = default)
        {
            PlayCalls.Add(new PlayCall
            {
                DeviceIndex = deviceIndex,
                Action = action
            });
        }

        public void Stop(int deviceIndex)
        {
            StopCalls.Add(deviceIndex);
        }

        public void Reset()
        {
            PlayCalls.Clear();
            StopCalls.Clear();
            IsPlayingResult = false;
        }
    }
}