/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         ConsoleWindow.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>      Console window with color-coded output
/// -----------------------------------------------------------------------------

using System.IO;
using System.Text;
using ImGuiNET;
using System.Numerics;

namespace EarthEngineEditor
{
    /// <summary>
    /// Represents a console window that captures and displays console output.
    /// </summary>
    public class ConsoleWindow
    {
        /// <summary>
        /// Represents a single line in the console log with text and color.
        /// </summary>
        private struct LogLine
        {
            public string Text;
            public Vector4 Color;

            public LogLine(string text, Vector4 color)
            {
                Text = text;
                Color = color;
            }
        }

        private readonly List<LogLine> _logLines = new();
        private readonly StringBuilder _currentLine = new();
        private readonly TextWriter _originalOut;
        private readonly TextWriter _originalError;
        private readonly ConsoleWriter _consoleWriter;
        private bool _showConsole = true;
        public bool IsVisible => _showConsole;
        public void SetVisible(bool visible) => _showConsole = visible;
        private bool _autoScroll = true;
        private int _maxLines = 200;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleWindow"/> class.
        /// </summary>
        public ConsoleWindow()
        {
            _originalOut = Console.Out;
            _originalError = Console.Error;
            _consoleWriter = new ConsoleWriter(this, Vector4.One);

            // Redirect console output
            Console.SetOut(_consoleWriter);
            Console.SetError(new ConsoleWriter(this, new Vector4(1, 0.4f, 0.4f, 1))); // light red for errors
        }

        /// <summary>
        /// Adds a line to the console log.
        /// </summary>
        public void AddLine(string line, Vector4? color = null)
        {
            if (string.IsNullOrEmpty(line)) return;

            var colorValue = color ?? new Vector4(1, 1, 1, 1); // default white

            lock (_logLines)
            {
                _logLines.Add(new LogLine($"[{DateTime.Now:HH:mm:ss}] {line}", colorValue));

                if (_logLines.Count > _maxLines)
                {
                    _logLines.RemoveRange(0, _logLines.Count - _maxLines);
                }
            }
        }

        /// <summary>
        /// Render the console.
        /// </summary>
        public void Render()
        {
            if (!_showConsole) return;

            ImGui.Begin("Console", ref _showConsole, ImGuiWindowFlags.MenuBar);

            // Menu bar
            if (ImGui.BeginMenuBar())
            {
                if (ImGui.MenuItem("Clear"))
                {
                    lock (_logLines) _logLines.Clear();
                }
                if (ImGui.MenuItem("Copy"))
                {
                    lock (_logLines)
                    {
                        string allText = string.Join("\n", _logLines.Select(l => l.Text));
                        ImGui.SetClipboardText(allText);
                    }
                }
                ImGui.EndMenuBar();
            }

            // Console content
            ImGui.BeginChild("ConsoleContent", Vector2.Zero, ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);

            // Copy lines to avoid holding lock during render
            List<LogLine> linesToRender;
            lock (_logLines)
            {
                linesToRender = new List<LogLine>(_logLines);
            }

            foreach (var line in linesToRender)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, line.Color);
                ImGui.TextWrapped(line.Text);
                ImGui.PopStyleColor();
            }

            if (_autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
            {
                ImGui.SetScrollHereY(1.0f);
            }

            ImGui.EndChild();
            ImGui.End();
        }

        /// <summary>
        /// Disposes the console window and restores original console output.
        /// </summary>
        public void Dispose()
        {
            Console.SetOut(_originalOut);
            Console.SetError(_originalError);
        }

        /// <summary>
        /// Custom TextWriter that writes to the console window.
        /// </summary>
        private class ConsoleWriter : TextWriter
        {
            private readonly ConsoleWindow _consoleWindow;
            public override Encoding Encoding => Encoding.UTF8;
            private readonly Vector4 _color;

            public ConsoleWriter(ConsoleWindow consoleWindow, Vector4 color)
            {
                _consoleWindow = consoleWindow;
                _color = color;
            }

            public override void Write(char value)
            {
                _consoleWindow._currentLine.Append(value);
            }

            public override void Write(string? value)
            {
                if (value != null)
                    _consoleWindow._currentLine.Append(value);
            }

            public override void WriteLine(string? value)
            {
                _consoleWindow._currentLine.Append(value);
                _consoleWindow.AddLine(_consoleWindow._currentLine.ToString(), _color);
                _consoleWindow._currentLine.Clear();
            }

            public override void WriteLine()
            {
                _consoleWindow.AddLine(_consoleWindow._currentLine.ToString(), _color);
                _consoleWindow._currentLine.Clear();
            }
        }
    }
}
