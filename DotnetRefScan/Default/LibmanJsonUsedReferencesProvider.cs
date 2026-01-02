using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DotnetRefScan.Default
{
    /// <summary>
    /// Used references provider for packages defined in libman json files.
    /// </summary>
    public class LibmanJsonUsedReferencesProvider : IUsedReferencesProvider
    {
        /// <inheritdoc/>
        public virtual string Name => nameof(LibmanJsonUsedReferencesProvider);

        /// <inheritdoc/>
        public virtual string? FileSearchPattern => "libman.json";

        /// <inheritdoc/>
        public IPackageLicenseInfoProvider? PackageLicenseInfoProvider => null;

        /// <inheritdoc/>
        public virtual async Task<ICollection<UsedPackageReference>> LoadReferences(string? fileName, Func<UsedPackageReference, bool>? shouldLoadLicense)
        {
            if (fileName == null)
            {
                return new List<UsedPackageReference>();
            }

            using StreamReader sr = new StreamReader(fileName, Encoding.UTF8);
            string json = await sr.ReadToEndAsync().ConfigureAwait(false);
            sr.Close();

            LibmanReferences? libmanReferences = JsonSerializer.Deserialize<LibmanReferences>(json);

            return libmanReferences?.Libraries
                    .Where(l => l.Name != null)
                    .Select(l => new UsedPackageReference(
                        l.Name![..l.Name!.LastIndexOf('@')],
                        l.Name[(l.Name.LastIndexOf('@') + 1)..],
                        l.Provider ?? libmanReferences.DefaultProvider ?? "libman",
                        null,
                        Name,
                        fileName))
                    .DistinctBy(p => $"{p.Name}@{p.Version}")
                    .OrderBy(l => l.Name)
                    .ToList()
                    .DistinctAndSorted()
                    ?? new List<UsedPackageReference>();
        }

        private class LibmanReferences
        {
            [JsonPropertyName("version")]
            public string? Version { get; set; }

            [JsonPropertyName("defaultProvider")]
            public string? DefaultProvider { get; set; }

            [JsonPropertyName("libraries")]
            public List<Library>? Libraries { get; set; }
        }

        private class Library
        {
            [JsonPropertyName("provider")]
            public string? Provider { get; set; }

            [JsonPropertyName("library")]
            public string? Name { get; set; }

            [JsonPropertyName("destination")]
            public string? Destination { get; set; }
        }
    }
}
