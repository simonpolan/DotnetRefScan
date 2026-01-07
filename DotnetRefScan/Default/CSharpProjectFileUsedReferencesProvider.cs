using DotnetRefScan.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DotnetRefScan.Default
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
        public virtual IPackageLicenseInfoProvider? PackageLicenseInfoProvider { get; } = new NuGetPackageLicenseInfoProvider();

        /// <inheritdoc/>
        public virtual int LicenseInfoLoadingMaxDegreeOfParallelism { get; set; } = 20;

        /// <summary>
        /// Gets or sets Dotnet command name (e.g. "dotnet" or a full path to the dotnet.exe).
        /// </summary>
        public string DotnetCommand { get; set; } = "dotnet";

        /// <summary>
        /// Gets or sets a value indicating whether the DOTNET RESTORE action should be performed before the DOTNET PACKAGE LIST command.
        /// </summary>
        public bool RestoreBeforeList { get; set; } = true;

        /// <inheritdoc/>
        public virtual async Task<ICollection<UsedPackageReference>> LoadReferences(string? fileName, Func<UsedPackageReference, bool>? shouldLoadLicense)
        {
            if (fileName == null)
            {
                return new List<UsedPackageReference>();
            }

            PackageReferences? references = null;
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    references = await TryListProjectReferences(fileName).ConfigureAwait(false);

                    if (references != null)
                        break;
                }
                catch (InvalidOperationException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }

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

            var packages = topLevelPackages
                .Union(transitivePackages)
                .Where(p => p.Id != null && p.ResolvedVersion != null)
                .OrderBy(p => p.Id)
                .Select(p => new UsedPackageReference(
                    p.Id!,
                    p.ResolvedVersion!,
                    "NuGet",
                    null,
                    Name,
                    fileName))
                .ToList()
                .DistinctAndSorted()
                .ToList();

            await PackageLicenseInfoProvider.TryGetLicenses(packages, shouldLoadLicense, LicenseInfoLoadingMaxDegreeOfParallelism).ConfigureAwait(false);

            return packages;
        }

        /// <summary>
        /// Attempts to list a single project file references.
        /// </summary>
        /// <param name="fileName">Project file name.</param>
        /// <returns>Package references if succeeded. Otherwise <c>null</c>.</returns>
        /// <exception cref="InvalidOperationException">If DOTNET command returns failure success code.</exception>
        protected virtual async Task<PackageReferences?> TryListProjectReferences(string fileName)
        {
            if (RestoreBeforeList)
                await ProcessExecutor.RunProcess(DotnetCommand, $"restore", Path.GetDirectoryName(fileName)).ConfigureAwait(false);

            var (ExitCode, StdOut, StdErr) = await ProcessExecutor.RunProcessWithOutput(DotnetCommand, $"package list --include-transitive --format json --no-restore", Path.GetDirectoryName(fileName)).ConfigureAwait(false);

            if (ExitCode != 0 || !string.IsNullOrEmpty(StdErr) || string.IsNullOrEmpty(StdOut))
            {
                throw new InvalidOperationException($"Dotnet command exited with code {ExitCode}. Output: {StdOut}. Error: {StdErr}.");
            }

            return System.Text.Json.JsonSerializer.Deserialize<PackageReferences>(StdOut);
        }

        /// <summary>
        /// Represents the root object containing package reference information
        /// for one or more projects.
        /// </summary>
        protected class PackageReferences
        {
            /// <summary>
            /// Gets or sets the schema or format version of the package references file.
            /// </summary>
            [JsonPropertyName("version")]
            public int Version { get; set; }

            /// <summary>
            /// Gets or sets optional parameters associated with the package references.
            /// </summary>
            /// <value>
            /// A string containing serialized parameters, or <c>null</c> if no parameters are defined.
            /// </value>
            [JsonPropertyName("parameters")]
            public string? Parameters { get; set; }

            /// <summary>
            /// Gets or sets the collection of projects included in the package references.
            /// </summary>
            /// <value>
            /// An array of <see cref="Project"/> instances, or <c>null</c> if no projects are defined.
            /// </value>
            [JsonPropertyName("projects")]
            public Project[]? Projects { get; set; }
        }

        /// <summary>
        /// Represents a single project and its associated target frameworks.
        /// </summary>
        protected class Project
        {
            /// <summary>
            /// Gets or sets the file system path to the project.
            /// </summary>
            /// <value>
            /// A relative or absolute path to the project file, or <c>null</c> if unspecified.
            /// </value>
            [JsonPropertyName("path")]
            public string? Path { get; set; }

            /// <summary>
            /// Gets or sets the target frameworks defined for the project.
            /// </summary>
            /// <value>
            /// An array of <see cref="Framework"/> instances, or <c>null</c> if no frameworks are defined.
            /// </value>
            [JsonPropertyName("frameworks")]
            public Framework[]? Frameworks { get; set; }
        }

        /// <summary>
        /// Represents a target framework and its package dependencies.
        /// </summary>
        protected class Framework
        {
            /// <summary>
            /// Gets or sets the name of the target framework.
            /// </summary>
            /// <value>
            /// A target framework moniker (TFM), such as <c>net8.0</c> or <c>netstandard2.0</c>.
            /// </value>
            [JsonPropertyName("framework")]
            public string? FrameworkName { get; set; }

            /// <summary>
            /// Gets or sets the collection of top-level packages
            /// directly referenced by the project.
            /// </summary>
            /// <value>
            /// An array of <see cref="Toplevelpackage"/> instances, or <c>null</c> if none are defined.
            /// </value>
            [JsonPropertyName("topLevelPackages")]
            public Toplevelpackage[]? TopLevelPackages { get; set; }

            /// <summary>
            /// Gets or sets the collection of transitive packages
            /// resolved as dependencies of top-level packages.
            /// </summary>
            /// <value>
            /// An array of <see cref="Transitivepackage"/> instances, or <c>null</c> if none are defined.
            /// </value>
            [JsonPropertyName("transitivePackages")]
            public Transitivepackage[]? TransitivePackages { get; set; }
        }

        /// <summary>
        /// Represents a top-level NuGet package reference explicitly
        /// requested by a project.
        /// </summary>
        protected class Toplevelpackage : Package
        {
            /// <summary>
            /// Gets or sets the version of the package explicitly requested
            /// by the project.
            /// </summary>
            /// <value>
            /// A version string as specified in the project file, or <c>null</c> if not defined.
            /// </value>
            [JsonPropertyName("requestedVersion")]
            public string? RequestedVersion { get; set; }
        }

        /// <summary>
        /// Represents a transitive NuGet package dependency
        /// resolved indirectly through other packages.
        /// </summary>
        protected class Transitivepackage : Package
        {
        }

        /// <summary>
        /// Represents a NuGet package with its resolved identity and version.
        /// </summary>
        protected class Package
        {
            /// <summary>
            /// Gets or sets the package identifier.
            /// </summary>
            /// <value>
            /// The NuGet package ID, or <c>null</c> if not available.
            /// </value>
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            /// <summary>
            /// Gets or sets the resolved version of the package.
            /// </summary>
            /// <value>
            /// The exact version selected during package restore,
            /// or <c>null</c> if not available.
            /// </value>
            [JsonPropertyName("resolvedVersion")]
            public string? ResolvedVersion { get; set; }
        }
    }
}
