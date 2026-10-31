using ModManagerGUI.Services;
using ModManagerGUI.Services.Steam;
using ModManagerGUI.ViewModels;

namespace ConfigReaderTest;

public static class Program {
    public static void Main(string[] args) {
        string id = args.Length > 0 ? args[0] : "";
        SteamServices.GetWorkshopInfo();
        foreach (var node in ModContentReader.ReadCached(new ModCardViewModel(id))) {
            ContentParser.PrintTree(node);
        }
    }
}