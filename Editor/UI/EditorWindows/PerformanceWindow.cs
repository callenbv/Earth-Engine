/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         PerformanceWindow.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using ImGuiNET;

namespace EarthEngineEditor.Windows
{
    /// <summary>
    /// PerformanceWindow is a window that displays performance metrics such as frame time, average frame time, and FPS.
    /// </summary>
    public class PerformanceWindow
    {
        private bool _showPerformanceWindow = false;
        private double _lastFrameTime = 0;
        private double _averageFrameTime = 0;
        private int _frameCount = 0;

        /// <summary>
        /// Initializes a new instance of the PerformanceWindow class.
        /// </summary>
        /// <param name="frameTime"></param>
        public void Update(double frameTime)
        {
            _lastFrameTime = frameTime;
            _averageFrameTime = (_averageFrameTime * _frameCount + _lastFrameTime) / (_frameCount + 1);
            _frameCount++;
        }

        /// <summary>
        /// Renders the performance window with the current frame statistics.
        /// </summary>
        public void Render()
        {
            if (!_showPerformanceWindow) return;

            ImGui.Begin("Performance", ref _showPerformanceWindow);
            ImGui.Text($"Last Frame: {_lastFrameTime:F2}ms");
            ImGui.Text($"Average Frame: {_averageFrameTime:F2}ms");
            ImGui.Text($"FPS: {1000.0 / _lastFrameTime:F1}");
            ImGui.Text($"Frame Count: {_frameCount}");
            ImGui.Separator();
            
            if (ImGui.Button("Reset Stats"))
            {
                _frameCount = 0;
                _averageFrameTime = 0;
            }
            
            ImGui.End();
        }

        public bool IsVisible => _showPerformanceWindow;
        public void SetVisible(bool visible) => _showPerformanceWindow = visible;
    }
} 
