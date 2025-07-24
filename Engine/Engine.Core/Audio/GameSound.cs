/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         GameSound.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Audio
{
    /// <summary>
    /// Represents a sound in the game, encapsulating its name, FMOD sound object, and type (Sound or Music).
    /// </summary>
    public class GameSound
    {
        string Name { get; set; } = string.Empty;
        public FMOD.Sound Sound { get; set; }
        public AudioType Type { get; set; } = AudioType.Sound;

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
        }

        /// <summary>
        /// Try to play an FMOD sound
        /// </summary>
        public void Play()
        {
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

