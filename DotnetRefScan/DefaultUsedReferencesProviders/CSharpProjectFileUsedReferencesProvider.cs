using CliWrap;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotnetRefScan
{
    /// <summary>
    /// Used references provider for NuGet packages defined in C# project files.
    /// It uses dotnet list package command to list all top level and transitive packages from the project file.
    /// </summary>
    public sealed class CSharpProjectFileUsedReferencesProvider : IUsedReferencesProvider
    {
        /// <inheritdoc/>
        public string Name => nameof(CSharpProjectFileUsedReferencesProvider);

        /// <inheritdoc/>
        public string? FileSearchPattern => "*.csproj";

        /// <inheritdoc/>
        public async Task<ICollection<UsedPackageReference>> LoadReferences(string? fileName)
        {
            if (fileName == null)
            {
                return await Task.FromResult(new List<UsedPackageReference>()).ConfigureAwait(false);
            }

            string workingDirectory = Path.GetDirectoryName(fileName);

            StringBuilder stdOutBuffer = new StringBuilder();
            CommandResult result = await Cli
                .Wrap("dotnet")
                .WithArguments("list package --include-transitive --format json")
                .WithWorkingDirectory(workingDirectory)
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync()
                .ConfigureAwait(false);

            string json = stdOutBuffer.ToString();

            PackageReferences? references = JsonConvert.DeserializeObject<PackageReferences>(json);

            if (references == null)
            {
                return await Task.FromResult(new List<UsedPackageReference>()).ConfigureAwait(false);
            }

            List<Package> topLevelPackages = references
                .Projects
                .Where(p => p.Frameworks != null)
                .SelectMany(p => p.Frameworks.Where(f => f.TopLevelPackages != null).SelectMany(f => f.TopLevelPackages))
                .Cast<Package>()
                .ToList();

            List<Package> transitivePackages = references
                .Projects
                .Where(p => p.Frameworks != null)
                .SelectMany(p => p.Frameworks.Where(f => f.TopLevelPackages != null).SelectMany(f => f.TransitivePackages))
                .Cast<Package>()
                .ToList();

            return topLevelPackages
                .Union(transitivePackages)
                .Where(p => p.Id != null && p.ResolvedVersion != null)
                .OrderBy(p => p.Id)
                .Select(p => new UsedPackageReference(
                    p.Id!,
                    p.ResolvedVersion!,
                    "NuGet",
                    Name,
                    fileName))                
                .ToList()
                .DistinctAndSorted();
        }


        private class PackageReferences
        {
            [JsonProperty("version")]
            public int Version { get; set; }

            [JsonProperty("parameters")]
            public string? Parameters { get; set; }

            [JsonProperty("projects")]
            public Project[]? Projects { get; set; }
        }

        private class Project
        {
            [JsonProperty("path")]
            public string? Path { get; set; }

            [JsonProperty("frameworks")]
            public Framework[]? Frameworks { get; set; }
        }

        private class Framework
        {
            [JsonProperty("framework")]
            public string? FrameworkName { get; set; }

            [JsonProperty("topLevelPackages")]
            public Toplevelpackage[]? TopLevelPackages { get; set; }

            [JsonProperty("transitivePackages")]
            public Transitivepackage[]? TransitivePackages { get; set; }
        }

        private class Toplevelpackage : Package
        {
            [JsonProperty("requestedVersion")]
            public string? RequestedVersion { get; set; }
        }

        private class Transitivepackage : Package
        { }

        private class Package
        {
            [JsonProperty("id")]
            public string? Id { get; set; }

            [JsonProperty("resolvedVersion")]
            public string? ResolvedVersion { get; set; }
        }
    }
}
