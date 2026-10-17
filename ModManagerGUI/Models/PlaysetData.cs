namespace ModManagerGUI.Models;
public sealed class Playset(string name,int id,bool enabled,List<string> modids) {
    public string Name { get; set; } = name;
    public int Id = id;
    public bool Enabled = enabled;
    public List<string> ModIds { get; set; } = modids;
};
