namespace DotnetRefScan
{
    /// <summary>
    /// Used package reference model.
    /// </summary>
    public class UsedPackageReference : PackageReference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UsedPackageReference"/> class.
        /// </summary>
        /// <param name="name">Package name.</param>
        /// <param name="version">Package version</param>
        /// <param name="packageSource">Package source.</param>
        /// <param name="providerName">Reference provider name.</param>
        /// <param name="definitionFileName">Definition file name.</param>
        public UsedPackageReference(string name, string version, string packageSource, string? providerName, string? definitionFileName) : base(name, version, packageSource)
        {
            ProviderName = providerName;
            DefinitionFileName = definitionFileName;
        }

        /// <summary>
        /// Gets reference provider name.
        /// </summary>
        public string? ProviderName { get; }

        /// <summary>
        /// Gets definition file name.
        /// </summary>
        public string? DefinitionFileName { get; }
    }
}
