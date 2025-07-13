using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Game
{
    public class ObjectDefinition
    {
        public string? Name { get; set; }
        public string? Sprite { get; set; }
        public JArray? Components { get; set; }
        public JArray? Scripts { get; set; }
        public Dictionary<string, Dictionary<string, object>>? scriptProperties { get; set; }
    }

    public static class GameObjectRegistry
    {
        static readonly Dictionary<string, ObjectDefinition> _definitions
            = new Dictionary<string, ObjectDefinition>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Call once at startup (or in the editor) to discover & load all .eo files.
        /// </summary>
        public static void LoadAll(string folderPath = "Content/Objects")
        {
            try
            {
                _definitions.Clear();

                foreach (var path in Directory.GetFiles(folderPath, "*.eo", SearchOption.AllDirectories))
                {
                    var key = Path.GetFileNameWithoutExtension(path);
                    var json = File.ReadAllText(path);
                    var def = JsonConvert.DeserializeObject<ObjectDefinition>(json);
                    if (def != null)
                        _definitions[key] = def;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Get the definition by its key (e.g. "Player" or "Enemies/Goblin").
        /// </summary>
        public static ObjectDefinition Get(string defName)
        {
            if (!_definitions.TryGetValue(defName, out var def))
                throw new KeyNotFoundException($"No object definition registered for '{defName}'");
            return def;
        }

        /// <summary>
        /// All loaded keys (for editor UIs, autocomplete, etc).
        /// </summary>
        public static IEnumerable<string> AllKeys => _definitions.Keys;
    }
}
