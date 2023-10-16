using System.Reflection;

namespace DotnetRefScan.Tests
{
    public class Tests
    {
        private readonly string solutionRootFolder;
        private readonly string testDataFolder;
        private readonly PackageReference packageRefJson = new("Newtonsoft.Json", "13.0.3", "NuGet");
        private readonly PackageReference packageRefCli = new("CliWrap", "3.6.4", "NuGet");

        public Tests()
        {
            solutionRootFolder = StorageHelper.GetSolutionRoot()!;
            testDataFolder = Path.Combine(solutionRootFolder, Assembly.GetExecutingAssembly().GetName().Name!, "TestData");
        }

        [Test]
        public async Task TestLoadUsedReferences()
        {
            RefScan refScan = new(solutionRootFolder);

            ICollection<UsedPackageReference> references = await refScan.LoadUsedReferences();

            Assert.Multiple(() =>
            {
                Assert.That(references, Is.Not.Null);
                Assert.That(references.Any(r => r.Name.StartsWith("Microsoft")), Is.True);
                Assert.That(references.Any(r => r.Name.StartsWith("System")), Is.True);
                Assert.That(references, Does.Contain(packageRefJson));
                Assert.That(references, Does.Contain(packageRefCli));
            });
        }

        [Test]
        public async Task TestLoadUsedReferencesWithCustomProvider()
        {
            RefScan refScan = new(solutionRootFolder);

            refScan.UsedReferencesProviders.Add(new CustomReferenceProvider());

            ICollection<UsedPackageReference> references = await refScan.LoadUsedReferences();

            Assert.Multiple(() =>
            {
                Assert.That(references, Is.Not.Null);
                Assert.That(references.Any(r => r.Name.StartsWith("Microsoft")), Is.True);
                Assert.That(references.Any(r => r.Name.StartsWith("System")), Is.True);
                Assert.That(references.Contains(CustomReferenceProvider.FakePackage), Is.True);
            });
        }

        [Test]
        public async Task TestLoadUsedReferencesWithCustomProviderOnly()
        {
            RefScan refScan = new(solutionRootFolder);

            refScan.UsedReferencesProviders.Clear();
            refScan.UsedReferencesProviders.Add(new CustomReferenceProvider());

            ICollection<UsedPackageReference> references = await refScan.LoadUsedReferences();

            Assert.Multiple(() =>
            {
                Assert.That(references, Is.Not.Null);
                Assert.That(references, Has.Count.EqualTo(1));
                Assert.That(references.Any(r => r.Name.StartsWith("Microsoft")), Is.False);
                Assert.That(references.Any(r => r.Name.StartsWith("System")), Is.False);
                Assert.That(references.Contains(CustomReferenceProvider.FakePackage), Is.True);
            });
        }

        [Test]
        public async Task TestLoadUsedReferencesFromAllSubfolders()
        {
            RefScan refScan = new(testDataFolder);

            ICollection<UsedPackageReference> references = await refScan.LoadUsedReferences();

            Assert.Multiple(() =>
            {
                Assert.That(references, Is.Not.Null);
                Assert.That(references, Has.Count.EqualTo(3));
                Assert.That(references, Does.Contain(new PackageReference("package1", "1.2.3", "jsdelivr")));
                Assert.That(references, Does.Contain(new PackageReference("package2", "4.5.6", "cdnjs")));
                Assert.That(references, Does.Contain(new PackageReference("package3", "7.8.9", "jsdelivr")));
            });
        }

        [Test]
        public async Task TestLoadUsedReferencesFromCurrentFolderOnly()
        {
            RefScan refScan = new(testDataFolder, SearchOption.TopDirectoryOnly);

            ICollection<UsedPackageReference> references = await refScan.LoadUsedReferences();

            Assert.Multiple(() =>
            {
                Assert.That(references, Is.Not.Null);
                Assert.That(references, Has.Count.EqualTo(2));
                Assert.That(references, Does.Contain(new PackageReference("package1", "1.2.3", "jsdelivr")));
                Assert.That(references, Does.Contain(new PackageReference("package2", "4.5.6", "cdnjs")));
                Assert.That(references, Does.Not.Contain(new PackageReference("package3", "7.8.9", "jsdelivr")));
            });
        }

        [Test]
        public async Task TestLoadLicenseReferences()
        {
            RefScan refScan = new(testDataFolder);

            ICollection<PackageReference> references = await refScan.LoadLicenseReferences(Path.Combine(testDataFolder, "TestLicense1.md"));

            Assert.Multiple(() =>
            {
                Assert.That(references, Is.Not.Null);
                Assert.That(references, Has.Count.EqualTo(3));
                Assert.That(references, Does.Contain(new PackageReference("package1", "1.2.3", "jsdelivr")));
                Assert.That(references, Does.Contain(new PackageReference("package2", "4.5.6", "cdnjs")));
                Assert.That(references, Does.Contain(new PackageReference("package3", "7.8.9", "jsdelivr")));
            });
        }

        [Test]
        public async Task TestVerifyLicense()
        {
            RefScan refScan = new(solutionRootFolder)
            {
                IsReferenceRequiredInLicense = (r) => !r.Name.StartsWith("Microsoft") && !r.Name.StartsWith("NETStandard") && !r.Name.StartsWith("System") && !r.Name.StartsWith("NUnit") && !r.Name.StartsWith("NuGet"),
                IsReferenceAcceptedToBeRedundantInLicense = (r) => r.Name == "RedundantPackage"
            };

            LicenseVerificationResult result = await refScan.VerifyLicense(Path.Combine(testDataFolder, "TestLicense2.md"));

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.IsUpToDate, Is.True);
                Assert.That(result.UsedPackageReferences, Has.Count.GreaterThan(0));
                Assert.That(result.LicensePackageReferences, Has.Count.GreaterThan(0));
                Assert.That(result.MissingInLicense, Has.Count.Zero);
                Assert.That(result.RedundantInLicense, Has.Count.Zero);
            });
        }
    }
}