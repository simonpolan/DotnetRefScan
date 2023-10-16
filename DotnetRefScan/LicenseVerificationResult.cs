using System.Collections.Generic;

namespace DotnetRefScan
{
    /// <summary>
    /// License verification result.
    /// </summary>
    public class LicenseVerificationResult
    {
        internal LicenseVerificationResult(ICollection<UsedPackageReference> usedPackageReferences, ICollection<PackageReference> licensePackageReferences, ICollection<UsedPackageReference> missingInLicense, ICollection<PackageReference> redundantInLicense)
        {
            UsedPackageReferences = usedPackageReferences ?? throw new System.ArgumentNullException(nameof(usedPackageReferences));
            LicensePackageReferences = licensePackageReferences ?? throw new System.ArgumentNullException(nameof(licensePackageReferences));
            MissingInLicense = missingInLicense ?? throw new System.ArgumentNullException(nameof(missingInLicense));
            RedundantInLicense = redundantInLicense ?? throw new System.ArgumentNullException(nameof(redundantInLicense));
        }

        /// <summary>
        /// Gets a value indicating whether the license file is up to date.
        /// If true, the license file contains all expected package references.
        /// </summary>
        public bool IsUpToDate => MissingInLicense.Count == 0 && RedundantInLicense.Count == 0;

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
    }
}
