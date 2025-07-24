using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Audio
{
    public class GameSound
    {
        string Name { get; set; } = string.Empty;
        public FMOD.Sound Sound { get; set; }
        public AudioType Type { get; set; } = AudioType.Sound;

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
