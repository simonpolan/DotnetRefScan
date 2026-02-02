using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DotnetRefScan.Extensions
{
    /// <summary>
    /// Extensions methods for IPackageLicenseInfoProvider.
    /// </summary>
    public static class IPackageLicenseInfoProviderExtensions
    {
        /// <summary>
        /// Attempts to get licenses for the given user package references using specific <see cref="IPackageLicenseInfoProvider"/> using a parallel execution.
        /// The task directly updates the provided <paramref name="references"/> list.
        /// </summary>
        /// <param name="packageLicenseInfoProvider">Package license info provider.</param>
        /// <param name="references">User package references.</param>
        /// <param name="shouldLoadLicense">A predicate to specify whether the given reference should get the license info loaded.</param>
        /// <param name="maxDegreeOfParallelism">Max degree of parallelism.</param>
        public static async Task TryGetLicenses(this IPackageLicenseInfoProvider? packageLicenseInfoProvider, List<UsedPackageReference> references, Func<UsedPackageReference, bool>? shouldLoadLicense, int maxDegreeOfParallelism = 20)
        {
            if (packageLicenseInfoProvider == null)
                return;

            var semaphore = new SemaphoreSlim(maxDegreeOfParallelism, maxDegreeOfParallelism);

            var tasks = references
                    .Select((reference, index) => (reference, index))
                    .Where(r => shouldLoadLicense == null || shouldLoadLicense(r.reference))
                    .Select(r => TryGetLicense(semaphore, packageLicenseInfoProvider, references, r.index))
                    .ToList();

            if (tasks.Count == 0)
                return;

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private static async Task TryGetLicense(SemaphoreSlim semaphore, IPackageLicenseInfoProvider packageLicenseInfoProvider, List<UsedPackageReference> references, int i)
        {
            await semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                var r = references[i];
                var license = await packageLicenseInfoProvider.TryGetLicense(r.Name, r.Version).ConfigureAwait(false);
                references[i] = new UsedPackageReference(r.Name, r.Version, r.Source, license, r.ProviderName, r.DefinitionFileName);
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
