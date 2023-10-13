using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotnetRefScan
{
    /// <summary>
    /// Used package reference provider for specific file type.
    /// </summary>
    public interface IUsedReferencesProvider
    {
        /// <summary>
        /// Gets provider name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets search pattern to search for supported files.
        /// If null is provided, the provider will be executed only once per call of <see cref="RefScan.LoadUsedReferences"/>.
        /// </summary>
        public string? FileSearchPattern { get; }

        /// <summary>
        /// Loads used package references from the provided file.
        /// </summary>
        /// <param name="fileName">File to load the package references from.</param>
        /// <returns>Collection of used package references.</returns>
        public Task<ICollection<UsedPackageReference>> LoadReferences(string? fileName);
    }
}
