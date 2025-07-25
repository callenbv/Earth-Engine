/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Audio.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

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
    /// Audio provides methods to play sounds and manage audio in the game engine.
    /// </summary>
    public static class Audio
    {
        /// <summary>
        /// Play a sound given a string
        /// </summary>
        /// <param name="sound"></param>
        public static void Play(string sound)
        {
            Play(sound);
        }

        /// <summary>
        /// Extended play audio method
        /// </summary>
        /// <param name="sound"></param>
        /// <param name="loop"></param>
        public static void Play(string sound, bool loop = false)
        {
            GameSound? soundObject = AudioManager.Instance.Sounds.TryGetValue(sound, out var gameSound) ? gameSound : null;
            if (soundObject != null)
            {
                soundObject.Loop = loop;
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

