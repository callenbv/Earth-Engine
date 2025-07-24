/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         AudioManager.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Data;

namespace Engine.Core.Audio
{
    /// <summary>
    /// Manages audio playback using FMOD
    /// </summary>
    public class AudioManager
    {
        public Dictionary<string, GameSound> Sounds = new Dictionary<string, GameSound>(); // List of all sounds
        public FMOD.System AudioSystem;
        public FMOD.ChannelGroup MainChannel;
        public int MaxChannels { get; private set; } = 32;
        public FMOD.Channel Sound { get; private set; }
        public FMOD.Channel Music { get; private set; }
        public static AudioManager Instance { get; private set; }

        /// <summary>
        /// Singleton instance of AudioManager
        /// </summary>
        public AudioManager()
        {
            Instance = this;
        }

        /// <summary>
        /// Initialze the audio system and load all sounds
        /// </summary>
        public void Initialize()
        {
            FMOD.RESULT result = FMOD.Factory.System_Create(out AudioSystem);
            if (result != FMOD.RESULT.OK)
            {
                throw new Exception($"Fmod result was {result}");
            }
            AudioSystem.init(MaxChannels, FMOD.INITFLAGS.NORMAL, (IntPtr)0);
            result = AudioSystem.createChannelGroup("MainChannel", out MainChannel);
            if (result != FMOD.RESULT.OK)
            {
                throw new Exception($"Fmod result was {result}");
            }

            string contentFolderPath = EnginePaths.AssetsBase;
            if (string.IsNullOrEmpty(contentFolderPath) || !Directory.Exists(contentFolderPath))
            {
                Console.WriteLine($"[AUDIO] Content loading from {contentFolderPath}");
                throw new DirectoryNotFoundException("Audio content folder not found.");
            }

            string[] files = Directory.GetFiles(contentFolderPath, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (var file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);

                // Determine if it's music based on path
                string relativePath = Path.GetRelativePath(contentFolderPath, file).Replace('\\', '/');
                AudioType audioType = relativePath.StartsWith("Audio/Music", StringComparison.OrdinalIgnoreCase)
                                      || relativePath.Contains("/Music/")
                                      ? AudioType.Music
                                      : AudioType.Sound;

                FMOD.MODE mode = (audioType == AudioType.Music) ? FMOD.MODE.CREATESTREAM : FMOD.MODE.DEFAULT;

                result = AudioSystem.createSound(file, mode, out FMOD.Sound sound);
                if (result != FMOD.RESULT.OK)
                {
                    Console.WriteLine($"[ERROR] Failed to load {fileName}: {result}");
                    continue;
                }

                GameSound gameSound = new GameSound(fileName, sound, audioType);
                Sounds[fileName] = gameSound;
            }
        }

        /// <summary>
        /// Update the audio system each frame
        /// </summary>
        /// <param name="dt"></param>
        public void Update(float dt)
        {
            AudioSystem.update();
        }
    }
}

