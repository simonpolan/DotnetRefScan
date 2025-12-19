using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Formats Markdown tables in a Markdown document so that
/// all columns have uniform widths, while leaving non-table
/// content unchanged.
/// </summary>
public interface IMarkdownFormatter
{
    /// <summary>
    /// Formats an entire Markdown document, modifying only GitHub-style tables.
    /// </summary>
    /// <param name="filePath">Markdown document file path.</param>
    Task FormatTable(string filePath);

    /// <summary>
    /// Gets Markdown document lines.
    /// </summary>
    /// <param name="filePath">Markdown document file path.</param>
    /// <returns>List of lines.</returns>
    List<string> GetLines(string filePath);

    /// <summary>
    /// Gets Markdown row string (unformatted) constructed from the given fields.
    /// </summary>
    /// <param name="fields">List of row fields.</param>
    /// <returns>Markdown table row string (unformatted).</returns>
    string GetRowString(List<string> fields);

    /// <summary>
    /// Parses a Markdown table row into individual cell values.
    /// </summary>
    /// <param name="row">The table row line.</param>
    /// <returns>A list of trimmed cell values.</returns>
    List<string> GetRowFields(string row);

    /// <summary>
    /// Get a specific row field.
    /// </summary>
    /// <param name="row">Row string (line).</param>
    /// <param name="index">Field index to get.</param>
    /// <returns>Field value or empty string if not found.</returns>
    string GetRowField(string row, int index);

    /// <summary>
    /// Gets From and To indexes indicating the table body indexes in the given list of Markdown document lines.
    /// </summary>
    /// <param name="lines">Markdown document lines</param>
    /// <returns>From and To (exclusive) indexes of the Markdown table body if found. Otherwise <see langword="null"/>.</returns>
    (int From, int To)? GetTableBodyIndexes(List<string> lines);
}