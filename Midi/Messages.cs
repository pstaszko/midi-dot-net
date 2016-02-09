// Copyright (c) 2009, Tom Lokovic
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//
//     * Redistributions of source code must retain the above copyright notice,
//       this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

using System;

namespace Midi
{
    /// <summary>
    ///     Base class for all MIDI messages.
    /// </summary>
    public abstract class Message
    {
        /// <summary>
        ///     Protected constructor.
        /// </summary>
        /// <param name="time">The timestamp for this message.</param>
        protected Message(float time)
        {
            Time = time;
        }

        /// <summary>
        ///     Milliseconds since the music started.
        /// </summary>
        public float Time { get; }

        /// <summary>
        ///     Sends this message immediately.
        /// </summary>
        public abstract void SendNow();

        /// <summary>
        ///     Returns a copy of this message, shifted in time by the specified amount.
        /// </summary>
        public abstract Message MakeTimeShiftedCopy(float delta);
    }

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

    /// <summary>
    ///     Base class for messages relevant to a specific device channel.
    /// </summary>
    public abstract class ChannelMessage : DeviceMessage
    {
        /// <summary>
        ///     Protected constructor.
        /// </summary>
        protected ChannelMessage(DeviceBase device, Channel channel, float time)
            : base(device, time)
        {
            channel.Validate();
            Channel = channel;
        }

        /// <summary>
        ///     Channel.
        /// </summary>
        public Channel Channel { get; }
    }

    /// <summary>
    ///     Base class for messages relevant to a specific note.
    /// </summary>
    public abstract class NoteMessage : ChannelMessage
    {
        /// <summary>
        ///     Protected constructor.
        /// </summary>
        protected NoteMessage(DeviceBase device, Channel channel, Pitch pitch, int velocity,
            float time)
            : base(device, channel, time)
        {
            if (!pitch.IsInMidiRange())
            {
                throw new ArgumentOutOfRangeException(nameof(pitch));
            }
            if (velocity < 0 || velocity > 127)
            {
                throw new ArgumentOutOfRangeException(nameof(velocity));
            }
            Pitch = pitch;
            Velocity = velocity;
        }

        /// <summary>The pitch for this note message.</summary>
        public Pitch Pitch { get; }

        /// <summary>
        ///     Velocity, 0..127.
        /// </summary>
        public int Velocity { get; }
    }

    /// <summary>
    ///     Note On message.
    /// </summary>
    public class NoteOnMessage : NoteMessage
    {
        /// <summary>
        ///     Constructs a Note On message.
        /// </summary>
        /// <param name="device">The device associated with this message.</param>
        /// <param name="channel">Channel, 0..15, 10 reserved for percussion.</param>
        /// <param name="pitch">The pitch for this note message.</param>
        /// <param name="velocity">Velocity, 0..127.</param>
        /// <param name="time">The timestamp for this message.</param>
        public NoteOnMessage(DeviceBase device, Channel channel, Pitch pitch, int velocity,
            float time)
            : base(device, channel, pitch, velocity, time)
        {
        }

        /// <summary>
        ///     Sends this message immediately.
        /// </summary>
        public override void SendNow()
        {
            ((OutputDevice) Device).SendNoteOn(Channel, Pitch, Velocity);
        }

        /// <summary>
        ///     Returns a copy of this message, shifted in time by the specified amount.
        /// </summary>
        public override Message MakeTimeShiftedCopy(float delta)
        {
            return new NoteOnMessage(Device, Channel, Pitch, Velocity, Time + delta);
        }
    }

    /// <summary>
    ///     Percussion message.
    /// </summary>
    /// <remarks>
    ///     A percussion message is simply shorthand for sending a Note On message to Channel10 with a
    ///     percussion-specific note.  This message can be sent to an OutputDevice but will be received
    ///     by an InputDevice as a NoteOn message.
    /// </remarks>
    public class PercussionMessage : DeviceMessage
    {
        /// <summary>
        ///     Constructs a Percussion message.
        /// </summary>
        /// <param name="device">The device associated with this message.</param>
        /// <param name="percussion">Percussion.</param>
        /// <param name="velocity">Velocity, 0..127.</param>
        /// <param name="time">The timestamp for this message.</param>
        public PercussionMessage(DeviceBase device, Percussion percussion, int velocity,
            float time)
            : base(device, time)
        {
            percussion.Validate();
            if (velocity < 0 || velocity > 127)
            {
                throw new ArgumentOutOfRangeException(nameof(velocity));
            }
            Percussion = percussion;
            Velocity = velocity;
        }

        /// <summary>
        ///     Percussion.
        /// </summary>
        public Percussion Percussion { get; }

        /// <summary>
        ///     Velocity, 0..127.
        /// </summary>
        public int Velocity { get; }

        /// <summary>
        ///     Sends this message immediately.
        /// </summary>
        public override void SendNow()
        {
            ((OutputDevice) Device).SendNoteOn(Channel.Channel10, (Pitch) Percussion, Velocity);
        }

        /// <summary>
        ///     Returns a copy of this message, shifted in time by the specified amount.
        /// </summary>
        public override Message MakeTimeShiftedCopy(float delta)
        {
            return new PercussionMessage(Device, Percussion, Velocity, Time + delta);
        }
    }

    /// <summary>
    ///     Note Off message.
    /// </summary>
    public class NoteOffMessage : NoteMessage
    {
        /// <summary>
        ///     Constructs a Note Off message.
        /// </summary>
        /// <param name="device">The device associated with this message.</param>
        /// <param name="channel">Channel, 0..15, 10 reserved for percussion.</param>
        /// <param name="pitch">The pitch for this note message.</param>
        /// <param name="velocity">Velocity, 0..127.</param>
        /// <param name="time">The timestamp for this message.</param>
        public NoteOffMessage(DeviceBase device, Channel channel, Pitch pitch, int velocity,
            float time)
            : base(device, channel, pitch, velocity, time)
        {
        }

        /// <summary>
        ///     Sends this message immediately.
        /// </summary>
        public override void SendNow()
        {
            ((OutputDevice) Device).SendNoteOff(Channel, Pitch, Velocity);
        }

        /// <summary>
        ///     Returns a copy of this message, shifted in time by the specified amount.
        /// </summary>
        public override Message MakeTimeShiftedCopy(float delta)
        {
            return new NoteOffMessage(Device, Channel, Pitch, Velocity, Time + delta);
        }
    }

    /// <summary>
    ///     A Note On message which schedules its own Note Off message when played.
    /// </summary>
    public class NoteOnOffMessage : NoteMessage
    {
        private readonly Clock _clock;

        /// <summary>
        ///     Constructs a Note On/Off message.
        /// </summary>
        /// <param name="device">The device associated with this message.</param>
        /// <param name="channel">Channel, 0..15, 10 reserved for percussion.</param>
        /// <param name="pitch">The pitch for this note message.</param>
        /// <param name="velocity">Velocity, 0..127.</param>
        /// <param name="time">The timestamp for this message.</param>
        /// <param name="clock">The clock that should schedule the off message.</param>
        /// <param name="duration">Time delay between on message and off messasge.</param>
        public NoteOnOffMessage(DeviceBase device, Channel channel, Pitch pitch,
            int velocity, float time, Clock clock, float duration)
            : base(device, channel, pitch, velocity, time)
        {
            _clock = clock;
            Duration = duration;
        }

        /// <summary>
        ///     The clock used to schedule the follow-up message.
        /// </summary>
        public Clock Clock => _clock;

        /// <summary>
        ///     Time delay between the Note On and the Note Off.
        /// </summary>
        public float Duration { get; }

        /// <summary>
        ///     Sends this message immediately.
        /// </summary>
        public override void SendNow()
        {
            ((OutputDevice) Device).SendNoteOn(Channel, Pitch, Velocity);
            _clock.Schedule(new NoteOffMessage(Device, Channel, Pitch, Velocity, Time + Duration));
        }

        /// <summary>
        ///     Returns a copy of this message, shifted in time by the specified amount.
        /// </summary>
        public override Message MakeTimeShiftedCopy(float delta)
        {
            return new NoteOnOffMessage(Device, Channel, Pitch, Velocity, Time + delta,
                Clock, Duration);
        }
    }

    /// <summary>
    ///     Control change message.
    /// </summary>
    public class ControlChangeMessage : ChannelMessage
    {
        /// <summary>
        ///     Construts a Control Change message.
        /// </summary>
        /// <param name="device">The device associated with this message.</param>
        /// <param name="channel">Channel, 0..15, 10 reserved for percussion.</param>
        /// <param name="control">Control, 0..119</param>
        /// <param name="value">Value, 0..127.</param>
        /// <param name="time">The timestamp for this message.</param>
        public ControlChangeMessage(DeviceBase device, Channel channel, Control control, int value,
            float time)
            : base(device, channel, time)
        {
            control.Validate();
            if (value < 0 || value > 127)
            {
                throw new ArgumentOutOfRangeException(nameof(control));
            }
            Control = control;
            Value = value;
        }

        /// <summary>
        ///     The control for this message.
        /// </summary>
        public Control Control { get; }

        /// <summary>
        ///     Value, 0..127.
        /// </summary>
        public int Value { get; }

        /// <summary>
        ///     Sends this message immediately.
        /// </summary>
        public override void SendNow()
        {
            ((OutputDevice) Device).SendControlChange(Channel, Control, Value);
        }

        /// <summary>
        ///     Returns a copy of this message, shifted in time by the specified amount.
        /// </summary>
        public override Message MakeTimeShiftedCopy(float delta)
        {
            return new ControlChangeMessage(Device, Channel, Control, Value, Time + delta);
        }
    }

    /// <summary>
    ///     Pitch Bend message.
    /// </summary>
    public class PitchBendMessage : ChannelMessage
    {
        /// <summary>
        ///     Constructs a Pitch Bend message.
        /// </summary>
        /// <param name="device">The device associated with this message.</param>
        /// <param name="channel">Channel, 0..15, 10 reserved for percussion.</param>
        /// <param name="value">Pitch bend value, 0..16383, 8192 is centered.</param>
        /// <param name="time">The timestamp for this message.</param>
        public PitchBendMessage(DeviceBase device, Channel channel, int value, float time)
            : base(device, channel, time)
        {
            if (value < 0 || value > 16383)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
            Value = value;
        }

        /// <summary>
        ///     Pitch bend value, 0..16383, 8192 is centered.
        /// </summary>
        public int Value { get; }

        /// <summary>
        ///     Sends this message immediately.
        /// </summary>
        public override void SendNow()
        {
            ((OutputDevice) Device).SendPitchBend(Channel, Value);
        }

        /// <summary>
        ///     Returns a copy of this message, shifted in time by the specified amount.
        /// </summary>
        public override Message MakeTimeShiftedCopy(float delta)
        {
            return new PitchBendMessage(Device, Channel, Value, Time + delta);
        }
    }

    /// <summary>
    ///     Program Change message.
    /// </summary>
    public class ProgramChangeMessage : ChannelMessage
    {
        /// <summary>
        ///     Constructs a Program Change message.
        /// </summary>
        /// <param name="device">The device associated with this message.</param>
        /// <param name="channel">Channel.</param>
        /// <param name="instrument">Instrument.</param>
        /// <param name="time">The timestamp for this message.</param>
        public ProgramChangeMessage(DeviceBase device, Channel channel, Instrument instrument,
            float time)
            : base(device, channel, time)
        {
            instrument.Validate();
            Instrument = instrument;
        }

        /// <summary>
        ///     Instrument.
        /// </summary>
        public Instrument Instrument { get; }

        /// <summary>
        ///     Sends this message immediately.
        /// </summary>
        public override void SendNow()
        {
            ((OutputDevice) Device).SendProgramChange(Channel, Instrument);
        }

        /// <summary>
        ///     Returns a copy of this message, shifted in time by the specified amount.
        /// </summary>
        public override Message MakeTimeShiftedCopy(float delta)
        {
            return new ProgramChangeMessage(Device, Channel, Instrument, Time + delta);
        }
    }

    /// <summary>
    ///     Pseudo-MIDI message used to arrange for a callback at a certain time.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This message can be scheduled with
    ///         <see cref="Clock.Schedule(Message)">
    ///             Clock.Schedule
    ///         </see>
    ///         just like any other message.  When its time comes and it
    ///         gets "sent", it invokes the callback provided in the constructor.
    ///     </para>
    ///     <para>
    ///         The idea is that you can embed callback points into the music you've
    ///         scheduled, so that (if the clock gets to that point in the music) your code has
    ///         an opportunity for some additional processing.
    ///     </para>
    ///     <para>The callback is invoked on the MidiOutputDevice's worker thread.</para>
    /// </remarks>
    public class CallbackMessage : Message
    {
        /// <summary>
        ///     Delegate called when a CallbackMessage is sent.
        /// </summary>
        /// <param name="time">The time at which this event was scheduled.</param>
        /// <returns>
        ///     Additional messages which should be scheduled as a result of this callback,
        ///     or null.
        /// </returns>
        public delegate void CallbackType(float time);

        /// <summary>
        ///     Constructs a Callback message.
        /// </summary>
        /// <param name="callback">The callback to invoke when this message is "sent".</param>
        /// <param name="time">The timestamp for this message.</param>
        public CallbackMessage(CallbackType callback, float time)
            : base(time)
        {
            Callback = callback;
        }

        /// <summary>
        ///     The callback to invoke when this message is "sent".
        /// </summary>
        public CallbackType Callback { get; }

        /// <summary>
        ///     Sends this message immediately, ignoring the beatTime.
        /// </summary>
        public override void SendNow()
        {
            Callback(Time);
        }

        /// <summary>
        ///     Returns a copy of this message, shifted in time by the specified amount.
        /// </summary>
        public override Message MakeTimeShiftedCopy(float delta)
        {
            return new CallbackMessage(Callback, Time + delta);
        }
    }

    #region SysEx

    /// <summary>
    ///     SysEx message
    /// </summary>
    public class SysExMessage : DeviceMessage
    {
        /// <summary>
        ///     Protected constructor.
        /// </summary>
        public SysExMessage(DeviceBase device, byte[] data, float time)
            : base(device, time)
        {
            Data = data;
        }

        /// <summary>
        ///     Data.
        /// </summary>
        public byte[] Data { get; }

        /// <summary>
        ///     Sends this message immediately.
        /// </summary>
        public override void SendNow()
        {
            ((OutputDevice) Device).SendSysEx(Data);
        }

        /// <summary>
        ///     Returns a copy of this message, shifted in time by the specified amount.
        /// </summary>
        public override Message MakeTimeShiftedCopy(float delta)
        {
            return new SysExMessage(Device, Data, Time + delta);
        }
    }

    #endregion
}