using System.Collections.Generic;

namespace DotnetRefScan
{
    /// <summary>
    /// License verification result.
    /// </summary>
    public class LicenseVerificationResult
    {
        internal LicenseVerificationResult(ICollection<UsedPackageReference> usedPackageReferences, ICollection<PackageReference> licensePackageReferences, ICollection<UsedPackageReference> missingInLicense, ICollection<PackageReference> redundantInLicense, ICollection<PackageReference> withInvalidLicense, ICollection<UsedPackageReference> withIncompleteLicenseInformation)
        {
            UsedPackageReferences = usedPackageReferences ?? throw new System.ArgumentNullException(nameof(usedPackageReferences));
            LicensePackageReferences = licensePackageReferences ?? throw new System.ArgumentNullException(nameof(licensePackageReferences));
            MissingInLicense = missingInLicense ?? throw new System.ArgumentNullException(nameof(missingInLicense));
            RedundantInLicense = redundantInLicense ?? throw new System.ArgumentNullException(nameof(redundantInLicense));
            WithInvalidLicense = withInvalidLicense ?? throw new System.ArgumentNullException(nameof(withInvalidLicense));
            WithIncompleteLicenseInformation = withIncompleteLicenseInformation ?? throw new System.ArgumentNullException(nameof(withIncompleteLicenseInformation));
        }

        /// <summary>
        /// Gets a value indicating whether the license file is up to date.
        /// If true, the license file contains all expected package references.
        /// </summary>
        public bool IsUpToDate => MissingInLicense.Count == 0 && RedundantInLicense.Count == 0 && WithInvalidLicense.Count == 0;

        /// <summary>
        /// Gets used package references.
        /// </summary>
        public ICollection<UsedPackageReference> UsedPackageReferences { get; }

        /// <summary>
        /// Gets license package references.
        /// </summary>
        public ICollection<PackageReference> LicensePackageReferences { get; }

        /// <summary>
        /// Gets package references missing in the license file.
        /// </summary>
        public ICollection<UsedPackageReference> MissingInLicense { get; }

        /// <summary>
        /// Gets package references which are redundant in the license file and should be therefore removed.
        /// </summary>
        public ICollection<PackageReference> RedundantInLicense { get; }

        /// <summary>
        /// Gets package references with invalid license information.
        /// </summary>
        public ICollection<PackageReference> WithInvalidLicense { get; }

        /// <summary>
        /// Gets used package references with incomplete license information.
        /// For these packages, not all license information values could be loaded, therefore you should verify it manually.
        /// </summary>
        public ICollection<UsedPackageReference> WithIncompleteLicenseInformation { get; }
    }
}
