using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace DotnetRefScan.DefaultPackageLicenseInfoProviders
{
    /// <summary>
    /// NuGet package license info provider.
    /// </summary>
    public class NuGetPackageLicenseInfoProvider : IPackageLicenseInfoProvider
    {
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initiates new instance of <see cref="NuGetPackageLicenseInfoProvider"/>.
        /// </summary>
        /// <param name="feedBaseUrl">NuGet feed base URL.</param>
        public NuGetPackageLicenseInfoProvider(string feedBaseUrl = "https://api.nuget.org/")
        {
            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri(feedBaseUrl)
            };
        }

        /// <summary>
        /// Tries to load license info for the given package via NuGet v3-flatcontainer API.
        /// </summary>
        /// <param name="packageId">Package ID / name.</param>
        /// <param name="packageVersion">Package version (optional).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Instance of <see cref="PackageLicense"/> of found. Otherwise <see langword="null"/>.</returns>
        public virtual async Task<PackageLicense?> TryGetLicense(string packageId, string? packageVersion = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(packageId))
                    throw new ArgumentException(nameof(packageId));

                if (string.IsNullOrWhiteSpace(packageVersion))
                    throw new ArgumentException(nameof(packageVersion));

                var idLower = packageId.ToLowerInvariant();
                var xml = await _httpClient.GetStringAsync($"/v3-flatcontainer/{idLower}/{packageVersion}/{idLower}.nuspec");

                var doc = new XmlDocument();
                doc.LoadXml(xml);

                var nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("ns", doc.DocumentElement!.NamespaceURI);

                // Repository URL
                string? repositoryUrl = doc
                    .SelectSingleNode("//ns:repository", nsmgr)?
                    .Attributes?["url"]?
                    .Value;

                // Copyright
                string? copyright = doc
                    .SelectSingleNode("//ns:copyright", nsmgr)?
                    .InnerText?
                    .Trim();

                // License (expression preferred)
                string? license = doc
                    .SelectSingleNode("//ns:license[@type='expression']", nsmgr)?
                    .InnerText?
                    .Trim();

                // Fallback to legacy licenseUrl
                if (string.IsNullOrEmpty(license))
                {
                    license = doc
                        .SelectSingleNode("//ns:licenseUrl", nsmgr)?
                        .InnerText?
                        .Trim();
                }

                return new PackageLicense(copyright ?? string.Empty,
                                          license ?? string.Empty,
                                          repositoryUrl ?? string.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return null;
            }
        }
    }
}
