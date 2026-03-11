using ImGuiNET;
using System.Numerics;

namespace Editor.Windows.ImGuiWrappers
{
    /// <summary>
    /// Dropdown menu list (combobox)
    /// </summary>
    public class EDropdown<T> : EWidget
    {
        public string Label { get; set; } = string.Empty;
        public T? Current { get; set; }
        public Action<T>? OnValueChanged { get; set; }
        public List<T> Items = new List<T>();
        public Vector2 Size = Vector2.One;

        /// <summary>
        /// Set size
        /// </summary>
        public EDropdown()
        {

        }

        /// <summary>
        /// Setup dropdown
        /// </summary>
        /// <param name="label"></param>
        /// <param name="size"></param>
        public EDropdown(string label, Vector2 size)
        {
            Label = label;
            Size = size;
        }

        /// <summary>
        /// Render the button
        /// </summary>
        public override void Draw()
        {
            string currentLabel = Current?.ToString() ?? string.Empty;

            ImGui.PushItemWidth(Size.X);
            if (ImGui.BeginCombo($"##{GetHashCode()}", currentLabel))
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    var item = Items[i];
                    bool isSelected = EqualityComparer<T>.Default.Equals(item, Current);

                    if (ImGui.Selectable(item?.ToString() ?? string.Empty, isSelected))
                    {
                        Current = item;
                        OnValueChanged?.Invoke(item);
                    }

                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }

                ImGui.EndCombo();
            }
        }
    }
}
