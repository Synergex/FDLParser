/*
 * Title: FDLFileParser.cs
 *
 * Description: Parses OpenVMS File Definition Language (FDL) files into .NET object models.
 *
 * Author: Steve Ives, Synergex Professional Services Group
 *
 * Copyright: (c) 2026 Synergex International Corporation, Inc. All rights reserved.
 *
 * Licensed under the BSD 2-Clause License. See LICENSE file for full terms.
 */

using System.Text;
using System.Text.RegularExpressions;

namespace FDLParser
{
    /// <summary>
    /// Parses an OpenVMS File Definition Language (FDL) document.
    /// </summary>
    /// <remarks>
    /// The parser retains every section and secondary attribute in the returned document. It also offers
    /// typed convenience models for the FILE, RECORD, and KEY sections. FDL keywords are treated as
    /// case-insensitive, and unique abbreviations for the FILE, RECORD, and KEY attributes are expanded.
    /// </remarks>
    public sealed class FDLFileParser
    {
        private static readonly string[] PrimarySectionNames =
        [
            "TITLE",
            "IDENT",
            "SYSTEM",
            "FILE",
            "DATE",
            "RECORD",
            "ACCESS",
            "NETWORK",
            "SHARING",
            "CONNECT",
            "AREA",
            "KEY",
            "JOURNAL",
            "ANALYSIS_OF_AREA",
            "ANALYSIS_OF_KEY"
        ];

        private static readonly IReadOnlyDictionary<string, string[]> AttributeNamesBySection =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["FILE"] =
                [
                    "ALLOCATION", "ASYNCHRONOUS", "BEST_TRY_CONTIGUOUS", "BUCKET_SIZE",
                    "CLUSTER_SIZE", "CONTEXT", "CONTIGUOUS", "CREATE_IF", "DEFAULT_NAME",
                    "DEFERRED_WRITE", "DELETE_ON_CLOSE", "DIRECTORY_ENTRY", "EXTENSION",
                    "FILE_MONITORING", "GLOBAL_BUFFER_COUNT", "MAX_RECORD_NUMBER",
                    "MAXIMIZE_VERSION", "MT_BLOCK_SIZE", "MT_CLOSE_REWIND", "MT_CURRENT_POSITION",
                    "MT_NOT_EOF", "MT_OPEN_REWIND", "MT_PROTECTION", "NAME",
                    "NON_FILE_STRUCTURED", "ORGANIZATION", "OUTPUT_FILE_PARSE", "OWNER",
                    "PRINT_ON_CLOSE", "PROTECTION", "READ_CHECK", "REVISION", "SEQUENTIAL_ONLY",
                    "STORED_SEMANTICS", "SUBMIT_ON_CLOSE", "SUPERSEDE", "TEMPORARY",
                    "TRUNCATE_ON_CLOSE", "USER_FILE_OPEN", "WINDOW_SIZE", "WRITE_CHECK"
                ],
                ["RECORD"] =
                ["BLOCK_SPAN", "CARRIAGE_CONTROL", "CONTROL_FIELD", "FORMAT", "SIZE"],
                ["KEY"] =
                [
                    "CHANGES", "COLLATING_SEQUENCE", "DATA_AREA", "DATA_FILL",
                    "DATA_KEY_COMPRESSION", "DATA_RECORD_COMPRESSION", "DUPLICATES", "INDEX_AREA",
                    "INDEX_COMPRESSION", "INDEX_FILL", "LENGTH", "LEVEL1_INDEX_AREA", "NAME",
                    "NULL_KEY", "NULL_VALUE", "POSITION", "PROLOG", "TYPE"
                ]
            };

        private static readonly string[] KeySegmentAttributeSuffixes = ["LENGTH", "POSITION"];

        private static readonly Regex KeySegmentNamePattern = new(
            "^SEG(?<number>\\d+)_(?<suffix>.+)$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Parses the complete text of an FDL document.
        /// </summary>
        /// <param name="fdl">The FDL text to parse.</param>
        /// <returns>The complete object tree for the FDL document.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fdl"/> is <see langword="null"/>.</exception>
        /// <exception cref="FDLParseException">The text has malformed quoting or an attribute without a section.</exception>
        public FDLFile Parse(string fdl)
        {
            FDLText.ThrowIfNull(fdl, nameof(fdl));

            var document = new FDLFile();
            FDLSection? currentSection = null;

            foreach (var statement in ReadStatements(fdl))
            {
                ProcessStatement(statement, document, ref currentSection);
            }

            return document;
        }

        /// <summary>
        /// Reads and parses an FDL file using the platform's default text detection.
        /// </summary>
        /// <param name="path">The path to the FDL file.</param>
        /// <returns>The complete object tree for the FDL document.</returns>
        public FDLFile ParseFile(string path)
        {
            FDLText.ThrowIfNullOrWhiteSpace(path, nameof(path));
            return Parse(File.ReadAllText(path));
        }

        private static IEnumerable<Statement> ReadStatements(string source)
        {
            // The lexer regards CRLF, CR, and LF as an FDL source-line terminator.
            source = source.Replace("\r\n", "\n").Replace('\r', '\n');

            var statement = new StringBuilder();
            var line = 1;
            var column = 1;
            var statementLocation = default(SourceLocation?);
            var quoteLocation = default(SourceLocation?);
            char? quoteCharacter = null;
            var readingComment = false;

            for (var index = 0; index < source.Length; index++)
            {
                var character = source[index];

                if (readingComment)
                {
                    if (character == ';')
                    {
                        readingComment = false;
                        column++;
                    }
                    else if (character == '\n')
                    {
                        readingComment = false;
                        line++;
                        column = 1;
                    }
                    else
                    {
                        column++;
                    }

                    continue;
                }

                if (quoteCharacter is not null)
                {
                    statement.Append(character);

                    if (character == quoteCharacter)
                    {
                        if (index + 1 < source.Length && source[index + 1] == quoteCharacter)
                        {
                            statement.Append(source[++index]);
                            column += 2;
                            continue;
                        }

                        quoteCharacter = null;
                        quoteLocation = null;
                    }

                    if (character == '\n')
                    {
                        line++;
                        column = 1;
                    }
                    else
                    {
                        column++;
                    }

                    continue;
                }

                switch (character)
                {
                    case '\'':
                    case '"':
                        StartStatementIfNeeded(statement, ref statementLocation, character, line, column);
                        statement.Append(character);
                        quoteCharacter = character;
                        quoteLocation = new SourceLocation(line, column);
                        column++;
                        break;

                    case '!':
                        var commentPrecedingStatement = CompleteStatement(statement, ref statementLocation);
                        if (commentPrecedingStatement is not null)
                        {
                            yield return commentPrecedingStatement.Value;
                        }

                        readingComment = true;
                        column++;
                        break;

                    case ';':
                        var semicolonPrecedingStatement = CompleteStatement(statement, ref statementLocation);
                        if (semicolonPrecedingStatement is not null)
                        {
                            yield return semicolonPrecedingStatement.Value;
                        }

                        column++;
                        break;

                    case '\n':
                        var linePrecedingStatement = CompleteStatement(statement, ref statementLocation);
                        if (linePrecedingStatement is not null)
                        {
                            yield return linePrecedingStatement.Value;
                        }

                        line++;
                        column = 1;
                        break;

                    default:
                        StartStatementIfNeeded(statement, ref statementLocation, character, line, column);
                        statement.Append(character);
                        column++;
                        break;
                }
            }

            if (quoteCharacter is not null)
            {
                throw new FDLParseException("An FDL quoted string is not terminated", quoteLocation!.Value);
            }

            var finalStatement = CompleteStatement(statement, ref statementLocation);
            if (finalStatement is not null)
            {
                yield return finalStatement.Value;
            }
        }

        private static void ProcessStatement(
            Statement statement,
            FDLFile document,
            ref FDLSection? currentSection)
        {
            var text = statement.Text;

            if (text[0] is '\'' or '"')
            {
                if (currentSection is not null
                    && currentSection.Name is "TITLE" or "IDENT"
                    && string.IsNullOrEmpty(currentSection.RawValue))
                {
                    currentSection.SetRawValue(text);
                    return;
                }

                throw new FDLParseException("An FDL statement cannot begin with a string value", statement.Location);
            }

            var (originalName, rawValue) = SplitNameAndValue(text);
            var primaryName = GetPrimarySectionName(originalName, currentSection);

            if (primaryName is not null)
            {
                currentSection = new FDLSection(
                    primaryName,
                    originalName,
                    rawValue,
                    statement.Location);
                document.AddSection(currentSection);
                return;
            }

            if (currentSection is null)
            {
                throw new FDLParseException(
                    $"The FDL attribute '{originalName}' does not belong to a primary section",
                    statement.Location);
            }

            var attributeName = GetAttributeName(currentSection.Name, originalName);
            currentSection.AddAttribute(new FDLAttribute(
                attributeName,
                originalName,
                rawValue,
                statement.Location));
        }

        private static string? GetPrimarySectionName(string name, FDLSection? currentSection)
        {
            var normalizedName = name.ToUpperInvariant();

            if (PrimarySectionNames.Contains(normalizedName, StringComparer.Ordinal))
            {
                return normalizedName;
            }

            var abbreviatedPrimaryName = FindUniqueKeyword(normalizedName, PrimarySectionNames);
            if (abbreviatedPrimaryName is null)
            {
                return null;
            }

            // An abbreviated word inside a primary section is an attribute whenever it is a unique attribute
            // abbreviation in that context. This avoids treating FILE CONTEXT as a CONNECT section header.
            return currentSection is null || GetAttributeName(currentSection.Name, name) == normalizedName
                ? abbreviatedPrimaryName
                : null;
        }

        private static string GetAttributeName(string sectionName, string name)
        {
            var normalizedName = name.ToUpperInvariant();

            if (string.Equals(sectionName, "KEY", StringComparison.OrdinalIgnoreCase))
            {
                var segmentMatch = KeySegmentNamePattern.Match(normalizedName);
                if (segmentMatch.Success)
                {
                    var suffix = FindUniqueKeyword(
                        segmentMatch.Groups["suffix"].Value,
                        KeySegmentAttributeSuffixes);
                    return suffix is null
                        ? normalizedName
                        : $"SEG{segmentMatch.Groups["number"].Value}_{suffix}";
                }
            }

            return AttributeNamesBySection.TryGetValue(sectionName, out var names)
                ? FindUniqueKeyword(normalizedName, names) ?? normalizedName
                : normalizedName;
        }

        private static string? FindUniqueKeyword(string abbreviation, IEnumerable<string> keywords)
        {
            var matches = keywords
                .Where(keyword => keyword.StartsWith(abbreviation, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToArray();

            return matches.Length == 1 ? matches[0] : null;
        }

        private static (string Name, string RawValue) SplitNameAndValue(string statement)
        {
            var separatorIndex = 0;
            while (separatorIndex < statement.Length && !char.IsWhiteSpace(statement[separatorIndex]))
            {
                separatorIndex++;
            }

            var name = statement.Substring(0, separatorIndex);
            var rawValue = separatorIndex == statement.Length
                ? string.Empty
                : statement.Substring(separatorIndex).Trim();
            return (name, rawValue);
        }

        private static void StartStatementIfNeeded(
            StringBuilder statement,
            ref SourceLocation? statementLocation,
            char character,
            int line,
            int column)
        {
            if (statementLocation is null && !char.IsWhiteSpace(character))
            {
                statementLocation = new SourceLocation(line, column);
            }
        }

        private static Statement? CompleteStatement(
            StringBuilder statement,
            ref SourceLocation? statementLocation)
        {
            var text = statement.ToString().Trim();
            var location = statementLocation;
            statement.Clear();
            statementLocation = null;

            if (text.Length > 0)
            {
                return new Statement(text, location ?? new SourceLocation(1, 1));
            }

            return null;
        }

        private readonly record struct Statement(string Text, SourceLocation Location);
    }
}
