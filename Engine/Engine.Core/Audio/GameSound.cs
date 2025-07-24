/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         GameSound.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

namespace Engine.Core.Audio
{
    /// <summary>
    /// Represents a sound in the game, encapsulating its name, FMOD sound object, and type (Sound or Music).
    /// </summary>
    public class GameSound
    {
        /// <summary>
        /// Name of the sound. This is used to identify the sound in the editor and in the game.
        /// </summary>
        string Name { get; set; } = string.Empty;

        /// <summary>
        /// Name of the sound. This is used to identify the sound in the editor and in the game.
        /// </summary>
        public FMOD.Sound Sound { get; set; }

        /// <summary>
        /// Type of audio this sound represents, either Sound or Music.
        /// </summary>
        public AudioType Type { get; set; } = AudioType.Sound;

        /// <summary>
        /// Indicates whether the sound should loop when played. Default is false.
        /// </summary>
        public bool Loop { get; set; } = false;

        /// <summary>
        /// Create a new GameSound instance
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sound"></param>
        /// <param name="type"></param>
        public GameSound(string name, FMOD.Sound sound, AudioType type)
        {
            Name = name;
            Sound = sound;
            Type = type;
            Loop = (AudioType.Music == type ? true : false);
        }

        /// <summary>
        /// Try to play an FMOD sound
        /// </summary>
        public void Play()
        {
            // Enable or disable loop mode
            Sound.setMode(Loop ? FMOD.MODE.LOOP_NORMAL : FMOD.MODE.LOOP_OFF);

            FMOD.RESULT result = AudioManager.Instance.AudioSystem.playSound(Sound, AudioManager.Instance.MainChannel, false, out FMOD.Channel channel);
            if (result != FMOD.RESULT.OK)
            {
                Console.WriteLine($"[ERROR] Failed to play sound {Name}: {result}");
                return;
            }

            // Set the channel group to the main channel
            channel.setChannelGroup(AudioManager.Instance.MainChannel);
        }
    }
}

