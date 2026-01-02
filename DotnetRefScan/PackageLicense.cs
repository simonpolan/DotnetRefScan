using System;
using System.Collections.Generic;

namespace DotnetRefScan
{
    /// <summary>
    /// Package license model.
    /// </summary>
    public class PackageLicense : IEquatable<PackageLicense?>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PackageLicense"/> class.
        /// </summary>
        /// <param name="copyright">Package copyright.</param>
        /// <param name="type">Package license type.</param>
        /// <param name="url">Package repository URL.</param>
        public PackageLicense(string copyright, string type, string url)
        {
            Copyright = copyright;
            Type = type;
            RepositoryUrl = url;
        }

        /// <summary>
        /// Gets package copyright.
        /// </summary>
        public string Copyright { get; }

        /// <summary>
        /// Gets package license type.
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Gets package repository URL.
        /// </summary>
        public string RepositoryUrl { get; }

        /// <summary>
        /// Gets a value indicating whether the package license is valid (non-empty).
        /// </summary>
        /// <returns><see langword="true"/> if valid.</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Copyright) && !string.IsNullOrWhiteSpace(Type) && !string.IsNullOrWhiteSpace(RepositoryUrl);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as PackageLicense);
        }

        /// <inheritdoc/>
        public bool Equals(PackageLicense? other)
        {
            return !(other is null) &&
                   Copyright == other.Copyright &&
                   Type == other.Type &&
                   RepositoryUrl == other.RepositoryUrl;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(Copyright, Type, RepositoryUrl);
        }

        /// <inheritdoc/>
        public static bool operator ==(PackageLicense? left, PackageLicense? right)
        {
            return EqualityComparer<PackageLicense>.Default.Equals(left!, right!);
        }

        /// <inheritdoc/>
        public static bool operator !=(PackageLicense? left, PackageLicense? right)
        {
            return !(left == right);
        }
    }
}
