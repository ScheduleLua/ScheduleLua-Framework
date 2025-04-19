using System.Collections.Generic;
using Newtonsoft.Json;

namespace ScheduleLua.API.Mods
{
    /// <summary>
    /// Represents the contents of a manifest.json file for a Lua mod
    /// </summary>
    public class ModManifest
    {
        /// <summary>
        /// The display name of the mod
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// The version of the mod in semver format
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>
        /// A description of what the mod does
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// The name of the mod author
        /// </summary>
        [JsonProperty("author")]
        public string Author { get; set; }

        /// <summary>
        /// The main entry point script for the mod (defaults to "init.lua")
        /// </summary>
        [JsonProperty("main")]
        public string Main { get; set; } = "init.lua";

        /// <summary>
        /// List of additional script files to register events on
        /// </summary>
        [JsonProperty("files")]
        public List<string> Files { get; set; } = new List<string>();

        /// <summary>
        /// List of other mod folders that this mod depends on
        /// </summary>
        [JsonProperty("dependencies")]
        public List<string> Dependencies { get; set; } = new List<string>();

        /// <summary>
        /// Legacy load order priority (no longer used, kept for backwards compatibility)
        /// </summary>
        [JsonProperty("load_order", Required = Required.Default)]
        public int LoadOrder { get; set; } = 100;

        /// <summary>
        /// The API version that this mod is compatible with
        /// </summary>
        [JsonProperty("api_version")]
        public string ApiVersion { get; set; }
    }
}