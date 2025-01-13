# DotnetRefScan
Reference scanner for Dotnet applications.

It loads used package references from your source code and package references from your license file and compares them to ensure your license file contains all used package references.

[![PR Build and Test](https://github.com/simonpolan/DotnetRefScan/actions/workflows/pr-build-and-test.yml/badge.svg)](https://github.com/simonpolan/DotnetRefScan/actions/workflows/pr-build-and-test.yml)

## Usage

### Reference the library
Reference the library in your project via NuGet (https://www.nuget.org/packages/DotnetRefScan)
```
DotnetRefScan
```


### Init the library

```
RefScan refScan = new RefScan("e.g. Solution or Project folder");
```
*There is optional parameter to specify `SearchOption`, which configures the library to either search for references only in the current folder or its subfolders as well.*


### Verify your license
You can simply let the library to compare your license agains source code and verify whether the license contains all used package references.
To do that, call:

```
var licensePackageReferences = await refScan.LoadLicenseReferences("License file name");
```

The returned object will show, whether the license file `IsUpToDate` and provides list of:
- `UsedPackageReferences`
- `LicensePackageReferences`
- Package references `MissingInLicense`
- Package references `RedundantInLicense`


# Customization

### UsedReferencesProviders

You can modify the list of `UsedReferencesProviders` in order to add new or remove default providers which scan the source code for used package references.

The default provides scan:
- C# project files (*.csproj) for NuGet packages
- Libman files (libman.json) for front-end packages from jsdeliver, cdnjs etc.


### LicenseReferencesProvider

The default `LicenseReferencesProvider` reads package references mentioned in the license file using Markdown format.

It expects the license file to have table-based package definition with "|" symbols as separators and specific columns. See **/DotnetRefScan.Tests/TestData/TestLicense1.md** for reference.

You can replace the default provider with your custom or re-configure the default provider by calling:
```
refScan.LicenseReferencesProvider = new MarkdownLicenseReferencesProvider(x, y, z);
```
Where `x`, `y` and `z` specify column indexes of your license file table, which contain the package reference **name**, **version** and **source**.


### Filtering the package references

You can specify two filters which will be be used within the `LoadLicenseReferences` method during the license verification, so you don't need to write custom reference providers just to filter some references out of the result.

#### `IsReferenceRequiredInLicense`
This filter is used to specify what package references are not needed to be mentioned in the license.

The following code will not require any package starting with "Microsoft" or "System" to be referenced in the license file and the license will be still considered up-to-date:
```
refScan.IsReferenceRequiredInLicense = (r) => !r.Name.StartsWith("Microsoft") && !r.Name.StartsWith("System");
```

#### `IsReferenceAcceptedToBeRedundantInLicense`
This filter is used to specify what package references can be mentioned in the license even if they are not found by the `UsedReferencesProviders` in the source code.
This might be useful if you reference some 3rd party packages for example by using some library-unsupported package manager or by simply downloading the files and putting them in your source code manually.

The following code will not report "CustomFont" reference in the license file as redundant and the license will be still considered up-to-date:
```
refScan.IsReferenceAcceptedToBeRedundantInLicense = (r) => r.Name == "CustomFont";
```

## Recommendation

It is recommend you to include this library in your software tests, ideally to run it as part of your Pull Request checks, so it makes sure you don't merge in any package references not mentioned in your license file.


## Other features

#### To list used package references from your source code:

```
var usedPackageReferences = await refScan.LoadUsedReferences();
```


#### To list package references mentioned in your license file:

```
var licensePackageReferences = await refScan.LoadLicenseReferences("License file name");
```

---

## Release notes

### 2.0.0
The library now searches for only the latest version of a specific reference (if multiple versions used in the project) when verifying license.

### 1.0.2
Initial version of the library

## License

This library is released under MIT license. Feel free to use it as your wish.

Hope this library will help you to manage used package references in your projects easier and will make your license file be always up-to-date so you can be safe and out of any troubles with licensing.

## Contribution

If you find any problems with this library or would like to contribute to it, do not hesitate to contact me via LinkedIn or propose new change by creating a new Pull Request within this repository.
