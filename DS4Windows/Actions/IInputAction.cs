using System;
using DS4Windows;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Evaluates controller state and produces a trigger context when conditions are met.
    /// </summary>
    public interface IInputAction
    {
        string Name { get; }
        /// <summary>Evaluate current device state; return a trigger context or null.</summary>
        ITriggerContext Evaluate(DS4State state);
        /// <summary>Reset internal state (e.g., timing windows).</summary>
        void Reset();
    }
}
