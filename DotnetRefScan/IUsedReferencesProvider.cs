using System;
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
        string Name { get; }

        /// <summary>
        /// Gets search pattern to search for supported files.
        /// If null is provided, the provider will be executed only once per call of <see cref="RefScan.LoadUsedReferences"/>.
        /// </summary>
        string? FileSearchPattern { get; }

        /// <summary>
        /// Gets package license info provider.
        /// </summary>
        IPackageLicenseInfoProvider? PackageLicenseInfoProvider { get; }

        /// <summary>
        /// Loads used package references from the provided file.
        /// </summary>
        /// <param name="fileName">File to load the package references from.</param>
        /// <param name="shouldLoadLicense">A predicate for license info loading - if <see langword="true"/>, the license info will be loaded for the given package. If <see langword="null"/>, license will be loaded for all packages references.</param>
        /// <returns>Collection of used package references.</returns>
        Task<ICollection<UsedPackageReference>> LoadReferences(string? fileName, Func<UsedPackageReference, bool>? shouldLoadLicense);
    }
}
