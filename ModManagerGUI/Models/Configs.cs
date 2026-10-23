using System.ComponentModel;

namespace ModManagerGUI.Models;

public class Configs {
    public class CommonConfig {
        public string SelectedList { get; set; } = "";
        public bool EnableHtmlRendering { get; set; }
        public List<Playset> Playsets { get; set; } = new();
    }

    public record GameConfig {
        public List<Playset> Playsets = new();
    }
}