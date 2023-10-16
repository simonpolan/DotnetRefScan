using System.Reflection;

namespace DotnetRefScan.Tests
{
    internal static class StorageHelper
    {
        public static string? GetSolutionRoot()
        {
            string? assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly()?.Location);

            if(assemblyDirectory == null)
            {
                return null;
            }

            string root = assemblyDirectory;
            while (Directory.GetFiles(root, "*.sln", SearchOption.TopDirectoryOnly).Any() == false)
            {
                root = Path.GetDirectoryName(root)!;
            }

            return root;
        }
    }
}
