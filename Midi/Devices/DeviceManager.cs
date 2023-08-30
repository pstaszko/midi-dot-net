using Midi.Win32;
using System;
using System.Collections.ObjectModel;

namespace Midi.Devices
{
    /// <summary>
    ///     MIDI Device Manager, providing access to available input/output midi devices
    /// </summary>
    public static class DeviceManager
    {
        private static readonly object InputDeviceLock = new object();
        private static readonly object OutputDeviceLock = new object();
        private static IInputDevice[] _inputDevices;
        private static IOutputDevice[] _outputDevices;

        /// <summary>
        ///     List of input devices installed on this system.
        /// </summary>
        public static ReadOnlyCollection<IInputDevice> InputDevices
        {
            get {
                if (_inputDevices == null) {
                    lock (InputDeviceLock) {
                        if (_inputDevices == null) {
                            var inputDevices = EnumerateInputDevices();
                            _inputDevices = inputDevices;
                        }
                    }
                }

                return new ReadOnlyCollection<IInputDevice>(_inputDevices);
            }
        }

        /// <summary>
        ///     List of devices installed on this system.
        /// </summary>
        public static ReadOnlyCollection<IOutputDevice> OutputDevices
        {
            get {
                if (_outputDevices == null) {
                    lock (OutputDeviceLock) {
                        if (_outputDevices == null) {
                            var outputDevices = EnumerateOutputDevices();
                            _outputDevices = outputDevices;
                        }
                    }
                }

                return new ReadOnlyCollection<IOutputDevice>(_outputDevices);
            }
        }

        /// <summary>
        ///     Refresh the list of input devices
        /// </summary>
        public static void UpdateInputDevices()
        {
            lock (InputDeviceLock) {
                _inputDevices = null;
            }
        }

        /// <summary>
        ///     Refresh the list of input devices
        /// </summary>
        public static void UpdateOutputDevices()
        {
            lock (OutputDeviceLock) {
                _outputDevices = null;
            }
        }

        private static IInputDevice[] EnumerateInputDevices()
        {
            var inDevs = Win32API.midiInGetNumDevs();
            var result = new IInputDevice[inDevs];
            for (uint deviceId = 0; deviceId < inDevs; deviceId++) {
                MIDIINCAPS caps;
                Win32API.midiInGetDevCaps((UIntPtr)deviceId, out caps);
                result[deviceId] = new InputDevice((UIntPtr)deviceId, caps);
            }
            return result;
        }

        private static IOutputDevice[] EnumerateOutputDevices()
        {
            var outDevs = Win32API.midiOutGetNumDevs();
            var result = new IOutputDevice[outDevs];
            for (uint deviceId = 0; deviceId < outDevs; deviceId++) {
                MidiOutCaps caps;
                Win32API.midiOutGetDevCaps((UIntPtr)deviceId, out caps);
                result[deviceId] = new OutputDevice((UIntPtr)deviceId, caps);
            }
            return result;
        }
    }
}