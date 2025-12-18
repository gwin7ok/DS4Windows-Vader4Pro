using System;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Represents an output operation (key press, virtual button, macro, etc.).
    /// </summary>
    public interface IOutputAction
    {
        string Id { get; }
        void Execute(IOutputContext ctx);
        void Stop(IOutputContext ctx);
    }
}
