using FMOD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Audio
{
    /// <summary>
    /// Define type of audio (Sound, Music)
    /// </summary>
    public enum AudioType
    {
        Sound,
        Music
    }

    /// <summary>
    /// Optionally use SoundIDs for type safe audio play
    /// </summary>
    public enum SoundID
    {
    }

    public static class Audio
    {
        /// <summary>
        /// Play a sound given a sound ID
        /// </summary>
        /// <param name="sound"></param>
        public static void Play(SoundID sound)
        {
            Play(sound.ToString());
        }

        /// <summary>
        /// Play a sound given a string
        /// </summary>
        /// <param name="sound"></param>
        public static void Play(string sound)
        {
            GameSound? soundObject = AudioManager.Instance.Sounds.TryGetValue(sound, out var gameSound) ? gameSound : null;

            if (soundObject != null)
            {
                soundObject.Play();
            }
        }

        /// <summary>
        /// Stops all audio playing
        /// </summary>
        public static void PauseAll(bool val)
        {
            AudioManager.Instance.MainChannel.setPaused(val);
        }
    }
}
