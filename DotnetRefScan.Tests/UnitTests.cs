using DotnetRefScan.Default;
using NSubstitute;
using System.Reflection;
using System.Text;

namespace DotnetRefScan.Tests
{
    [NonParallelizable]
    public class Tests
    {
        private readonly string solutionRootFolder;
        private readonly string testDataFolder;

        public Tests()
        {
            solutionRootFolder = StorageHelper.GetSolutionRoot()!;
            testDataFolder = Path.Combine(solutionRootFolder, Assembly.GetExecutingAssembly().GetName().Name!, "TestData");
        }

        [OneTimeSetUp]
        public async Task Setup()
        {
            RefScan refScan = new(solutionRootFolder);

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    // This is needed to have the test succeed in CI - not clear why, but the first execution tends to fail on:
                    // System.IO.IOException : Cannot assign requested address
                    // ----> System.Net.Sockets.SocketException : Cannot assign requested address
                    // Stack Trace:
                    //    at System.IO.Pipes.PipeStream.ReadAsyncCore(Memory`1 destination, CancellationToken cancellationToken)
                    _ = await refScan.LoadUsedReferences();
                    break;
                }
                catch
                {
                    // Ignore
                    await Task.Delay(250);
                }
            }
        }

        [Test]
        public async Task LoadUsedReferences()
        {
            RefScan refScan = new(solutionRootFolder, filter: p => !p.EndsWith("tests.csproj", StringComparison.OrdinalIgnoreCase));

            ICollection<UsedPackageReference> references = await refScan.LoadUsedReferences();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(references, Is.Not.Null);
                Assert.That(references.Any(r => r.Name.StartsWith("Microsoft")), Is.True);
                Assert.That(references.Any(r => r.Name.StartsWith("System")), Is.True);
                Assert.That(references.Any(r => r.DefinitionFileName?.EndsWith("libman.json") == true), Is.True);
                Assert.That(references.Any(r => r.DefinitionFileName?.EndsWith("DotnetRefScan.csproj") == true), Is.True);
                Assert.That(references.Any(r => r.DefinitionFileName?.EndsWith("DotnetRefScan.Tests.csproj") == true), Is.False);
                Assert.That(references.Any(r => r.Name == "System.Text.Json" && r.Source == "NuGet"), Is.True);
            }
        }

        [Test]
        public async Task LoadUsedReferencesWithFilter()
        {
            RefScan refScan = new(solutionRootFolder, filter: p => p.EndsWith("tests.csproj", StringComparison.OrdinalIgnoreCase));

            ICollection<UsedPackageReference> references = await refScan.LoadUsedReferences();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(references, Is.Not.Null);
                Assert.That(references.Any(r => r.Name.StartsWith("Microsoft")), Is.True);
                Assert.That(references.Any(r => r.Name.StartsWith("System")), Is.True);
                Assert.That(references.Any(r => r.DefinitionFileName?.EndsWith("libman.json") == true), Is.False);
                Assert.That(references.Any(r => r.DefinitionFileName?.EndsWith("DotnetRefScan.csproj") == true), Is.False);
                Assert.That(references.Any(r => r.DefinitionFileName?.EndsWith("DotnetRefScan.Tests.csproj") == true), Is.True);
                Assert.That(references.Any(r => r.Name == "System.Text.Json" && r.Source == "NuGet"), Is.True);
            }
        }

        [Test]
        public async Task LoadUsedReferencesWithCustomProvider()
        {
            RefScan refScan = new(solutionRootFolder);

            refScan.UsedReferencesProviders.Add(new CustomReferenceProvider());

            ICollection<UsedPackageReference> references = await refScan.LoadUsedReferences();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(references, Is.Not.Null);
                Assert.That(references.Any(r => r.Name.StartsWith("Microsoft")), Is.True);
                Assert.That(references.Any(r => r.Name.StartsWith("System")), Is.True);
                Assert.That(references.Contains(CustomReferenceProvider.FakePackage), Is.True);
            }
        }

        [Test]
        public async Task LoadUsedReferencesWithCustomProviderOnly()
        {
            RefScan refScan = new(solutionRootFolder);

            refScan.UsedReferencesProviders.Clear();
            refScan.UsedReferencesProviders.Add(new CustomReferenceProvider());

            ICollection<UsedPackageReference> references = await refScan.LoadUsedReferences();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(references, Is.Not.Null);
                Assert.That(references, Has.Count.EqualTo(1));
                Assert.That(references.Any(r => r.Name.StartsWith("Microsoft")), Is.False);
                Assert.That(references.Any(r => r.Name.StartsWith("System")), Is.False);
                Assert.That(references.Contains(CustomReferenceProvider.FakePackage), Is.True);
            }
        }

        [Test]
        public async Task LoadUsedReferencesFromAllSubfolders()
        {
            RefScan refScan = new(testDataFolder);

            ICollection<UsedPackageReference> references = await refScan.LoadUsedReferences();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(references, Is.Not.Null);
                Assert.That(references, Has.Count.EqualTo(4));
                Assert.That(references, Does.Not.Contain(new PackageReference("package1", "1.2.1", "jsdelivr", null)));
                Assert.That(references, Does.Contain(new PackageReference("package1", "1.2.3", "jsdelivr", null)));
                Assert.That(references, Does.Contain(new PackageReference("package1", "4.5.6", "cdnjs", null)));
                Assert.That(references, Does.Contain(new PackageReference("package2", "4.5.6", "cdnjs", null)));
                Assert.That(references, Does.Contain(new PackageReference("package3", "7.8.9", "jsdelivr", null)));
            }
        }

        [Test]
        public async Task LoadUsedReferencesFromCurrentFolderOnly()
        {
            RefScan refScan = new(testDataFolder, SearchOption.TopDirectoryOnly);

            ICollection<UsedPackageReference> references = await refScan.LoadUsedReferences();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(references, Is.Not.Null);
                Assert.That(references, Has.Count.EqualTo(3));
                Assert.That(references, Does.Not.Contain(new PackageReference("package1", "1.2.1", "jsdelivr", null)));
                Assert.That(references, Does.Contain(new PackageReference("package1", "1.2.3", "jsdelivr", null)));
                Assert.That(references, Does.Contain(new PackageReference("package1", "4.5.6", "cdnjs", null)));
                Assert.That(references, Does.Contain(new PackageReference("package2", "4.5.6", "cdnjs", null)));
                Assert.That(references, Does.Not.Contain(new PackageReference("package3", "7.8.9", "jsdelivr", null)));
            }
        }

        [Test]
        public async Task LoadLicenseReferences()
        {
            RefScan refScan = new(testDataFolder);

            ICollection<PackageReference> references = await refScan.LoadLicenseReferences(Path.Combine(testDataFolder, "TestLicense1.md"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(references, Is.Not.Null);
                Assert.That(references, Has.Count.EqualTo(4));
                Assert.That(references, Does.Contain(new PackageReference("package1", "4.5.6", "cdnjs", null)));
                Assert.That(references, Does.Contain(new PackageReference("package1", "1.2.3", "jsdelivr", new PackageLicense(string.Empty, "MIT", string.Empty))));
                Assert.That(references, Does.Contain(new PackageReference("package2", "4.5.6", "cdnjs", new PackageLicense(string.Empty, "MIT", string.Empty))));
                Assert.That(references, Does.Contain(new PackageReference("package3", "7.8.9", "jsdelivr", new PackageLicense("copyright", "MIT", "N/A"))));
            }
        }

        [Test]
        public async Task VerifyLicense_IgnoreLicenseInfo()
        {
            RefScan refScan = new(solutionRootFolder, filter: fileName => !fileName.EndsWith(".Tests.csproj"), verifyLicenseInfo: false)
            {
                IsReferenceRequiredInLicense = (r) => !r.Name.StartsWith("Microsoft") && !r.Name.StartsWith("NETStandard") && !r.Name.StartsWith("System") && r.Name != typeof(RefScan).Assembly.GetName().Name,
                IsReferenceAcceptedToBeRedundantInLicense = (r) => r.Name == "RedundantPackage"
            };

            string licenseFileName = Path.Combine(testDataFolder, "TestLicense2.md");
            LicenseVerificationResult result = await refScan.VerifyLicense(licenseFileName);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.IsUpToDate, Is.True);
                Assert.That(result.UsedPackageReferences, Has.Count.GreaterThan(0));
                Assert.That(result.LicensePackageReferences, Has.Count.EqualTo(5));
                Assert.That(result.MissingInLicense, Has.Count.Zero);
                Assert.That(result.RedundantInLicense, Has.Count.Zero);
                Assert.That(result.WithInvalidLicense, Has.Count.Zero);
            }
        }

        [Test]
        public async Task VerifyLicense_CheckLicenseInfo()
        {
            RefScan refScan = new(solutionRootFolder, filter: fileName => !fileName.EndsWith(".Tests.csproj"))
            {
                IsReferenceRequiredInLicense = (r) => !r.Name.StartsWith("Microsoft") && !r.Name.StartsWith("NETStandard") && !r.Name.StartsWith("System") && r.Name != typeof(RefScan).Assembly.GetName().Name,
                IsReferenceAcceptedToBeRedundantInLicense = (r) => r.Name == "RedundantPackage"
            };

            string licenseFileName = Path.Combine(testDataFolder, "TestLicense3.md");
            LicenseVerificationResult result = await refScan.VerifyLicense(licenseFileName);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.IsUpToDate, Is.False);
                Assert.That(result.UsedPackageReferences, Has.Count.GreaterThan(0));
                Assert.That(result.LicensePackageReferences, Has.Count.EqualTo(5));
                Assert.That(result.MissingInLicense, Has.Count.Zero);
                Assert.That(result.RedundantInLicense, Has.Count.Zero);
                Assert.That(result.WithInvalidLicense, Has.Count.EqualTo(1));
                Assert.That(result.WithInvalidLicense.FirstOrDefault(), Is.EqualTo(new PackageReference("package1", "4.5.6", "cdnjs", null)));
            }
        }

        [Test]
        public async Task UpdateLicense()
        {
            string? output = LicenseTextBeforeUpdate;

            var formatter = Substitute.ForPartsOf<MarkdownFormatter>();
            formatter.GetLines(Arg.Any<string>()).Returns(info => [.. output.Split(["\r\n", "\n", "\r"], StringSplitOptions.None)]);

            formatter.When(f => f.SaveToFile(Arg.Any<string>(), Arg.Any<string>())).DoNotCallBase();
            formatter.When(f => f.SaveToFile(Arg.Any<string>(), Arg.Any<string>())).Do(info => output = (string)info.Args()[1]);

            var licenseProvider = Substitute.ForPartsOf<MarkdownLicenseReferencesProvider>(0, 1, 2, 3, 4, 5, true, formatter);
            licenseProvider.When(f => f.SaveToFile(Arg.Any<string>(), Arg.Any<List<string>>())).DoNotCallBase();
            licenseProvider.When(f => f.SaveToFile(Arg.Any<string>(), Arg.Any<List<string>>())).Do(info => output = string.Join(Environment.NewLine, (List<string>)info.Args()[1]).TrimEnd());

            RefScan refScan = new(solutionRootFolder)
            {
                IsReferenceRequiredInLicense = (r) => !r.Name.StartsWith("Microsoft") && !r.Name.StartsWith("NETStandard") && !r.Name.StartsWith("System") && !r.Name.StartsWith("NUnit") && (!r.Source.StartsWith("NuGet") || r.Name == "Newtonsoft.Json") && r.Name != typeof(RefScan).Assembly.GetName().Name,
                IsReferenceAcceptedToBeRedundantInLicense = (r) => r.Name == "RedundantPackage",
                LicenseReferencesProvider = licenseProvider,
            };

            await refScan.UpdateLicense(Path.Combine(testDataFolder, "TestLicense1.md"));

            Assert.That(output, Is.EqualTo(LicenseTextAfterUpdate));
        }

        private static string C => Encoding.UTF8.GetString([194, 169]);

        private static string LicenseTextBeforeUpdate => @"# DotnetRefScan license

License text...

### Copyright (C) DotnetRefScan.
### All rights reserved.
### Written by DotnetRefScan.

**The solution uses following 3rd party libraries:**

| Library                           | Version | Source     | Copyright                         | License type            | License or project link         |
|-----------------------------------|---------|------------|-----------------------------------|-------------------------|---------------------------------|
| package1                          | 1.2.3   | jsdelivr   | test                              | MIT                     |                                 |
| package2                          | 4.5.6   | cdnjs      |                                   | MIT                     | http://test                     |
| package3                          | 7.8.9   | jsdelivr   |                                   | MIT                     |                                 |

*Additionally, .NET, Microsoft and System libraries are used*";

        private static string LicenseTextAfterUpdate => $@"# DotnetRefScan license

License text...

### Copyright (C) DotnetRefScan.
### All rights reserved.
### Written by DotnetRefScan.

**The solution uses following 3rd party libraries:**

| Library         | Version | Source   | Copyright                          | License type | License or project link                    |
| --------------- | ------- | -------- | ---------------------------------- | ------------ | ------------------------------------------ |
| package1        | 4.5.6   | cdnjs    |                                    |              |                                            |
| package2        | 4.5.6   | cdnjs    |                                    | MIT          | http://test                                |
| package1        | 1.2.3   | jsdelivr | test                               | MIT          |                                            |
| package3        | 7.8.9   | jsdelivr |                                    | MIT          |                                            |
| Newtonsoft.Json | 13.0.4  | NuGet    | Copyright {C} James Newton-King 2008 | MIT          | https://github.com/JamesNK/Newtonsoft.Json |

*Additionally, .NET, Microsoft and System libraries are used*";
    }
}