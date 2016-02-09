namespace Midi.Devices
{
    /// <summary>
    ///     Common base class for input and output devices.
    /// </summary>
    /// This base class exists mainly so that input and output devices can both go into the same
    /// kinds of MidiMessages.
    public class DeviceBase
    {
        /// <summary>
        ///     Protected constructor.
        /// </summary>
        /// <param name="name">The name of this device.</param>
        protected DeviceBase(string name)
        {
            Name = name;
        }

        /// <summary>
        ///     The name of this device.
        /// </summary>
        public string Name { get; }
    }
}