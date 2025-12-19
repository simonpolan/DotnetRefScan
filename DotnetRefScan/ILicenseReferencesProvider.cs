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
        string Name { get; }

        /// <summary>
        /// Adds or updates package references within the provided license file.
        /// </summary>
        /// <param name="licenseFileName">License file name.</param>
        /// <param name="oldReference">Old package reference.</param>
        /// <param name="newReference">New package reference.</param>
        Task AddOrUpdateReference(string licenseFileName, PackageReference? oldReference, PackageReference newReference);

        /// <summary>
        /// Formats the provided license file.
        /// </summary>
        /// <param name="licenseFileName">License file name.</param>
        Task FormatLicense(string licenseFileName);

        /// <summary>
        /// Loads package references from the provided license file.
        /// </summary>
        /// <param name="licenseFileName">License file name.</param>
        /// <returns>Collection of package references.</returns>
        Task<List<PackageReference>> LoadReferences(string licenseFileName);

        /// <summary>
        /// Removes package references from the provided license file.
        /// </summary>
        /// <param name="licenseFileName">License file name.</param>
        /// <param name="reference">Package reference to remove.</param>
        Task RemoveReference(string licenseFileName, PackageReference reference);

        /// <summary>
        /// Sorts package references in the license table (if found).
        /// </summary>
        /// <param name="licenseFileName">License file name.</param>
        Task SortReferences(string licenseFileName);
    }
}
