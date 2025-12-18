using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Formats Markdown tables in a Markdown document so that
/// all columns have uniform widths, while leaving non-table
/// content unchanged.
/// </summary>
public class MarkdownFormatter : IMarkdownFormatter
{
    /// <inheritdoc/>
    public virtual Task FormatTable(string filePath)
    {
        var lines = GetLines(filePath);
        var output = new StringBuilder();

        int i = 0;
        while (i < lines.Count)
        {
            if (IsTableHeader(lines, i))
            {
                var tableLines = new List<string>();

                while (i < lines.Count && IsTableRow(lines[i]))
                {
                    tableLines.Add(lines[i]);
                    i++;
                }

                output.AppendLine(FormatTable(tableLines));
            }
            else
            {
                output.AppendLine(lines[i]);
                i++;
            }
        }

        SaveToFile(filePath, output.ToString().TrimEnd());
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public virtual List<string> GetLines(string filePath)
    {
        string markdown = File.ReadAllText(filePath, Encoding.UTF8);
        return markdown.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None).ToList();
    }

    /// <inheritdoc/>
    public virtual string GetRowString(List<string> fields)
    {
        return $"|{string.Join("|", fields)}|";
    }

    /// <inheritdoc/>
    public virtual List<string> GetRowFields(string row)
    {
        return row
            .Trim()
            .Trim('|')
            .Split('|')
            .Select(c => c.Trim())
            .ToList();
    }

    /// <inheritdoc/>
    public virtual string GetRowField(string row, int index)
    {
        var fields = GetRowFields(row);
        return fields.Count > index ? fields[index] : string.Empty;
    }

    /// <inheritdoc/>
    public virtual (int From, int To)? GetTableBodyIndexes(List<string> lines)
    {
        int from = -1;
        int to;

        for (to = 0; to < lines.Count; to++)
        {
            if (!IsTableRow(lines[to])) // Not a table row
                if (from < 0) // Table not entered yet
                    continue;
                else
                    break; // Table entered & exited

            if (from < 0)
            {
                // Table header / body delimiter row detected
                if (IsSeparatorRow(lines[to]))
                {
                    from = to + 1;
                }
                continue;
            }

        }

        return from >= 0 ? (from, to) : ((int, int)?)null;
    }

    /// <summary>
    /// Determines whether the current line starts a Markdown table.
    /// A table start is defined as a table row followed by a separator row.
    /// </summary>
    /// <param name="lines">All lines of the Markdown document.</param>
    /// <param name="index">Current line index.</param>
    /// <returns><c>true</c> if the line starts a table; otherwise <c>false</c>.</returns>
    internal protected virtual bool IsTableHeader(List<string> lines, int index)
    {
        if (index + 1 >= lines.Count)
            return false;

        return IsTableRow(lines[index]) && IsSeparatorRow(lines[index + 1]);
    }

    /// <summary>
    /// Determines whether a line represents a Markdown table row.
    /// </summary>
    /// <param name="line">The line to evaluate.</param>
    /// <returns><c>true</c> if the line is a table row; otherwise <c>false</c>.</returns>
    internal protected virtual bool IsTableRow(string line)
    {
        var trimmed = line.Trim();
        return trimmed.StartsWith("|") && trimmed.EndsWith("|");
    }

    /// <summary>
    /// Determines whether a line is a Markdown table separator row.
    /// </summary>
    /// <param name="line">The line to evaluate.</param>
    /// <returns><c>true</c> if the line is a separator row; otherwise <c>false</c>.</returns>
    internal protected virtual bool IsSeparatorRow(string line)
    {
        var cells = GetRowFields(line);
        return !cells.Any(c => c.Any(ch => ch != '-' && ch != ':' && ch != ' '));
    }

    /// <summary>
    /// Formats a single Markdown table so that all columns have equal width.
    /// </summary>
    /// <param name="tableLines">Raw lines belonging to the table.</param>
    /// <returns>The formatted table.</returns>
    internal protected virtual string FormatTable(List<string> tableLines)
    {
        var rows = tableLines.Select(GetRowFields).ToList();
        int columnCount = rows.Max(r => r.Count);

        // Normalize row lengths
        foreach (var row in rows)
        {
            while (row.Count < columnCount)
                row.Add(string.Empty);
        }

        // Calculate column widths (ignore separator row)
        var widths = new int[columnCount];
        for (int c = 0; c < columnCount; c++)
        {
            widths[c] = rows
                .Where(r => !IsSeparatorRow(BuildRawRow(r)))
                .Max(r => r[c].Length);
        }

        var sb = new StringBuilder();

        foreach (var row in rows)
        {
            if (IsSeparatorRow(BuildRawRow(row)))
                sb.AppendLine(BuildSeparatorRow(widths));
            else
                sb.AppendLine(BuildDataRow(row, widths));
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Builds a formatted data row using calculated column widths.
    /// </summary>
    /// <param name="row">Cell values.</param>
    /// <param name="widths">Column widths.</param>
    /// <returns>A formatted Markdown table row.</returns>
    internal protected virtual string BuildDataRow(List<string> row, int[] widths)
    {
        var cells = row.Select((cell, i) =>
            " " + cell.PadRight(widths[i]) + " ");

        return "|" + string.Join("|", cells) + "|";
    }

    /// <summary>
    /// Builds a Markdown separator row using calculated column widths.
    /// </summary>
    /// <param name="widths">Column widths.</param>
    /// <returns>A formatted separator row.</returns>
    internal protected virtual string BuildSeparatorRow(int[] widths)
    {
        var cells = widths.Select(w =>
            " " + new string('-', w) + " ");

        return "|" + string.Join("|", cells) + "|";
    }

    /// <summary>
    /// Builds a raw pipe-delimited row string from parsed cells.
    /// Used for separator row detection.
    /// </summary>
    /// <param name="row">Parsed row cells.</param>
    /// <returns>A raw row string.</returns>
    internal protected virtual string BuildRawRow(List<string> row)
    {
        return "|" + string.Join("|", row) + "|";
    }

    /// <summary>
    /// Saves the given content to the given file.
    /// </summary>
    /// <param name="filePath">File path.</param>
    /// <param name="content">Content to save to the file.</param>
    internal protected virtual void SaveToFile(string filePath, string content)
    {
        File.WriteAllText(filePath, content, Encoding.UTF8);
    }
}
