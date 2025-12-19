using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DotnetRefScan.DefaultUsedReferencesProviders
{
    /// <summary>
    /// Used references provider for NuGet packages defined in C# project files.
    /// It uses dotnet list package command to list all top level and transitive packages from the project file.
    /// </summary>
    public class CSharpProjectFileUsedReferencesProvider : IUsedReferencesProvider
    {
        /// <inheritdoc/>
        public virtual string Name => nameof(CSharpProjectFileUsedReferencesProvider);

        /// <inheritdoc/>
        public virtual string? FileSearchPattern => "*.csproj";

        /// <inheritdoc/>
        public virtual async Task<ICollection<UsedPackageReference>> LoadReferences(string? fileName)
        {
            if (fileName == null)
            {
                return new List<UsedPackageReference>();
            }

            string workingDirectory = Path.GetDirectoryName(fileName);

            var (ExitCode, StdOut, StdErr) = await RunProcessAsync("dotnet", "list package --include-transitive --format json", workingDirectory).ConfigureAwait(false);
            if (ExitCode != 0 || !string.IsNullOrEmpty(StdErr) || string.IsNullOrEmpty(StdOut))
            {
                throw new InvalidOperationException($"Dotnet command exited with code {ExitCode}. Details: {StdErr}.");
            }

            PackageReferences? references = JsonSerializer.Deserialize<PackageReferences>(StdOut);

            if (references == null)
            {
                return new List<UsedPackageReference>();
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
                .SelectMany(p => p.Frameworks.Where(f => f.TransitivePackages != null).SelectMany(f => f.TransitivePackages))
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

        private static async Task<(int ExitCode, string StdOut, string StdErr)> RunProcessAsync(string fileName, string arguments, string workingDirectory)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory,
            };

            using var process = new Process { StartInfo = psi };

            process.Start();

            Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
            Task<string> stderrTask = process.StandardError.ReadToEndAsync();

            process.WaitForExit();

            return (
                process.ExitCode,
                await stdoutTask.ConfigureAwait(false),
                await stderrTask.ConfigureAwait(false)
            );
        }

        private class PackageReferences
        {
            [JsonPropertyName("version")]
            public int Version { get; set; }

            [JsonPropertyName("parameters")]
            public string? Parameters { get; set; }

            [JsonPropertyName("projects")]
            public Project[]? Projects { get; set; }
        }

        private class Project
        {
            [JsonPropertyName("path")]
            public string? Path { get; set; }

            [JsonPropertyName("frameworks")]
            public Framework[]? Frameworks { get; set; }
        }

        private class Framework
        {
            [JsonPropertyName("framework")]
            public string? FrameworkName { get; set; }

            [JsonPropertyName("topLevelPackages")]
            public Toplevelpackage[]? TopLevelPackages { get; set; }

            [JsonPropertyName("transitivePackages")]
            public Transitivepackage[]? TransitivePackages { get; set; }
        }

        private class Toplevelpackage : Package
        {
            [JsonPropertyName("requestedVersion")]
            public string? RequestedVersion { get; set; }
        }

        private class Transitivepackage : Package
        { }

        private class Package
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("resolvedVersion")]
            public string? ResolvedVersion { get; set; }
        }
    }
}
