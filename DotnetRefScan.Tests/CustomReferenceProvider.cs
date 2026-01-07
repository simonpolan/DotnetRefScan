namespace DotnetRefScan.Tests
{
    internal class CustomReferenceProvider : IUsedReferencesProvider
    {
        public static UsedPackageReference FakePackage = new("FakePackage01", "1.2.3", "FakeProvider", null, nameof(CustomReferenceProvider), null);

        public string Name => nameof(CustomReferenceProvider);

        public string? FileSearchPattern => null;

        public IPackageLicenseInfoProvider? PackageLicenseInfoProvider => null;

        public int LicenseInfoLoadingMaxDegreeOfParallelism { get; set; } = 1;

        public Task<ICollection<UsedPackageReference>> LoadReferences(string? fileName, Func<UsedPackageReference, bool>? shouldLoadLicense)
        {
            return Task.FromResult((ICollection<UsedPackageReference>)
            [
                FakePackage
            ]);
        }
    }
}
