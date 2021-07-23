using System.IO;

namespace thrombin.Helpers
{
    public class Logger
    {
        public DirectoryInfo Directory { get; private set; }

        public Logger(string directory)
        {
            Directory = new DirectoryInfo(Path.Combine("_Results", directory));

            if (!Directory.Exists)
                Directory.Create();
        }

        public void WriteLine(string catogery, string message, bool toConsoleAlso = false)
        {
            using (var file = new StreamWriter(Path.Combine(Directory.FullName, $"{catogery}.txt"), append: true))
            {
                file.WriteLine(message);
            }
            if (toConsoleAlso)
                System.Console.WriteLine(message);
        }


        public void Write(string catogery, string message, bool toConsoleAlso = false)
        {
            using (var file = new StreamWriter(Path.Combine(Directory.FullName, $"{catogery}.txt"), append: true))
            {
                file.Write(message);
            }
            if (toConsoleAlso)
                System.Console.Write(message);
        }
    }
}