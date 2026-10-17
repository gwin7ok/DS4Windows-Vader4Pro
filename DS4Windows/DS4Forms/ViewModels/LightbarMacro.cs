/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WinWPF.DS4Forms.ViewModels
{

    public class LightbarMacro
    {
        private bool _active;
        public bool Active
        {
            get => _active;
            set
            {
                _active = value;
                ActiveChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler ActiveChanged;

        public ObservableCollection<LightbarMacroElement> Macro { get; set; }

        private LightbarMacroTrigger _trigger;
        public LightbarMacroTrigger Trigger
        {
            get => _trigger;
            set
            {
                _trigger = value;
                TriggerChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler TriggerChanged;

        private bool _cancelCurrent;
        /// <summary>
        ///     Indicates whether this macro should take precedence over a currently running one
        /// </summary>
        public bool CancelCurrent
        {
            get => _cancelCurrent;
            set
            {
                _cancelCurrent = value;
                CancelCurrentChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler CancelCurrentChanged;

        public LightbarMacro()
        {
            Active = false;
            Macro = [];
            Trigger = LightbarMacroTrigger.Press;
            CancelCurrent = false;
        }

        /// <summary>
        ///     Automatically parses a string to this object
        /// </summary>
        /// <param name="macroString">Correctly formatted string, see <see cref="Compile"/></param>
        public LightbarMacro(string macroString)
        {
            Parse(macroString);
        }

        public LightbarMacro(bool active, LightbarMacroElement[] macro, LightbarMacroTrigger trigger, bool cancelCurrent)
        {
            Active = active;
            Trigger = trigger;
            Macro = new ObservableCollection<LightbarMacroElement>(macro);
            CancelCurrent = cancelCurrent;
        }

        /// <summary>
        ///     <para>Compiles this object to a string that can be serialised and is used in the profile XML.</para>
        ///     <para>Example: <c>True/255,255,255:100;255,0,0:50;/Press/False</c>, which means:</para>
        ///     <para><see cref="Active"/>: <c>true</c>,</para>
        ///     <para><see cref="Macro"/>: 2 elements: #FFFFFF for 100ms and #FF0000 for 50ms</para>
        ///     <para><see cref="Trigger"/>: <see cref="LightbarMacroTrigger.Press"/></para>
        ///     <para><see cref="CancelCurrent"/>: <c>false</c></para>
        /// </summary>
        /// <returns>Serialisable string</returns>
        public string Compile()
        {
            var sb = new StringBuilder();

            sb.Append(Active.ToString());

            // / between active flag, macro, trigger and cancellation flag
            sb.Append('/');

            var firstAppended = false;
            foreach (var element in Macro)
            {
                // ; after each element of the macro
                if (firstAppended)
                    sb.Append(';');
                else
                    firstAppended = true;

                // format of r,g,b to comply with TryParse method on DS4Color class
                sb.Append(element.Color.red);
                sb.Append(',');
                sb.Append(element.Color.green);
                sb.Append(',');
                sb.Append(element.Color.blue);
                // : between the colour and the timespan
                sb.Append(':');
                sb.Append(element.Length);
            }

            sb.Append('/');
            sb.Append(Trigger.ToString());

            sb.Append('/');
            sb.Append(CancelCurrent.ToString());

            return sb.ToString();
        }

        public void Parse(string macro)
        {
            var parsed = GetMacroFromString(macro);
            Active = parsed.Active;
            Macro = parsed.Macro;
            Trigger = parsed.Trigger;
            CancelCurrent = parsed.CancelCurrent;
        }

        public static LightbarMacro GetMacroFromString(string macro)
        {
            // parsing the string in general + active flag
            var fieldSplit = macro.Split('/');
            if (fieldSplit.Length is < 3 or > 4) throw new ArgumentException($"Provided string doesn't comply with the format (too few or too many sections separated with '/').\n{macro}");

            // prior to 3.9.2 cancellation was not available, so the string was different, check is done for backwards compatibility
            var oldFormat = fieldSplit.Length == 3;

            if (!bool.TryParse(fieldSplit[0], out var active)) throw new ArgumentException("Provided string doesn't comply with the format (cannot parse active flag).");

            var macroSplit = fieldSplit[1].Split(';');
            List<LightbarMacroElement> macroList = [];
            foreach (var macroElement in macroSplit)
            {
                if (string.IsNullOrEmpty(macroElement)) return new LightbarMacro();
                var elementSplit = macroElement.Split(':');
                var color = elementSplit[0];
                var length = elementSplit[1];
                DS4Color parsedColor = new();
                if (!DS4Color.TryParse(color, ref parsedColor)
                    || !uint.TryParse(length, out var parsedLength))
                    throw new ArgumentException("Provided lightbar macro string doesn't comply with the format (cannot parse color or length).");
                macroList.Add(new LightbarMacroElement(parsedColor, parsedLength));
            }

            Enum.TryParse<LightbarMacroTrigger>(fieldSplit[2], out var lightbarMacroTrigger);

            bool cancel;
            // before the choice of cancellation was introduced, default behaviour was to cancel the current macro
            if (oldFormat)
                cancel = true;
            else
                if (!bool.TryParse(fieldSplit[3], out cancel))
                    throw new ArgumentException("Provided lightbar macro string doesn't comply with the format (cannot parse cancellation flag).");

            return new LightbarMacro(active, macroList.ToArray(), lightbarMacroTrigger, cancel);
        }
    }
}
