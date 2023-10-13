namespace DotnetRefScan.Tests
{
    internal class CustomReferenceProvider : IUsedReferencesProvider
    {
        public static UsedPackageReference FakePackage = new("FakePackage01", "1.2.3", "FakeProvider", nameof(CustomReferenceProvider), null);

        public string Name => nameof(CustomReferenceProvider);

        public string? FileSearchPattern => null;

        public async Task<ICollection<UsedPackageReference>> LoadReferences(string? fileName)
        {
            return await Task.FromResult(new List<UsedPackageReference>()
            {
                FakePackage
            }).ConfigureAwait(false);
        }
    }
}
