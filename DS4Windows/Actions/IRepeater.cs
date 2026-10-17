using System;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Abstracts periodic invocation used for key repeats.
    /// </summary>
    public interface IRepeater : IDisposable
    {
        void Start(TimeSpan initialDelay, TimeSpan interval, System.Action tickAction);
        void Stop();
    }
}
