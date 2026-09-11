using System.IO;
using System.Reflection;

namespace ThunderFury.DevTool.Data
{
    // Stores DevTool-authored content as JSON next to this mod's own DLL,
    // not under BepInEx's .cfg-oriented Config folder -- these are
    // structured content definitions (items/recipes), not flat settings.
    public static class DevToolPaths
    {
        public static string DataDirectory =>
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DevToolData");

        public static string ItemsFile => Path.Combine(DataDirectory, "items.json");
        public static string RecipesFile => Path.Combine(DataDirectory, "recipes.json");

        public static void EnsureDataDirectory()
        {
            Directory.CreateDirectory(DataDirectory);
        }
    }
}
