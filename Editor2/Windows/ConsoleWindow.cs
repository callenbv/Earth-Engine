using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ImGuiNET;
using System.Numerics;

namespace EarthEngineEditor
{
    public class ConsoleWindow
    {
        private readonly List<string> _logLines = new();
        private readonly StringBuilder _currentLine = new();
        private readonly TextWriter _originalOut;
        private readonly TextWriter _originalError;
        private readonly ConsoleWriter _consoleWriter;
        private bool _showConsole = true;
        public bool IsVisible => _showConsole;
        public void SetVisible(bool visible) => _showConsole = visible;
        private bool _autoScroll = true;
        private int _maxLines = 200;

        public ConsoleWindow()
        {
            _originalOut = Console.Out;
            _originalError = Console.Error;
            _consoleWriter = new ConsoleWriter(this);
            
            // Redirect console output
            Console.SetOut(_consoleWriter);
            Console.SetError(_consoleWriter);
        }

        public void AddLine(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            
            lock (_logLines)
            {
                // Use string interpolation for better performance
                _logLines.Add($"[{DateTime.Now:HH:mm:ss}] {line}");
                
                // Keep only the last maxLines
                if (_logLines.Count > _maxLines)
                {
                    _logLines.RemoveRange(0, _logLines.Count - _maxLines);
                }
            }
        }

        public void Render()
        {
            if (!_showConsole) return;

            ImGui.Begin("Console", ref _showConsole, ImGuiWindowFlags.MenuBar);
            
            // Menu bar
            if (ImGui.BeginMenuBar())
            {
                if (ImGui.MenuItem("Clear"))
                {
                    lock (_logLines)
                    {
                        _logLines.Clear();
                    }
                }
                ImGui.MenuItem("Auto-scroll", null, ref _autoScroll);
                if (ImGui.MenuItem("Copy to Clipboard"))
                {
                    lock (_logLines)
                    {
                        string allText = string.Join("\n", _logLines);
                        ImGui.SetClipboardText(allText);
                    }
                }
                ImGui.EndMenuBar();
            }

            // Console content
            ImGui.BeginChild("ConsoleContent", Vector2.Zero, ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);
            
            // Copy lines to avoid long lock
            List<string> linesToRender;
            lock (_logLines)
            {
                linesToRender = new List<string>(_logLines);
            }
            
            foreach (string line in linesToRender)
            {
                ImGui.TextWrapped(line);
            }

            // Auto-scroll to bottom
            if (_autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
            {
                ImGui.SetScrollHereY(1.0f);
            }

            ImGui.EndChild();
            ImGui.End();
        }

        public void Dispose()
        {
            // Restore original console output
            Console.SetOut(_originalOut);
            Console.SetError(_originalError);
        }

        private class ConsoleWriter : TextWriter
        {
            private readonly ConsoleWindow _consoleWindow;

            public ConsoleWriter(ConsoleWindow consoleWindow)
            {
                _consoleWindow = consoleWindow;
            }

            public override Encoding Encoding => Encoding.UTF8;

            public override void Write(char value)
            {
                _consoleWindow._currentLine.Append(value);
            }

            public override void Write(string? value)
            {
                if (value != null)
                {
                    _consoleWindow._currentLine.Append(value);
                }
            }

            public override void WriteLine(string? value)
            {
                _consoleWindow._currentLine.Append(value);
                _consoleWindow.AddLine(_consoleWindow._currentLine.ToString());
                _consoleWindow._currentLine.Clear();
            }

            public override void WriteLine()
            {
                _consoleWindow.AddLine(_consoleWindow._currentLine.ToString());
                _consoleWindow._currentLine.Clear();
            }
        }
    }
} 