using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotnetRefScan
{
    /// <summary>
    /// Used references provider for packages defined in libman json files.
    /// </summary>
    public sealed class LibmanJsonUsedReferencesProvider : IUsedReferencesProvider
    {
        /// <inheritdoc/>
        public string Name => nameof(LibmanJsonUsedReferencesProvider);

        /// <inheritdoc/>
        public string? FileSearchPattern => "libman.json";

        /// <inheritdoc/>
        public async Task<ICollection<UsedPackageReference>> LoadReferences(string? fileName)
        {
            if (fileName == null)
            {
                return await Task.FromResult(new List<UsedPackageReference>()).ConfigureAwait(false);
            }

            using StreamReader sr = new StreamReader(fileName, Encoding.UTF8);
            string json = await sr.ReadToEndAsync().ConfigureAwait(false);
            sr.Close();

            LibmanReferences? libmanReferences = JsonConvert.DeserializeObject<LibmanReferences>(json);

            return libmanReferences?.Libraries
                    .Where(l => l.Name != null)
                    .Select(l => new UsedPackageReference(
                        l.Name![..l.Name!.LastIndexOf('@')],
                        l.Name[(l.Name.LastIndexOf('@') + 1)..],
                        l.Provider ?? libmanReferences.DefaultProvider ?? "libman",
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
            [JsonProperty("version")]
            public string? Version { get; set; }

            [JsonProperty("defaultProvider")]
            public string? DefaultProvider { get; set; }

            [JsonProperty("libraries")]
            public List<Library>? Libraries { get; set; }
        }

        private class Library
        {
            [JsonProperty("provider")]
            public string? Provider { get; set; }

            [JsonProperty("library")]
            public string? Name { get; set; }

            [JsonProperty("destination")]
            public string? Destination { get; set; }
        }
    }
}
