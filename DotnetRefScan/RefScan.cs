using DotnetRefScan.DefaultLicenseReferencesProviders;
using DotnetRefScan.DefaultUsedReferencesProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetRefScan
{
    /// <summary>
    /// Package reference scanner for .NET projects.
    /// </summary>
    public class RefScan
    {
        private readonly string _location;
        private readonly SearchOption _searchOption;
        private readonly Func<string, bool>? _filter;

        /// <summary>
        /// Initializes a new instance of the <see cref="RefScan"/> class.
        /// </summary>
        /// <param name="location">Scanned folder name.</param>
        /// <param name="searchOption">Search options.</param>
        /// <param name="filter">Package references file filter. Return <see langword="true"/> to include the file.</param>
        public RefScan(string location, SearchOption searchOption = SearchOption.AllDirectories, Func<string, bool>? filter = null)
        {
            _location = location;
            _searchOption = searchOption;
            _filter = filter;
        }

        /// <summary>
        /// Gets or sets a value indicating whether only the latest reference version will be considered if multiple used.
        /// The latest reference version will be taken based on <see cref="PackageReference.Source"/> and <see cref="PackageReference.Name"/>.
        /// </summary>
        public bool ConsiderOnlyLatestVersionsIfMultipleReferenced { get; set; } = true;

        /// <summary>
        /// Used references providers.
        /// You can add custom or remove existing providers here.
        /// </summary>
        public ICollection<IUsedReferencesProvider> UsedReferencesProviders { get; } = new List<IUsedReferencesProvider>()
        {
            new CSharpProjectFileUsedReferencesProvider(),
            new LibmanJsonUsedReferencesProvider(),
        };

        /// <summary>
        /// License references provider.
        /// You can change the provider here.
        /// </summary>
        public ILicenseReferencesProvider LicenseReferencesProvider { get; set; } = new MarkdownLicenseReferencesProvider();

        /// <summary>
        /// Gets or sets a filter which decides whether the given package reference must be mentioned in the License file.
        /// Default: <see langword="true"/>
        /// </summary>
        public Func<UsedPackageReference, bool> IsReferenceRequiredInLicense { get; set; } = (r) => true;

        /// <summary>
        /// Gets or sets a filter which decides whether the given package reference can be left redundant in the License file.
        /// Default: <see langword="false"/>
        /// </summary>
        public Func<PackageReference, bool> IsReferenceAcceptedToBeRedundantInLicense { get; set; } = (r) => false;

        /// <summary>
        /// Loads all used package references using the registered <see cref="UsedReferencesProviders"/>.
        /// </summary>
        /// <returns>Collection of the package references found.</returns>
        public async Task<ICollection<UsedPackageReference>> LoadUsedReferences()
        {
            List<UsedPackageReference> references = new List<UsedPackageReference>();

            foreach (IUsedReferencesProvider provider in UsedReferencesProviders)
            {
                if (provider.FileSearchPattern != null)
                {
                    IEnumerable<string> files = Directory
                        .GetFiles(_location, provider.FileSearchPattern, _searchOption)
                        .Where(IsFileAllowed);

                    foreach (string file in files)
                    {
                        references.AddRange(await provider.LoadReferences(file).ConfigureAwait(false));
                    }
                }
                else
                {
                    references.AddRange(await provider.LoadReferences(null).ConfigureAwait(false));
                }
            }

            return ConsiderOnlyLatestVersionsIfMultipleReferenced
                ? references.LatestAndSorted()
                : references.DistinctAndSorted();
        }

        /// <summary>
        /// Loads all package references mentioned in the license file using the registered <see cref="LicenseReferencesProvider"/>.
        /// </summary>
        /// <param name="licenseFileName">License file name.</param>
        /// <returns>Collection of the package references found.</returns>
        public async Task<ICollection<PackageReference>> LoadLicenseReferences(string licenseFileName)
        {
            ICollection<PackageReference> references = await LicenseReferencesProvider.LoadReferences(licenseFileName).ConfigureAwait(false);
            return references.DistinctAndSorted();
        }

        /// <summary>
        /// Verifies the package references in the license file against the used package references.
        /// </summary>
        /// <param name="licenseFileName">License file name.</param>
        /// <returns>License verification result.</returns>
        public async Task<LicenseVerificationResult> VerifyLicense(string licenseFileName)
        {
            ICollection<UsedPackageReference> usedPackageReferences = await LoadUsedReferences().ConfigureAwait(false);
            ICollection<PackageReference> licensePackageReferences = await LoadLicenseReferences(licenseFileName).ConfigureAwait(false);

            ICollection<UsedPackageReference> missingInLicense = usedPackageReferences.Where(IsReferenceRequiredInLicense).Except(licensePackageReferences).Cast<UsedPackageReference>().ToList();
            ICollection<PackageReference> redundantInLicense = licensePackageReferences.Where(IsReferenceAcceptedToBeRedundantInLicense.Not()).Except(usedPackageReferences).ToList();

            return new LicenseVerificationResult(usedPackageReferences, licensePackageReferences, missingInLicense, redundantInLicense);
        }

        /// <summary>
        /// Updates the package references in the license file.
        /// </summary>
        /// <param name="licenseFileName">License file name.</param>
        /// <returns>License verification result.</returns>
        public async Task UpdateLicense(string licenseFileName)
        {
            ICollection<UsedPackageReference> usedPackageReferences = await LoadUsedReferences().ConfigureAwait(false);
            ICollection<PackageReference> licensePackageReferences = await LoadLicenseReferences(licenseFileName).ConfigureAwait(false);

            ICollection<(PackageReference OldReference, UsedPackageReference NewReference)> updatedReferences = usedPackageReferences.Where(r => IsReferenceRequiredInLicense(r) && licensePackageReferences.Any(lr => lr.Name == r.Name && lr.Source == r.Source) && !licensePackageReferences.Any(lr => lr == r)).Cast<UsedPackageReference>().Select(r => (licensePackageReferences.First(lr => lr.Name == r.Name && lr.Source == r.Source), r)).ToList();

            ICollection<UsedPackageReference> missingInLicense = usedPackageReferences.Where(r => IsReferenceRequiredInLicense(r) && !licensePackageReferences.Any(lr => lr.Name == r.Name && lr.Source == r.Source)).Cast<UsedPackageReference>().ToList();

            ICollection<PackageReference> redundantInLicense = licensePackageReferences.Where(lr => !IsReferenceAcceptedToBeRedundantInLicense(lr) && !usedPackageReferences.Any(r => r.Name == lr.Name && r.Source == lr.Source)).Cast<PackageReference>().ToList();

            // Update references
            foreach (var (oldReference, newReference) in updatedReferences)
            {
                await LicenseReferencesProvider.AddOrUpdateReference(licenseFileName, oldReference, newReference);
            }

            // Add new references
            foreach (var reference in missingInLicense)
            {
                await LicenseReferencesProvider.AddOrUpdateReference(licenseFileName, null, reference);
            }

            // Remove references
            foreach (var reference in redundantInLicense)
            {
                await LicenseReferencesProvider.RemoveReference(licenseFileName, reference);
            }

            // Format
            await LicenseReferencesProvider.FormatLicense(licenseFileName);
            await LicenseReferencesProvider.SortReferences(licenseFileName);
        }

        private bool IsFileAllowed(string path)
        {
            return _filter?.Invoke(path) ?? true;
        }
    }
}
