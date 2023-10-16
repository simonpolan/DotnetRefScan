using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotnetRefScan
{
    /// <summary>
    /// License package reference provider for specific file type.
    /// </summary>
    public interface ILicenseReferencesProvider
    {
        /// <summary>
        /// Gets provider name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Loads package references from the provided license file.
        /// </summary>
        /// <param name="licenseFileName">License file to load the package references from.</param>
        /// <returns>Collection of package references.</returns>
        public Task<ICollection<PackageReference>> LoadReferences(string? licenseFileName);
    }
}
