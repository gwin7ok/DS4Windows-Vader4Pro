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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.DualShock4;

namespace DS4Windows
{
    abstract class DS4OutDevice : OutputDevice
    {
        internal const byte RUMBLE_FEATURE_FLAG = 0x01;
        internal const byte LIGHTBAR_FEATURE_FLAG = 0x02;
        internal const byte FLASH_FEATURE_FLAG = 0x04;

        public const string devtype = "DS4";

        public IDualShock4Controller cont;
        // The ViGEm event types are marked obsolete in newer client versions.
        // Suppress obsolete warnings here while keeping compatibility with the
        // current usage pattern. Consider migrating to AwaitRawOutputReport()
        // in a future refactor.
    #pragma warning disable CS0618
        //public DualShock4FeedbackReceivedEventHandler forceFeedbackCall;
        public System.Collections.Generic.Dictionary<int, Nefarius.ViGEm.Client.Targets.DualShock4.DualShock4FeedbackReceivedEventHandler> forceFeedbacksDict =
            new System.Collections.Generic.Dictionary<int, Nefarius.ViGEm.Client.Targets.DualShock4.DualShock4FeedbackReceivedEventHandler>();
    #pragma warning restore CS0618

        protected bool canUseAwaitOutputBuffer = false;
        public bool CanUseAwaitOutputBuffer => canUseAwaitOutputBuffer;

        public DS4OutDevice(ViGEmClient client)
        {
            cont = client.CreateDualShock4Controller();
            //cont = client.CreateDualShock4Controller(0x054C, 0x09CC);
            cont.AutoSubmitReport = false;
        }

        public override void Connect()
        {
            cont.Connect();
            connected = true;
        }
        public override void Disconnect()
        {
            // Remove feedback handlers before Disconnect
            RemoveFeedbacks();

            connected = false;
            cont.Disconnect();
            //cont.Dispose();
            cont = null;
        }
        public override string GetDeviceType() => devtype;

        public override void RemoveFeedbacks()
        {
#pragma warning disable CS0618
            foreach (System.Collections.Generic.KeyValuePair<int, Nefarius.ViGEm.Client.Targets.DualShock4.DualShock4FeedbackReceivedEventHandler> pair in forceFeedbacksDict)
            {
                cont.FeedbackReceived -= pair.Value;
            }

            forceFeedbacksDict.Clear();
#pragma warning restore CS0618
        }

        public override void RemoveFeedback(int inIdx)
        {
#pragma warning disable CS0618
            if (forceFeedbacksDict.TryGetValue(inIdx, out Nefarius.ViGEm.Client.Targets.DualShock4.DualShock4FeedbackReceivedEventHandler handler))
            {
                cont.FeedbackReceived -= handler;
                forceFeedbacksDict.Remove(inIdx);
            }
#pragma warning restore CS0618
        }

        public virtual void StartOutputBufferThread()
        {
        }
    }
}
