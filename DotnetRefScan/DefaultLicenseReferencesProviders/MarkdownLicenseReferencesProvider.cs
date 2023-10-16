using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotnetRefScan
{
    /// <summary>
    /// Library references provider for Markdown license files with references listed in a "|" separated table.
    /// The package reference values will be parsed from individual table columns by given column index.
    /// For example license file see the project's repository test files.
    /// </summary>
    public class MarkdownLicenseReferencesProvider : ILicenseReferencesProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MarkdownLicenseReferencesProvider"/> class.
        /// </summary>
        /// <param name="nameColumnIndex">Name column index.</param>
        /// <param name="versionColumnIndex">Version column index.</param>
        /// <param name="sourceColumnIndex">Source column index.</param>
        public MarkdownLicenseReferencesProvider(int nameColumnIndex = 0, int versionColumnIndex = 1, int sourceColumnIndex = 2)
        {
            NameColumnIndex = nameColumnIndex;
            VersionColumnIndex = versionColumnIndex;
            SourceColumnIndex = sourceColumnIndex;
        }

        /// <inheritdoc/>
        public string Name => nameof(CSharpProjectFileUsedReferencesProvider);

        /// <summary>
        /// Gets name column index.
        /// </summary>
        public int NameColumnIndex { get; }

        /// <summary>
        /// Gets version column index.
        /// </summary>
        public int VersionColumnIndex { get; }

        /// <summary>
        /// Gets source column index.
        /// </summary>
        public int SourceColumnIndex { get; }

        /// <inheritdoc/>
        public async Task<ICollection<PackageReference>> LoadReferences(string? licenseFileName)
        {
            List<PackageReference> references = new List<PackageReference>();

            UTF8Encoding utf8WithoutBom = new UTF8Encoding(false);

            using StreamReader sr = new StreamReader(licenseFileName, utf8WithoutBom);
            string licenseDefinition = await sr.ReadToEndAsync().ConfigureAwait(false);
            sr.Close();

            string[] licenseLines = licenseDefinition.Split(Environment.NewLine.ToCharArray());
            bool tableBodyEntered = false;

            foreach (string line in licenseLines)
            {
                string lineContent = line.Trim();

                if (!tableBodyEntered)
                {
                    if (lineContent.StartsWith("|-"))
                    {
                        tableBodyEntered = true;
                    }
                    continue;
                }

                if (lineContent.Length == 0 || lineContent.StartsWith('|') == false || lineContent.Contains(' ') == false)
                {
                    continue;
                }

                string[] packageInfo = lineContent.Trim().Trim('|').Split('|');

                references.Add(new PackageReference(packageInfo[NameColumnIndex].Trim(), packageInfo[VersionColumnIndex].Trim(), packageInfo[SourceColumnIndex].Trim()));
            }

            return references
                .DistinctBy(p => $"{p.Name}@{p.Version}")
                .ToList();
        }
    }
}
