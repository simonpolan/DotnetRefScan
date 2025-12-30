using System.Threading;
using System.Threading.Tasks;

namespace DotnetRefScan
{
    /// <summary>
    /// License package license info reference provider for specific package provider.
    /// </summary>
    public interface IPackageLicenseInfoProvider
    {
        /// <summary>
        /// Tries to obtain license info for the given package.
        /// </summary>
        /// <param name="packageId">Package ID / name.</param>
        /// <param name="packageVersion">Package version (optional). If not provided, the latest package version will be used.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Package license (if found).</returns>
        Task<PackageLicense?> TryGetLicense(string packageId, string? packageVersion = null, CancellationToken cancellationToken = default);
    }
}
