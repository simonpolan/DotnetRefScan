using NSubstitute;
using System.Reflection;

namespace DotnetRefScan.Tests
{
    public class Tests
    {
        private readonly string solutionRootFolder;
        private readonly string testDataFolder;
        private readonly PackageReference packageRefJson = new("Newtonsoft.Json", "13.0.4", "NuGet");
        private readonly PackageReference packageRefCli = new("CliWrap", "3.9.0", "NuGet");

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
                Assert.That(references.Any(r => r.DefinitionFileName?.EndsWith("libman.json") == true), Is.True);
                Assert.That(references.Any(r => r.DefinitionFileName?.EndsWith("DotnetRefScan.csproj") == true), Is.True);
                Assert.That(references.Any(r => r.DefinitionFileName?.EndsWith("DotnetRefScan.Tests.csproj") == true), Is.True);
                Assert.That(references, Does.Contain(packageRefJson));
                Assert.That(references, Does.Contain(packageRefCli));
            });
        }

        [Test]
        public async Task TestLoadUsedReferencesWithFilter()
        {
            RefScan refScan = new(solutionRootFolder, filter: p => p.EndsWith("tests.csproj", StringComparison.OrdinalIgnoreCase));

            ICollection<UsedPackageReference> references = await refScan.LoadUsedReferences();

            Assert.Multiple(() =>
            {
                Assert.That(references, Is.Not.Null);
                Assert.That(references.Any(r => r.Name.StartsWith("Microsoft")), Is.True);
                Assert.That(references.Any(r => r.Name.StartsWith("System")), Is.True);
                Assert.That(references.Any(r => r.DefinitionFileName?.EndsWith("libman.json") == true), Is.False);
                Assert.That(references.Any(r => r.DefinitionFileName?.EndsWith("DotnetRefScan.csproj") == true), Is.False);
                Assert.That(references.Any(r => r.DefinitionFileName?.EndsWith("DotnetRefScan.Tests.csproj") == true), Is.True);
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
                Assert.That(references, Has.Count.EqualTo(4));
                Assert.That(references, Does.Not.Contain(new PackageReference("package1", "1.2.1", "jsdelivr")));
                Assert.That(references, Does.Contain(new PackageReference("package1", "1.2.3", "jsdelivr")));
                Assert.That(references, Does.Contain(new PackageReference("package1", "4.5.6", "cdnjs")));
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
                Assert.That(references, Has.Count.EqualTo(3));
                Assert.That(references, Does.Not.Contain(new PackageReference("package1", "1.2.1", "jsdelivr")));
                Assert.That(references, Does.Contain(new PackageReference("package1", "1.2.3", "jsdelivr")));
                Assert.That(references, Does.Contain(new PackageReference("package1", "4.5.6", "cdnjs")));
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
                Assert.That(references, Has.Count.EqualTo(4));
                Assert.That(references, Does.Contain(new PackageReference("package1", "4.5.6", "cdnjs")));
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
                IsReferenceRequiredInLicense = (r) => !r.Name.StartsWith("Microsoft") && !r.Name.StartsWith("NETStandard") && !r.Name.StartsWith("System") && !r.Name.StartsWith("NUnit") && !r.Name.StartsWith("NuGet") && r.Name != typeof(RefScan).Assembly.GetName().Name,
                IsReferenceAcceptedToBeRedundantInLicense = (r) => r.Name == "RedundantPackage"
            };

            LicenseVerificationResult result = await refScan.VerifyLicense(Path.Combine(testDataFolder, "TestLicense2.md"));

            if (result.MissingInLicense.Count > 0)
            {
                Console.WriteLine("Missing references:");
                foreach (var r in result.MissingInLicense)
                    Console.WriteLine($"\t| {r.Name} | {r.Version} | {r.Source}");
                Console.WriteLine("------------------------------------------------------------");
            }

            if (result.RedundantInLicense.Count > 0)
            {
                Console.WriteLine("Redundant references:");
                foreach (var r in result.RedundantInLicense)
                    Console.WriteLine($"\t| {r.Name} | {r.Version} | {r.Source}");
                Console.WriteLine("------------------------------------------------------------");
            }

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

        [Test]
        public async Task TestUpdateLicense()
        {
            string? output = licenseTextBeforeUpdate;

            var formatter = Substitute.ForPartsOf<MarkdownFormatter>();
            formatter.GetLines(Arg.Any<string>()).Returns(info => [.. output.Split(["\r\n", "\n", "\r"], StringSplitOptions.None)]);

            formatter.When(f => f.SaveToFile(Arg.Any<string>(), Arg.Any<string>())).DoNotCallBase();
            formatter.When(f => f.SaveToFile(Arg.Any<string>(), Arg.Any<string>())).Do(info => output = (string)info.Args()[1]);

            var licenseProvider = Substitute.ForPartsOf<MarkdownLicenseReferencesProvider>(0, 1, 2, formatter);
            licenseProvider.When(f => f.SaveToFile(Arg.Any<string>(), Arg.Any<List<string>>())).DoNotCallBase();
            licenseProvider.When(f => f.SaveToFile(Arg.Any<string>(), Arg.Any<List<string>>())).Do(info => output = string.Join(Environment.NewLine, (List<string>)info.Args()[1]).TrimEnd());

            RefScan refScan = new(solutionRootFolder)
            {
                IsReferenceRequiredInLicense = (r) => !r.Name.StartsWith("Microsoft") && !r.Name.StartsWith("NETStandard") && !r.Name.StartsWith("System") && !r.Name.StartsWith("NUnit") && !r.Source.StartsWith("NuGet") && r.Name != typeof(RefScan).Assembly.GetName().Name,
                IsReferenceAcceptedToBeRedundantInLicense = (r) => r.Name == "RedundantPackage",
                LicenseReferencesProvider = licenseProvider,
            };

            await refScan.UpdateLicense(Path.Combine(testDataFolder, "TestLicense1.md"));

            Assert.That(output, Is.EqualTo(licenseTextAfterUpdate));
        }

        private const string licenseTextBeforeUpdate = @"# DotnetRefScan license

License text...

### Copyright (C) DotnetRefScan 2025.
### All rights reserved.
### Written by DotnetRefScan.

**The solution uses following 3rd party libraries:**

| Library                           | Version | Source     | Copyright                         | License type            | License or project link         |
|-----------------------------------|---------|------------|-----------------------------------|-------------------------|---------------------------------|
| package1                          | 1.2.3   | jsdelivr   |                                   | MIT                     |                                 |
| package2                          | 4.5.6   | cdnjs      |                                   | MIT                     |                                 |
| package3                          | 7.8.9   | jsdelivr   |                                   | MIT                     |                                 |

*Additionally, .NET, Microsoft and System libraries are used*";
        private const string licenseTextAfterUpdate = @"# DotnetRefScan license

License text...

### Copyright (C) DotnetRefScan 2025.
### All rights reserved.
### Written by DotnetRefScan.

**The solution uses following 3rd party libraries:**

| Library  | Version | Source   | Copyright | License type | License or project link |
| -------- | ------- | -------- | --------- | ------------ | ----------------------- |
| package1 | 4.5.6   | cdnjs    |           |              |                         |
| package2 | 4.5.6   | cdnjs    |           | MIT          |                         |
| package1 | 1.2.3   | jsdelivr |           | MIT          |                         |
| package3 | 7.8.9   | jsdelivr |           | MIT          |                         |

*Additionally, .NET, Microsoft and System libraries are used*";
    }
}