using System.Text.Json.Serialization;

namespace AutoDragonOath.Models
{
    /// <summary>
    /// Represents a scene/map in the game
    /// </summary>
    public class Scene
    {
        [JsonPropertyName("no")]
        public string? No { get; set; }

        [JsonPropertyName("scenenumber")]
        public int? SceneNumber { get; set; }

        [JsonPropertyName("threadindex")]
        public int? ThreadIndex { get; set; }

        [JsonPropertyName("clientres")]
        public int ClientRes { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("active")]
        public int? Active { get; set; }

        [JsonPropertyName("file")]
        public string? File { get; set; }

        [JsonPropertyName("serverid")]
        public int? ServerId { get; set; }

        [JsonPropertyName("type")]
        public int? Type { get; set; }

        [JsonPropertyName("PvpRuler")]
        public int? PvpRuler { get; set; }

        [JsonPropertyName("BeginPlus")]
        public int? BeginPlus { get; set; }

        [JsonPropertyName("_clientres")]
        public int? ClientResAlt { get; set; }

        [JsonPropertyName("EndPlus")]
        public int? EndPlus { get; set; }

        [JsonPropertyName("IsReLive")]
        public int? IsReLive { get; set; }
    }
}
