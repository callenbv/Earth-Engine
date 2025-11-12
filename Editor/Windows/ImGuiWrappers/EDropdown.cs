using ImGuiNET;

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

        /// <summary>
        /// Render the button
        /// </summary>
        public override void Draw()
        {
            string currentLabel = Current?.ToString() ?? string.Empty;

            if (ImGui.BeginCombo($"{Label}##{GetHashCode()}", currentLabel))
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
