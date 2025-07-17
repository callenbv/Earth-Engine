using ImGuiNET;

namespace EarthEngineEditor.Windows
{
    public class PerformanceWindow
    {
        private bool _showPerformanceWindow = false;
        private double _lastFrameTime = 0;
        private double _averageFrameTime = 0;
        private int _frameCount = 0;

        public void Update(double frameTime)
        {
            _lastFrameTime = frameTime;
            _averageFrameTime = (_averageFrameTime * _frameCount + _lastFrameTime) / (_frameCount + 1);
            _frameCount++;
        }

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