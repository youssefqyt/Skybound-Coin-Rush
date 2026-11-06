using System;
using System.Collections.Generic;
using UnityEngine;

namespace Skybound
{
    public enum MusicTrack
    {
        None,
        Menu,
        Run,
        Intense,
        Victory,
        GameOver
    }

    /// <summary>
    /// Original procedural audio: synthesized music loops and SFX, so the project
    /// has a complete soundtrack without shipping large audio files.
    /// </summary>
    public static class ProceduralAudio
    {
        private const int MusicRate = 22050;
        private const int SfxRate = 44100;
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        private enum Wave
        {
            Sine,
            Triangle,
            Saw,
            Square,
            Noise
        }

        public static AudioClip GetMusic(MusicTrack track)
        {
            string key = "music_" + track;
            AudioClip clip;
            if (Cache.TryGetValue(key, out clip)) return clip;
            switch (track)
            {
                case MusicTrack.Menu:
                    clip = BuildMusic(key, 92, 4,
                        new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 48, 52, 55 }, new[] { 55, 59, 62 },
                        33, 29, 24, 31, 0.82f, false, false);
                    break;
                case MusicTrack.Run:
                    clip = BuildMusic(key, 124, 4,
                        new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 48, 52, 55 }, new[] { 55, 59, 62 },
                        33, 29, 24, 31, 1f, true, false);
                    break;
                case MusicTrack.Intense:
                    clip = BuildMusic(key, 142, 4,
                        new[] { 50, 53, 57 }, new[] { 46, 50, 53 }, new[] { 53, 57, 60 }, new[] { 55, 59, 62 },
                        26, 22, 29, 31, 1f, true, true);
                    break;
                case MusicTrack.Victory:
                    clip = BuildJingle(key, true);
                    break;
                case MusicTrack.GameOver:
                    clip = BuildJingle(key, false);
                    break;
                default:
                    return null;
            }
            clip.name = key;
            Cache[key] = clip;
            return clip;
        }

        public static AudioClip GetSfx(string id)
        {
            string key = "sfx_" + id;
            AudioClip clip;
            if (Cache.TryGetValue(key, out clip)) return clip;
            switch (id)
            {
                case "coin": clip = BuildCoin(); break;
                case "gold": clip = BuildGoldCoin(); break;
                case "jump": clip = BuildJump(); break;
                case "land": clip = BuildLand(); break;
                case "slide": clip = BuildSlide(); break;
                case "hit": clip = BuildHit(); break;
                case "powerup": clip = BuildPowerUp(); break;
                case "shield": clip = BuildShield(); break;
                case "button": clip = BuildButton(); break;
                case "achievement": clip = BuildAchievement(); break;
                case "level": clip = BuildLevelComplete(); break;
                case "warning": clip = BuildWarning(); break;
                case "purchase": clip = BuildPurchase(); break;
                case "revive": clip = BuildRevive(); break;
                case "thunder": clip = BuildThunder(); break;
                case "laser": clip = BuildLaser(); break;
                case "swoosh": clip = BuildSwoosh(); break;
                case "star": clip = BuildStar(); break;
                default: clip = BuildButton(); break;
            }
            clip.name = key;
            Cache[key] = clip;
            return clip;
        }

        // ------------------------------------------------------------- MUSIC

        private static AudioClip BuildMusic(string name, int bpm, int bars,
            int[] chordA, int[] chordB, int[] chordC, int[] chordD,
            int bassA, int bassB, int bassC, int bassD,
            float gain, bool drums, bool intense)
        {
            float beat = 60f / bpm;
            float barLength = beat * 4f;
            int total = Mathf.CeilToInt(barLength * bars * MusicRate) + MusicRate / 20;
            float[] data = new float[total];
            int[][] chords = { chordA, chordB, chordC, chordD };
            int[] bassNotes = { bassA, bassB, bassC, bassD };
            System.Random rng = new System.Random(777 + bpm);
            for (int b = 0; b < bars; b++)
            {
                int[] chord = chords[b % 4];
                int bass = bassNotes[b % 4];
                float barStart = b * barLength;
                // pad
                for (int n = 0; n < chord.Length; n++)
                {
                    AddNote(data, MusicRate, barStart, barLength * 1.02f, MidiToFreq(chord[n]), 0.055f,
                        Wave.Sine, 0.45f, 0.6f, 0.003f);
                    AddNote(data, MusicRate, barStart, barLength * 1.02f, MidiToFreq(chord[n] + 12), 0.018f,
                        Wave.Triangle, 0.6f, 0.7f, -0.002f);
                }
                // bass line: 8th notes
                for (int step = 0; step < 8; step++)
                {
                    float t = barStart + step * beat * 0.5f;
                    int note = bass + (step % 4 == 3 ? 7 : 0);
                    AddNote(data, MusicRate, t, beat * 0.42f, MidiToFreq(note), 0.115f,
                        Wave.Saw, 0.008f, 0.12f);
                }
                // arpeggio: 16th notes
                int arpSteps = intense ? 16 : 12;
                for (int step = 0; step < arpSteps; step++)
                {
                    float t = barStart + step * (barLength / arpSteps);
                    int note = chord[step % chord.Length] + 12 + ((step / chord.Length) % 2) * 12;
                    float amp = intense ? 0.042f : 0.036f;
                    AddNote(data, MusicRate, t, beat * 0.24f, MidiToFreq(note), amp,
                        intense ? Wave.Square : Wave.Triangle, 0.005f, 0.18f);
                }
                if (drums)
                {
                    for (int beatIndex = 0; beatIndex < 4; beatIndex++)
                    {
                        float t = barStart + beatIndex * beat;
                        if (beatIndex == 0 || beatIndex == 2)
                            AddKick(data, MusicRate, t, 0.24f);
                        if (beatIndex == 1 || beatIndex == 3)
                            AddSnare(data, MusicRate, t, 0.085f, rng);
                        AddHat(data, MusicRate, t, 0.026f, rng);
                        AddHat(data, MusicRate, t + beat * 0.5f, 0.016f, rng);
                        if (intense)
                            AddHat(data, MusicRate, t + beat * 0.25f, 0.012f, rng);
                    }
                }
            }
            // gentle loop seam
            int fade = MusicRate / 60;
            for (int i = 0; i < fade; i++)
            {
                float f = i / (float)fade;
                data[i] *= f;
                data[data.Length - 1 - i] *= f;
            }
            Normalize(data, gain);
            AudioClip clip = AudioClip.Create(name, data.Length, 1, MusicRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildJingle(string name, bool victory)
        {
            int rate = MusicRate;
            float duration = victory ? 4.2f : 3.4f;
            int total = Mathf.CeilToInt(duration * rate);
            float[] data = new float[total];
            int[] notes = victory
                ? new[] { 60, 64, 67, 72, 76, 79 }
                : new[] { 69, 65, 62, 57, 45 };
            float spacing = victory ? 0.28f : 0.42f;
            for (int i = 0; i < notes.Length; i++)
            {
                AddNote(data, rate, i * spacing, duration - i * spacing, MidiToFreq(notes[i]),
                    victory ? 0.12f : 0.09f, Wave.Sine, 0.01f, 0.5f);
                AddNote(data, rate, i * spacing, duration - i * spacing, MidiToFreq(notes[i] + 12),
                    0.035f, Wave.Triangle, 0.01f, 0.6f);
                if (victory)
                    AddNote(data, rate, i * spacing, 0.4f, MidiToFreq(notes[i] + 24), 0.028f, Wave.Sine, 0.005f, 0.2f);
            }
            if (victory)
            {
                for (int i = 0; i < 10; i++)
                {
                    float t = i * 0.18f;
                    AddNote(data, rate, t, 0.16f, 1200 + i * 140, 0.02f, Wave.Sine, 0.001f, 0.1f);
                }
            }
            Normalize(data, 0.9f);
            AudioClip clip = AudioClip.Create(name, total, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // --------------------------------------------------------------- SFX

        private static AudioClip BuildCoin()
        {
            float[] data = new float[SfxRate / 3];
            AddNote(data, SfxRate, 0f, 0.09f, 1318f, 0.16f, Wave.Sine, 0.001f, 0.06f);
            AddNote(data, SfxRate, 0.055f, 0.16f, 1976f, 0.12f, Wave.Sine, 0.001f, 0.12f);
            AddNote(data, SfxRate, 0.055f, 0.16f, 2637f, 0.04f, Wave.Triangle, 0.001f, 0.1f);
            Normalize(data, 0.9f);
            return Clip("coin_sfx", data, SfxRate);
        }

        private static AudioClip BuildGoldCoin()
        {
            float[] data = new float[SfxRate * 5 / 10];
            float[] notes = { 1046f, 1318f, 1568f, 2093f };
            for (int i = 0; i < notes.Length; i++)
            {
                AddNote(data, SfxRate, i * 0.045f, 0.2f, notes[i], 0.13f, Wave.Sine, 0.001f, 0.12f);
            }
            Normalize(data, 0.9f);
            return Clip("gold_sfx", data, SfxRate);
        }

        private static AudioClip BuildJump()
        {
            float[] data = new float[SfxRate / 4];
            AddSweep(data, SfxRate, 0f, 0.18f, 300f, 780f, 0.12f, Wave.Triangle);
            AddNoise(data, SfxRate, 0f, 0.14f, 0.05f, 0.4f);
            Normalize(data, 0.85f);
            return Clip("jump_sfx", data, SfxRate);
        }

        private static AudioClip BuildLand()
        {
            float[] data = new float[SfxRate / 6];
            AddNote(data, SfxRate, 0f, 0.12f, 98f, 0.22f, Wave.Sine, 0.001f, 0.09f);
            AddNoise(data, SfxRate, 0f, 0.08f, 0.08f, 0.12f);
            Normalize(data, 0.85f);
            return Clip("land_sfx", data, SfxRate);
        }

        private static AudioClip BuildSlide()
        {
            float[] data = new float[SfxRate / 3];
            AddNoise(data, SfxRate, 0f, 0.28f, 0.11f, 0.35f);
            AddSweep(data, SfxRate, 0f, 0.25f, 620f, 180f, 0.03f, Wave.Sine);
            Normalize(data, 0.8f);
            return Clip("slide_sfx", data, SfxRate);
        }

        private static AudioClip BuildHit()
        {
            float[] data = new float[SfxRate / 2];
            AddNote(data, SfxRate, 0f, 0.26f, 84f, 0.32f, Wave.Sine, 0.001f, 0.2f);
            AddSweep(data, SfxRate, 0f, 0.2f, 240f, 55f, 0.16f, Wave.Saw);
            AddNoise(data, SfxRate, 0f, 0.22f, 0.16f, 0.28f);
            Normalize(data, 1f);
            return Clip("hit_sfx", data, SfxRate);
        }

        private static AudioClip BuildPowerUp()
        {
            float[] data = new float[SfxRate * 6 / 10];
            float[] notes = { 523f, 659f, 784f, 1046f, 1318f };
            for (int i = 0; i < notes.Length; i++)
            {
                AddNote(data, SfxRate, i * 0.055f, 0.25f, notes[i], 0.10f, Wave.Triangle, 0.002f, 0.15f);
                AddNote(data, SfxRate, i * 0.055f, 0.2f, notes[i] * 1.5f, 0.03f, Wave.Sine, 0.002f, 0.12f);
            }
            Normalize(data, 0.9f);
            return Clip("power_sfx", data, SfxRate);
        }

        private static AudioClip BuildShield()
        {
            float[] data = new float[SfxRate / 2];
            for (int i = 0; i < 4; i++)
            {
                AddNote(data, SfxRate, i * 0.035f, 0.3f, 880f + i * 220f, 0.07f, Wave.Sine, 0.005f, 0.22f);
            }
            AddNoise(data, SfxRate, 0f, 0.2f, 0.03f, 0.3f);
            Normalize(data, 0.85f);
            return Clip("shield_sfx", data, SfxRate);
        }

        private static AudioClip BuildButton()
        {
            float[] data = new float[SfxRate / 10];
            AddNote(data, SfxRate, 0f, 0.045f, 1400f, 0.15f, Wave.Sine, 0.001f, 0.03f);
            Normalize(data, 0.7f);
            return Clip("button_sfx", data, SfxRate);
        }

        private static AudioClip BuildAchievement()
        {
            float[] data = new float[SfxRate];
            float[] notes = { 784f, 988f, 1318f, 1568f, 2093f };
            for (int i = 0; i < notes.Length; i++)
            {
                AddNote(data, SfxRate, i * 0.08f, 0.5f, notes[i], 0.10f, Wave.Sine, 0.002f, 0.35f);
                AddNote(data, SfxRate, i * 0.08f, 0.35f, notes[i] * 2f, 0.025f, Wave.Triangle, 0.002f, 0.25f);
            }
            Normalize(data, 0.9f);
            return Clip("achievement_sfx", data, SfxRate);
        }

        private static AudioClip BuildLevelComplete()
        {
            float[] data = new float[SfxRate * 12 / 10];
            float[] notes = { 523f, 659f, 784f, 1046f, 1318f };
            for (int i = 0; i < notes.Length; i++)
            {
                AddNote(data, SfxRate, i * 0.13f, 0.75f, notes[i], 0.11f, Wave.Triangle, 0.004f, 0.5f);
            }
            for (int i = 0; i < 8; i++)
                AddNote(data, SfxRate, 0.65f + i * 0.07f, 0.15f, 1600f + i * 180f, 0.02f, Wave.Sine, 0.001f, 0.1f);
            Normalize(data, 0.95f);
            return Clip("level_sfx", data, SfxRate);
        }

        private static AudioClip BuildWarning()
        {
            float[] data = new float[SfxRate * 6 / 10];
            AddNote(data, SfxRate, 0f, 0.22f, 440f, 0.12f, Wave.Square, 0.005f, 0.18f);
            AddNote(data, SfxRate, 0.28f, 0.3f, 330f, 0.12f, Wave.Square, 0.005f, 0.25f);
            Normalize(data, 0.8f);
            return Clip("warning_sfx", data, SfxRate);
        }

        private static AudioClip BuildPurchase()
        {
            float[] data = new float[SfxRate / 2];
            AddNote(data, SfxRate, 0f, 0.12f, 1568f, 0.12f, Wave.Sine, 0.001f, 0.1f);
            AddNote(data, SfxRate, 0.08f, 0.2f, 2093f, 0.09f, Wave.Sine, 0.001f, 0.15f);
            AddNote(data, SfxRate, 0.16f, 0.25f, 1046f, 0.06f, Wave.Triangle, 0.001f, 0.2f);
            Normalize(data, 0.9f);
            return Clip("purchase_sfx", data, SfxRate);
        }

        private static AudioClip BuildRevive()
        {
            float[] data = new float[SfxRate];
            AddSweep(data, SfxRate, 0f, 0.65f, 180f, 1200f, 0.12f, Wave.Sine);
            AddSweep(data, SfxRate, 0.05f, 0.6f, 360f, 1800f, 0.045f, Wave.Triangle);
            AddNoise(data, SfxRate, 0f, 0.5f, 0.03f, 0.4f);
            Normalize(data, 0.9f);
            return Clip("revive_sfx", data, SfxRate);
        }

        private static AudioClip BuildThunder()
        {
            float[] data = new float[SfxRate * 2];
            AddNoise(data, SfxRate, 0f, 1.8f, 0.22f, 0.06f);
            AddNote(data, SfxRate, 0f, 1.2f, 55f, 0.18f, Wave.Sine, 0.02f, 1.0f);
            Normalize(data, 0.9f);
            return Clip("thunder_sfx", data, SfxRate);
        }

        private static AudioClip BuildLaser()
        {
            float[] data = new float[SfxRate / 3];
            AddSweep(data, SfxRate, 0f, 0.22f, 1600f, 300f, 0.10f, Wave.Saw);
            AddNoise(data, SfxRate, 0f, 0.15f, 0.03f, 0.3f);
            Normalize(data, 0.8f);
            return Clip("laser_sfx", data, SfxRate);
        }

        private static AudioClip BuildSwoosh()
        {
            float[] data = new float[SfxRate / 3];
            AddNoise(data, SfxRate, 0f, 0.26f, 0.07f, 0.2f);
            Normalize(data, 0.8f);
            return Clip("swoosh_sfx", data, SfxRate);
        }

        private static AudioClip BuildStar()
        {
            float[] data = new float[SfxRate / 3];
            AddNote(data, SfxRate, 0f, 0.28f, 1760f, 0.09f, Wave.Sine, 0.001f, 0.2f);
            AddNote(data, SfxRate, 0.04f, 0.26f, 2637f, 0.04f, Wave.Sine, 0.001f, 0.2f);
            Normalize(data, 0.85f);
            return Clip("star_sfx", data, SfxRate);
        }

        // ----------------------------------------------------------- HELPERS

        private static AudioClip Clip(string name, float[] data, int rate)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float MidiToFreq(int midi)
        {
            return 440f * Mathf.Pow(2f, (midi - 69) / 12f);
        }

        private static void AddNote(float[] data, int rate, float startSec, float duration, float freq,
            float amp, Wave wave, float attack, float release, float detune = 0f)
        {
            int start = Mathf.Max(0, Mathf.RoundToInt(startSec * rate));
            int len = Mathf.Min(data.Length - start, Mathf.RoundToInt(duration * rate));
            if (len <= 0) return;
            float phase = 0f;
            float phase2 = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)len;
                float env = Envelope(t, attack, release);
                float f = freq * (1f + detune);
                phase += f / rate;
                phase2 += (f * 1.006f) / rate;
                float v;
                switch (wave)
                {
                    case Wave.Triangle:
                        v = 1f - 4f * Mathf.Abs(Mathf.Repeat(phase, 1f) - 0.5f);
                        break;
                    case Wave.Saw:
                        v = 2f * Mathf.Repeat(phase, 1f) - 1f;
                        break;
                    case Wave.Square:
                        v = Mathf.Repeat(phase, 1f) < 0.5f ? 1f : -1f;
                        break;
                    case Wave.Noise:
                        v = HashNoise(i + start) * 2f - 1f;
                        break;
                    default:
                        v = Mathf.Sin(phase * Mathf.PI * 2f);
                        break;
                }
                if (detune != 0f)
                    v = (v + Mathf.Sin(phase2 * Mathf.PI * 2f)) * 0.5f;
                data[start + i] += v * env * amp;
            }
        }

        private static void AddSweep(float[] data, int rate, float startSec, float duration,
            float f0, float f1, float amp, Wave wave)
        {
            int start = Mathf.Max(0, Mathf.RoundToInt(startSec * rate));
            int len = Mathf.Min(data.Length - start, Mathf.RoundToInt(duration * rate));
            if (len <= 0) return;
            float phase = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)len;
                float env = Envelope(t, 0.01f, 0.7f);
                float freq = Mathf.Lerp(f0, f1, t);
                phase += freq / rate;
                float v;
                if (wave == Wave.Saw) v = 2f * Mathf.Repeat(phase, 1f) - 1f;
                else if (wave == Wave.Triangle) v = 1f - 4f * Mathf.Abs(Mathf.Repeat(phase, 1f) - 0.5f);
                else if (wave == Wave.Square) v = Mathf.Repeat(phase, 1f) < 0.5f ? 1f : -1f;
                else v = Mathf.Sin(phase * Mathf.PI * 2f);
                data[start + i] += v * env * amp;
            }
        }

        private static void AddNoise(float[] data, int rate, float startSec, float duration, float amp, float smooth)
        {
            int start = Mathf.Max(0, Mathf.RoundToInt(startSec * rate));
            int len = Mathf.Min(data.Length - start, Mathf.RoundToInt(duration * rate));
            if (len <= 0) return;
            float filtered = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)len;
                float env = Envelope(t, 0.005f, 0.8f);
                float n = HashNoise(i + start) * 2f - 1f;
                filtered = Mathf.Lerp(filtered, n, Mathf.Clamp01(smooth));
                data[start + i] += filtered * env * amp;
            }
        }

        private static void AddKick(float[] data, int rate, float startSec, float amp)
        {
            int start = Mathf.Max(0, Mathf.RoundToInt(startSec * rate));
            int len = Mathf.Min(data.Length - start, Mathf.RoundToInt(0.18f * rate));
            float phase = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)len;
                float env = Mathf.Exp(-t * 8f);
                float freq = Mathf.Lerp(145f, 44f, t * t);
                phase += freq / rate;
                data[start + i] += Mathf.Sin(phase * Mathf.PI * 2f) * env * amp;
            }
        }

        private static void AddSnare(float[] data, int rate, float startSec, float amp, System.Random rng)
        {
            int start = Mathf.Max(0, Mathf.RoundToInt(startSec * rate));
            int len = Mathf.Min(data.Length - start, Mathf.RoundToInt(0.16f * rate));
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)len;
                float env = Mathf.Exp(-t * 14f);
                float noise = (float)rng.NextDouble() * 2f - 1f;
                data[start + i] += noise * env * amp;
            }
        }

        private static void AddHat(float[] data, int rate, float startSec, float amp, System.Random rng)
        {
            int start = Mathf.Max(0, Mathf.RoundToInt(startSec * rate));
            int len = Mathf.Min(data.Length - start, Mathf.RoundToInt(0.05f * rate));
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)len;
                float env = Mathf.Exp(-t * 24f);
                float noise = (float)rng.NextDouble() * 2f - 1f;
                data[start + i] += noise * env * amp;
            }
        }

        private static float Envelope(float t, float attack, float release)
        {
            float a = attack <= 0f ? 1f : Mathf.Clamp01(t / attack);
            float r = release <= 0f ? 1f : Mathf.Clamp01((1f - t) / release);
            return Mathf.Min(a, r);
        }

        private static float HashNoise(int i)
        {
            unchecked
            {
                uint x = (uint)i;
                x ^= x << 13;
                x ^= x >> 17;
                x ^= x << 5;
                return (x & 0xFFFFFF) / 16777215f;
            }
        }

        private static void Normalize(float[] data, float gain)
        {
            float max = 0.0001f;
            for (int i = 0; i < data.Length; i++)
            {
                float a = Mathf.Abs(data[i]);
                if (a > max) max = a;
            }
            float scale = Mathf.Min(1f / max, 4f) * gain;
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Mathf.Clamp(data[i] * scale, -0.98f, 0.98f);
            }
        }
    }
}
