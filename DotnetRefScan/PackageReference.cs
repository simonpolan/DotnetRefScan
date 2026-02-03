using System;
using System.Collections.Generic;

namespace DotnetRefScan
{
    /// <summary>
    /// Package reference model.
    /// </summary>
    public class PackageReference : IEquatable<PackageReference?>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PackageReference"/> class.
        /// </summary>
        /// <param name="name">Package name.</param>
        /// <param name="version">Package version</param>
        /// <param name="packageSource">Package source.</param>
        /// <param name="license">Package license.</param>
        public PackageReference(string name, string version, string packageSource, PackageLicense? license)
        {
            Name = name;
            Version = version;
            Source = packageSource;
            License = license;
        }

        /// <summary>
        /// Gets package name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets package version.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Gets package source.
        /// </summary>
        public string Source { get; }

        /// <summary>
        /// Gets package license.
        /// </summary>
        public PackageLicense? License { get; internal set; }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as PackageReference);
        }

        /// <inheritdoc/>
        public bool Equals(PackageReference? other)
        {
            return !(other is null) &&
                   Name == other.Name &&
                   Version == other.Version &&
                   Source == other.Source &&
                   License == other.License;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Version, Source, License);
        }

        /// <inheritdoc/>
        public static bool operator ==(PackageReference? left, PackageReference? right)
        {
            return EqualityComparer<PackageReference>.Default.Equals(left!, right!);
        }

        /// <inheritdoc/>
        public static bool operator !=(PackageReference? left, PackageReference? right)
        {
            return !(left == right);
        }
    }
}
