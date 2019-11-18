using System.IO;

namespace SwSh_Sound_Merger
{
    public static class Utils
    {
        public static FileInfo GetFile(this DirectoryInfo obj, string fileName) => new FileInfo($"{obj.FullName}{Path.DirectorySeparatorChar}{fileName}");

        public static DirectoryInfo GetDirectory(this DirectoryInfo obj, string foldername) => new DirectoryInfo($"{obj.FullName}{Path.DirectorySeparatorChar}{foldername.Replace('/', Path.DirectorySeparatorChar)}");
    }
}
