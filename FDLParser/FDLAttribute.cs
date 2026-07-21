/*
 * Title: Attribute.cs
 *
 * Description: Models source locations, comments, and attributes in an OpenVMS FDL file.
 *
 * Author: Steve Ives, Synergex Professional Services Group
 *
 * Copyright: (c) 2026 Synergex International Corporation, Inc. All rights reserved.
 *
 * Licensed under the BSD 2-Clause License. See LICENSE file for full terms.
 */

using System.Globalization;

namespace FDLParser
{
    /// <summary>
    /// Identifies a position in an FDL source document.
    /// </summary>
    public readonly record struct SourceLocation(int Line, int Column);

    /// <summary>
    /// Represents one secondary FDL attribute and its value.
    /// </summary>
    public sealed class FDLAttribute
    {
        internal FDLAttribute(
            string name,
            string originalName,
            string rawValue,
            SourceLocation location)
        {
            Name = name;
            OriginalName = originalName;
            RawValue = rawValue;
            Location = location;
        }

        /// <summary>
        /// Gets the normalized, uppercase attribute name. Known abbreviated FDL names are expanded.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the attribute name exactly as it appeared in the FDL source, except for surrounding whitespace.
        /// </summary>
        public string OriginalName { get; }

        /// <summary>
        /// Gets the value exactly as it appeared after the attribute name, except for surrounding whitespace.
        /// </summary>
        public string RawValue { get; }

        /// <summary>
        /// Gets the logical value with one matching pair of enclosing quotation marks removed.
        /// Doubled quotation marks within a quoted value are reduced to one quotation mark.
        /// </summary>
        public string Value => FDLText.Unquote(RawValue);

        /// <summary>
        /// Gets the value as a Boolean when it is one of the FDL switch forms YES, TRUE, NO, or FALSE;
        /// otherwise, <see langword="null"/>.
        /// </summary>
        public bool? BooleanValue => FDLText.ToBoolean(Value);

        /// <summary>
        /// Gets the value as a 32-bit integer when it is a decimal integer; otherwise, <see langword="null"/>.
        /// </summary>
        public int? Int32Value => int.TryParse(
            Value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var value)
            ? value
            : null;

        /// <summary>
        /// Gets the value as a 64-bit integer when it is a decimal integer; otherwise, <see langword="null"/>.
        /// </summary>
        public long? Int64Value => long.TryParse(
            Value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var value)
            ? value
            : null;

        /// <summary>
        /// Gets the source location of the attribute name.
        /// </summary>
        public SourceLocation Location { get; }
    }

    /// <summary>
    /// Represents invalid FDL syntax and identifies its location in the source document.
    /// </summary>
    public sealed class FDLParseException : FormatException
    {
        internal FDLParseException(string message, SourceLocation location)
            : base($"{message} (line {location.Line}, column {location.Column}).")
        {
            Location = location;
        }

        /// <summary>
        /// Gets the location at which parsing failed.
        /// </summary>
        public SourceLocation Location { get; }
    }

    internal static class FDLText
    {
        internal static string Unquote(string value)
        {
            if (value.Length < 2 || value[0] is not ('\'' or '"') || value[value.Length - 1] != value[0])
            {
                return value;
            }

            var quote = value[0].ToString();
            return value.Substring(1, value.Length - 2).Replace(quote + quote, quote);
        }

        internal static void ThrowIfNull(string? value, string parameterName)
        {
            if (value is null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        internal static void ThrowIfNullOrWhiteSpace(string? value, string parameterName)
        {
            ThrowIfNull(value, parameterName);

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty or whitespace.", parameterName);
            }
        }

        internal static bool? ToBoolean(string value)
        {
            return value.Trim().ToUpperInvariant() switch
            {
                "YES" or "TRUE" or "Y" or "T" => true,
                "NO" or "FALSE" or "N" or "F" => false,
                _ => null
            };
        }
    }
}
