/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         AudioHandler.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Data;
using Engine.Core.Audio;
using System.IO;
using ImGuiNET;
using FMOD;
using EarthEngineEditor;

namespace Editor.AssetManagement
{
    /// <summary>
    /// Handles loading, saving, and rendering prefabs in the editor.
    /// </summary>
    public class AudioHandler : IAssetHandler
    {
        private GameSound? sound = null;
        private Channel? _previewChannel = null;
        private bool loop = false;
        private float _playbackPosition = 0f;
        private float _duration = 0f;

        /// <summary>
        /// Loads a texture from the specified path and binds it for rendering in the editor.
        /// </summary>
        /// <param name="path"></param>
        public void Open(string path)
        {
            string filePath = Path.GetFileName(path);
            filePath = Path.GetFileNameWithoutExtension(filePath);
            AudioManager.Instance.Sounds.TryGetValue(Path.GetFileName(filePath), out sound);
        }

        public void Load(string path)
        {
        }

        public void Save(string path)
        {
        }

        /// <summary>
        /// Renders the prefab's components in the editor UI.
        /// </summary>
        public void Render()
        {
            if (sound == null)
                return;

            // Play the sound
            bool isPlaying = _previewChannel != null && _previewChannel.Value.IsPlaying();

            string icon = isPlaying ? "\uf04d" : "\uf04b"; // Stop or Play
            string label = isPlaying ? "Stop" : "Play Sound";

            if (ImGuiRenderer.IconButton(label, icon, Microsoft.Xna.Framework.Color.White))
            {
                if (isPlaying)
                {
                    Audio.StopAll(); // Stop everything (or just _previewChannel?.Stop())
                    _previewChannel = null;
                }
                else
                {
                    Audio.StopAll(); // Stop previous previews
                    _previewChannel = sound.Play();
                }
            }

            ImGui.SameLine();


            // Timeline slider (seek bar)
            if (_previewChannel != null)
            {
                _playbackPosition = _previewChannel.Value.GetPositionSeconds();
                _duration = sound.Sound.GetLengthSeconds();
                sound.Loop = loop;
            }

            float temp = _playbackPosition;

            if (ImGui.SliderFloat("##Timeline", ref temp, 0, _duration, $"{temp:F2} / {_duration:F2} sec"))
            {
                if (_previewChannel != null && _previewChannel.Value.IsPlaying())
                {
                    _previewChannel.Value.SetPositionSeconds(temp);
                    _playbackPosition = temp;
                }
            }
            ImGui.Checkbox("Loop", ref loop);
            ImGui.SameLine();
        }
    }
}

