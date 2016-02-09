using System;
using Midi.Devices;

namespace Midi.Messages
{
    /// <summary>
    ///     Base class for messages relevant to a specific device.
    /// </summary>
    public abstract class DeviceMessage : Message
    {
        /// <summary>
        ///     Protected constructor.
        /// </summary>
        protected DeviceMessage(DeviceBase device, float time)
            : base(time)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }
            Device = device;
        }

        /// <summary>
        ///     The device from which this message originated, or for which it is destined.
        /// </summary>
        public DeviceBase Device { get; }
    }
}