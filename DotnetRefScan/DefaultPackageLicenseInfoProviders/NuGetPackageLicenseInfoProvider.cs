using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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
        /// Tries to load license info for the given package.
        /// </summary>
        /// <param name="packageId">Package ID / name.</param>
        /// <param name="packageVersion">Package version (optional).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Instance of <see cref="PackageLicense"/> of found. Otherwise <see langword="null"/>.</returns>
        public virtual async Task<PackageLicense?> TryGetLicense(string packageId, string? packageVersion = null, CancellationToken cancellationToken = default)
        {
            var registrationUrl = $"v3/registration5-semver1/{packageId.ToLowerInvariant()}/index.json";

            using var response = await _httpClient.GetAsync(registrationUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(content);

            var root = document.RootElement;

            // Flatten all versions across pages
            var allVersions =
                root.GetProperty("items")
                    .EnumerateArray()
                    .SelectMany(page =>
                        page.TryGetProperty("items", out var items)
                            ? items.EnumerateArray()
                            : Enumerable.Empty<JsonElement>())
                    .Select(item => item.GetProperty("catalogEntry"));

            var selected =
                packageVersion == null
                    ? allVersions
                        .OrderByDescending(v => v.GetProperty("version").GetString(), StringComparer.OrdinalIgnoreCase)
                        .FirstOrDefault()
                    : allVersions.FirstOrDefault(v =>
                        string.Equals(
                            v.GetProperty("version").GetString(),
                            packageVersion,
                            StringComparison.OrdinalIgnoreCase));

            if (selected.ValueKind == JsonValueKind.Undefined)
                return null;

            var info = new
            {
                LicenseExpression = selected.TryGetProperty("licenseExpression", out var le)
                ? le.GetString()
                : null,

                LicenseUrl = selected.TryGetProperty("licenseUrl", out var lu)
                ? lu.GetString()
                : null,

                ProjectUrl = selected.TryGetProperty("projectUrl", out var p)
                ? p.GetString()
                : null,
            };

            string? copyright = null;
            if (info.LicenseUrl != null && info.LicenseUrl.EndsWith(".md", StringComparison.InvariantCultureIgnoreCase))
            {
                string licenseText = await _httpClient.GetStringAsync(info.LicenseUrl);
            }

            return new PackageLicense(copyright ?? string.Empty,
                                      info.LicenseExpression ?? string.Empty,
                                      info.LicenseUrl ?? info.ProjectUrl ?? string.Empty);
        }
    }
}
