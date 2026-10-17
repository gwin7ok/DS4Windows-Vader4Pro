using System;
using System.Collections.Generic;
using DS4Windows;

namespace DS4WinWPF.DS4Control;

public class Debouncer(TimeSpan duration)
{
    private readonly Dictionary<string, DebouncerInstance> _debouncers = new();
    // Phase6-Step12: SetDuration で更新される現在の持続時間。コンストラクタ引数 duration は生成時の値のまま変わらず、
    // 0 で生成したあとに SetDuration で 0 より大きくしても、ProcessInput の先頭の判定が古い値（0）を見て、デバウンスが働かなかった。
    private TimeSpan _duration = duration;

    public void AddDebouncer(string name)
    {
        _debouncers[name] = new DebouncerInstance(_duration);
    }

    public DS4State ProcessInput(DS4State cState)
    {
        if (_duration.TotalMilliseconds == 0) return cState;

        DS4State modifiedState = new();
        cState.CopyTo(modifiedState);
        foreach (var key in _debouncers.Keys)
        {
            var field = typeof(DS4State).GetField(key)!;
            var current = (bool)field.GetValue(modifiedState)!;
            var debounced = _debouncers[key].ProcessInput(current, cState.ReportTimeStamp);
            field.SetValue(modifiedState, debounced);
        }

        return modifiedState;
    }

    public void SetDuration(TimeSpan newDuration)
    {
        _duration = newDuration;
        foreach (var debouncer in _debouncers.Values)
        {
            debouncer.Duration = newDuration;
        }
    }

    private class DebouncerInstance(TimeSpan duration)
    {
        private bool _previousState;
        private bool _currentlyDebouncing;
        private DateTime _debounceStartTime;

        public TimeSpan Duration { get; set; } = duration;

        /// <summary>
        ///     Processes the input and applies debouncing if required.
        /// </summary>
        /// <param name="input"><c>bool</c> indicating the state of the button</param>
        /// <param name="timestamp">Current timestamp in <c>DateTime.Ticks</c></param>
        /// <returns>Processed input with debouncing applied</returns>
        public bool ProcessInput(bool input, DateTime timestamp)
        {
            if (_currentlyDebouncing)
            {
                return Debounce(input, timestamp);
            }

            if (_previousState != input)
            {
                StartDebouncing(input, timestamp);
                return true;
            }

            _previousState = input;
            return input;
        }

        private void StartDebouncing(bool input, DateTime timestamp)
        {
            _currentlyDebouncing = true;
            _debounceStartTime = timestamp;
            Debounce(input, timestamp);
        }

        private void StopDebouncing()
        {
            _currentlyDebouncing = false;
        }

        private bool Debounce(bool reading, DateTime timestamp)
        {

            // if the duration hasn't been reached yet, we return true as if the button was pressed all this time
            var span = timestamp - _debounceStartTime;
            if (span.TotalMilliseconds < Duration.TotalMilliseconds) return true;

            StopDebouncing();
            return reading;
        }
    }
}