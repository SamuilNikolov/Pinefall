using System;

namespace Pinefall
{
    public sealed class VoiceJitterBuffer
    {
        const int Slots = 32;
        readonly float[][] frames = new float[Slots][];
        readonly uint[] sequences = new uint[Slots]; readonly bool[] valid = new bool[Slots];
        readonly object sync = new object();
        uint next; int queued, offset, missingFrames; bool primed, initialized; float last, concealGain = 1;
        bool resampling; float phase, low, high;
        public int PlayedSamples { get; private set; }
        public int ConcealedSamples { get; private set; }
        public int DroppedPackets { get; private set; }
        public int BufferedFrames { get { lock (sync) return queued; } }
        public VoiceJitterBuffer() { for (int i = 0; i < Slots; i++) frames[i] = new float[ProximityVoice.FrameSamples]; }
        public void Reset()
        { lock (sync) { Array.Clear(valid, 0, valid.Length); queued = offset = missingFrames = 0; primed = initialized = resampling = false; phase = low = high = last = 0; concealGain = 1; } }
        public bool Push(uint sequence, byte[] encoded)
        {
            if (encoded == null || encoded.Length != ProximityVoice.FrameSamples) return false;
            lock (sync)
            {
                if (!initialized || !primed && queued == 0) { next = sequence; initialized = true; offset = 0; }
                int delta = unchecked((int)(sequence - next));
                if (delta < 0) { if (!primed && delta >= -4) next = sequence; else { DroppedPackets++; return false; } }
                if (delta >= Slots) { Array.Clear(valid, 0, valid.Length); queued = offset = 0; next = sequence; primed = resampling = false; }
                int slot = (int)(sequence % Slots);
                if (valid[slot] && sequences[slot] == sequence) { DroppedPackets++; return false; }
                if (!valid[slot]) queued++;
                for (int i = 0; i < encoded.Length; i++) frames[slot][i] = ProximityVoice.Decode(encoded[i]);
                sequences[slot] = sequence; valid[slot] = true;
                if (queued > 10)
                {
                    uint keep = sequence - 5;
                    for (int i = 0; i < Slots; i++) if (valid[i] && unchecked((int)(sequences[i] - keep)) < 0) { valid[i] = false; queued--; DroppedPackets++; }
                    next = keep; offset = 0;
                }
                return true;
            }
        }
        bool Prime()
        {
            if (primed) return true;
            if (queued < 4) return false;
            primed = true; missingFrames = 0; concealGain = 1; return true;
        }
        float ReadSample()
        {
            if (!Prime()) return 0;
            int slot = (int)(next % Slots); bool present = valid[slot] && sequences[slot] == next;
            float value;
            if (present) { last = frames[slot][offset]; concealGain = 1; value = last; PlayedSamples++; }
            else { concealGain *= .988f; value = last * concealGain; ConcealedSamples++; }
            if (++offset == ProximityVoice.FrameSamples)
            {
                offset = 0; next++;
                if (present) { valid[slot] = false; queued--; missingFrames = 0; } else missingFrames++;
                if (queued == 0 && missingFrames >= 2) primed = false;
            }
            return value;
        }
        public void Render(float[] output)
        { lock (sync) { for (int i = 0; i < output.Length; i++) output[i] = ReadSample(); } }
        public void RenderDevice(float[] output, int channels, int sampleRate)
        {
            // OnAudioFilterRead consumes one device DSP block, never a one-second streamed clip.
            // Keep the wire rate at 16 kHz and interpolate once into the device's native rate.
            lock (sync)
            {
                if (channels < 1 || sampleRate < 1 || !Prime()) { Array.Clear(output, 0, output.Length); resampling = false; return; }
                if (!resampling) { low = ReadSample(); high = ReadSample(); phase = 0; resampling = true; }
                float step = (float)ProximityVoice.SampleRate / sampleRate;
                for (int frame = 0; frame < output.Length / channels; frame++)
                {
                    float value = low + (high - low) * phase;
                    for (int channel = 0; channel < channels; channel++) output[frame * channels + channel] = value;
                    phase += step;
                    while (phase >= 1) { phase -= 1; low = high; high = ReadSample(); }
                }
            }
        }

    }
    public sealed class VoicePacketBudget
    {
        float credit = 20; double previous;
        public bool Accept(double now)
        { credit = Math.Min(20, credit + (float)Math.Max(0, now - previous) * 70); previous = now; if (credit < 1) return false; credit--; return true; }
    }
}
