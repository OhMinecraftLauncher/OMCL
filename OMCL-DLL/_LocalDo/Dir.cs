using System.IO;

namespace OMCL_DLL.Tools
{
    public class Dir
    {
        private static string currentDirectory;
        public static string GetCurrentDirectory()
        {
            if (currentDirectory == null || currentDirectory == "") return Directory.GetCurrentDirectory();
            else return currentDirectory;
        }
        public static void SetCurrentDirectory(string value)
        {
            currentDirectory = value;
        }
    }
}