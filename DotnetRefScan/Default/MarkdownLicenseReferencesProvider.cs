using DotnetRefScan.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotnetRefScan.Default
{
    /// <summary>
    /// Library references provider for Markdown license files with references listed in a "|" separated table.
    /// The package reference values will be parsed from individual table columns by given column index.
    /// For example license file see the project's repository test files.
    /// </summary>
    public class MarkdownLicenseReferencesProvider : ILicenseReferencesProvider
    {
        private readonly IMarkdownFormatter _markdownFormatter;

        /// <summary>
        /// Initializes a new instance of the <see cref="MarkdownLicenseReferencesProvider"/> class.
        /// </summary>
        /// <param name="nameColumnIndex">Name column index.</param>
        /// <param name="versionColumnIndex">Version column index.</param>
        /// <param name="sourceColumnIndex">Source column index.</param>
        /// <param name="copyrightColumnIndex">Copyright column index.</param>
        /// <param name="licenseColumnIndex">License column index.</param>
        /// <param name="urlColumnIndex">URL column index.</param>
        /// <param name="verifyLicenseInfo">Should license info be loaded and verified in the license file?</param>
        /// <param name="markdownFormatter">Markdown formatter implementation.</param>
        public MarkdownLicenseReferencesProvider(int nameColumnIndex = 0,
                                                 int versionColumnIndex = 1,
                                                 int sourceColumnIndex = 2,
                                                 int? copyrightColumnIndex = 3,
                                                 int? licenseColumnIndex = 4,
                                                 int? urlColumnIndex = 5,
                                                 bool verifyLicenseInfo = true,
                                                 IMarkdownFormatter? markdownFormatter = null)
        {
            NameColumnIndex = nameColumnIndex;
            VersionColumnIndex = versionColumnIndex;
            SourceColumnIndex = sourceColumnIndex;
            CopyrightColumnIndex = copyrightColumnIndex;
            LicenseColumnIndex = licenseColumnIndex;
            UrlColumnIndex = urlColumnIndex;
            VerifyLicenseInfo = verifyLicenseInfo;
            _markdownFormatter = markdownFormatter ?? new MarkdownFormatter();
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

        /// <summary>
        /// Gets copyright column index.
        /// </summary>
        public int? CopyrightColumnIndex { get; }

        /// <summary>
        /// Gets license column index.
        /// </summary>
        public int? LicenseColumnIndex { get; }

        /// <summary>
        /// Gets URL column index.
        /// </summary>
        public int? UrlColumnIndex { get; }

        /// <summary>
        /// Gets value indicating whether the package license infos should be loaded and verified in the license file.
        /// </summary>
        public bool VerifyLicenseInfo { get; }

        /// <inheritdoc/>
        public virtual Task AddOrUpdateReference(string licenseFileName, PackageReference? oldReference, PackageReference newReference)
        {
            var lines = _markdownFormatter.GetLines(licenseFileName);

            var bodyIndexes = _markdownFormatter.GetTableBodyIndexes(lines);
            if (bodyIndexes == null)
                return Task.CompletedTask;

            bool saveChanges = oldReference == null;

            // Try to find & update existing reference
            if (oldReference != null)
            {
                for (int i = bodyIndexes.Value.From; i < bodyIndexes.Value.To; i++)
                {
                    List<string> fields = _markdownFormatter.GetRowFields(lines[i]);

                    if (oldReference != null && IsReferenceRow(fields, oldReference))
                    {
                        UpdateReferenceRow(fields, newReference);
                        saveChanges = true;
                    }

                    lines[i] = _markdownFormatter.GetRowString(fields);
                }
            }
            // Insert new reference
            else
            {
                lines.Insert(bodyIndexes.Value.To, CreateReferenceRow(newReference));
            }

            if (saveChanges)
            {
                SaveToFile(licenseFileName, lines);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual Task FormatLicense(string licenseFileName)
        {
            return _markdownFormatter.FormatTable(licenseFileName);
        }

        /// <inheritdoc/>
        public virtual Task<List<PackageReference>> LoadReferences(string licenseFileName)
        {
            List<PackageReference> references = new List<PackageReference>();

            var lines = _markdownFormatter.GetLines(licenseFileName);

            var bodyIndexes = _markdownFormatter.GetTableBodyIndexes(lines);
            if (bodyIndexes == null)
                return Task.FromResult(new List<PackageReference>());

            for (int i = bodyIndexes.Value.From; i < bodyIndexes.Value.To; i++)
            {
                List<string> packageInfo = _markdownFormatter.GetRowFields(lines[i]);
                references.Add(new PackageReference(packageInfo[NameColumnIndex].Trim(), packageInfo[VersionColumnIndex].Trim(), packageInfo[SourceColumnIndex].Trim(), GetLicenseInfo(packageInfo)));
            }

            return Task.FromResult(references
                .DistinctBy(p => $"{p.Name}@{p.Version}")
                .ToList());
        }

        /// <inheritdoc/>
        public Task RemoveReference(string licenseFileName, PackageReference reference)
        {
            var lines = _markdownFormatter.GetLines(licenseFileName);

            var bodyIndexes = _markdownFormatter.GetTableBodyIndexes(lines);
            if (bodyIndexes == null)
                return Task.CompletedTask;

            int rowIndex = -1;

            for (int i = bodyIndexes.Value.From; i < bodyIndexes.Value.To; i++)
            {
                List<string> fields = _markdownFormatter.GetRowFields(lines[i]);
                if (IsReferenceRow(fields, reference))
                {
                    rowIndex = i;
                    break;
                }
            }

            if (rowIndex >= 0)
            {
                lines.RemoveAt(rowIndex);
                SaveToFile(licenseFileName, lines);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task SortReferences(string licenseFileName)
        {
            var lines = _markdownFormatter.GetLines(licenseFileName);

            var bodyIndexes = _markdownFormatter.GetTableBodyIndexes(lines);
            if (bodyIndexes == null)
                return Task.CompletedTask;

            int tableBodyRowCount = bodyIndexes.Value.To - bodyIndexes.Value.From;

            // Sort table body
            var tableBodySorted = lines.GetRange(bodyIndexes.Value.From, tableBodyRowCount);
            tableBodySorted = tableBodySorted
                .OrderBy(l => _markdownFormatter.GetRowField(l, SourceColumnIndex))
                .ThenBy(l => _markdownFormatter.GetRowField(l, NameColumnIndex))
                .ToList();

            // Replace the table body in the original lines
            lines.RemoveRange(bodyIndexes.Value.From, tableBodyRowCount);
            lines.InsertRange(bodyIndexes.Value.From, tableBodySorted);

            SaveToFile(licenseFileName, lines);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets package license info from the package info list.
        /// </summary>
        /// <param name="packageInfo">Package info list.</param>
        /// <returns>Instance of <see cref="PackageLicense"/> if <see cref="VerifyLicenseInfo"/> set to <see langword="true"/>. Otherwise <see langword="null"/>.</returns>
        internal protected virtual PackageLicense? GetLicenseInfo(List<string> packageInfo)
        {
            if (!VerifyLicenseInfo)
                return null;

            string? copyright = packageInfo.TryGet(CopyrightColumnIndex)?.Trim() ?? string.Empty;
            string? type = packageInfo.TryGet(LicenseColumnIndex)?.Trim() ?? string.Empty;
            string? url = packageInfo.TryGet(UrlColumnIndex)?.Trim() ?? string.Empty;

            return !string.IsNullOrEmpty(copyright) || !string.IsNullOrEmpty(type) || !string.IsNullOrEmpty(url)
                ? new PackageLicense(copyright, type, url)
                : null;
        }

        /// <summary>
        /// Gets a value indicating whether the row is related to the given package reference.
        /// </summary>
        /// <param name="fields">Markdown table row fields.</param>
        /// <param name="reference">Package reference.</param>
        /// <returns><see langword="true"/> if matched. Otherwise <see langword="false"/>.</returns>
        internal protected virtual bool IsReferenceRow(List<string> fields, PackageReference reference)
        {
            return
                fields.Count > NameColumnIndex &&
                fields[NameColumnIndex] == reference.Name &&
                fields.Count > SourceColumnIndex &&
                fields[SourceColumnIndex] == reference.Source;
        }

        /// <summary>
        /// Updates the given Markdown table row with the new values for the given package reference.
        /// </summary>
        /// <param name="fields">Markdown table row fields.</param>
        /// <param name="reference">Package reference.</param>
        internal protected virtual void UpdateReferenceRow(List<string> fields, PackageReference reference)
        {
            fields[VersionColumnIndex] = reference.Version;

            // Update license info only if new info is available, but it empty strings provided, update the license to signalize license change
            if (VerifyLicenseInfo && reference.License != null)
            {
                if (CopyrightColumnIndex.HasValue)
                    fields[CopyrightColumnIndex.Value] = reference.License.CopyrightOrAuthors;

                if (LicenseColumnIndex.HasValue)
                    fields[LicenseColumnIndex.Value] = reference.License.Type;

                if (UrlColumnIndex.HasValue)
                    fields[UrlColumnIndex.Value] = reference.License.RepositoryUrl;
            }
        }

        /// <summary>
        /// Creates new Markdown table row string based on the values for the given package reference.
        /// </summary>
        /// <param name="reference">Package reference.</param>
        internal protected virtual string CreateReferenceRow(PackageReference reference)
        {
            List<string> fields = new List<string>(new int[] {
                NameColumnIndex,
                VersionColumnIndex,
                SourceColumnIndex,
                CopyrightColumnIndex ?? -1,
                LicenseColumnIndex ?? -1,
                UrlColumnIndex ?? -1
            }.Max() + 1);

            fields.Insert(NameColumnIndex, reference.Name);
            fields.Insert(VersionColumnIndex, reference.Version);
            fields.Insert(SourceColumnIndex, reference.Source);

            if (VerifyLicenseInfo && reference.License != null)
            {
                if (CopyrightColumnIndex.HasValue)
                    fields.Insert(CopyrightColumnIndex.Value, reference.License.CopyrightOrAuthors);

                if (LicenseColumnIndex.HasValue)
                    fields.Insert(LicenseColumnIndex.Value, reference.License.Type);

                if (UrlColumnIndex.HasValue)
                    fields.Insert(UrlColumnIndex.Value, reference.License.RepositoryUrl);
            }

            return _markdownFormatter.GetRowString(fields);
        }

        /// <summary>
        /// Saves the given content to the given file.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="lines">Content lines to save to the file.</param>
        internal protected virtual void SaveToFile(string filePath, List<string> lines)
        {
            File.WriteAllText(filePath, string.Join(Environment.NewLine, lines).TrimEnd(), Encoding.UTF8);
        }
    }
}
