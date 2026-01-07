using System;
using System.Collections.Generic;
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

            var tasks = new List<Task>();
            Parallel.For(0, references.Count, new ParallelOptions() { MaxDegreeOfParallelism = maxDegreeOfParallelism }, i =>
            {
                var r = references[i];

                if (shouldLoadLicense != null && !shouldLoadLicense(r))
                    return;

                tasks.Add(TryGetLicense(packageLicenseInfoProvider, references, i));
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private static async Task TryGetLicense(IPackageLicenseInfoProvider packageLicenseInfoProvider, List<UsedPackageReference> references, int i)
        {
            var r = references[i];
            var license = await packageLicenseInfoProvider.TryGetLicense(r.Name, r.Version).ConfigureAwait(false);
            references[i] = new UsedPackageReference(r.Name, r.Version, r.Source, license, r.ProviderName, r.DefinitionFileName);
        }
    }
}
