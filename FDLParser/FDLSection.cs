/*
 * Title: FDLSection.cs
 *
 * Description: Models a primary section in an OpenVMS FDL file.
 *
 * Author: Steve Ives, Synergex Professional Services Group
 *
 * Copyright: (c) 2026 Synergex International Corporation, Inc. All rights reserved.
 *
 * Licensed under the BSD 2-Clause License. See LICENSE file for full terms.
 */

namespace FDLParser
{
    /// <summary>
    /// Represents one primary FDL section and all of its secondary attributes.
    /// </summary>
    public sealed class FDLSection
    {
        private readonly List<FDLAttribute> attributes = [];

        internal FDLSection(
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
        /// Gets the normalized, uppercase primary-section name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the primary-section name as written in the source.
        /// </summary>
        public string OriginalName { get; }

        /// <summary>
        /// Gets the section value as written in the source. KEY and AREA values identify their number;
        /// TITLE and IDENT values contain their descriptive text.
        /// </summary>
        public string RawValue { get; private set; }

        /// <summary>
        /// Gets the logical section value with enclosing quotation marks removed.
        /// </summary>
        public string Value => FDLText.Unquote(RawValue);

        /// <summary>
        /// Gets all secondary attributes in source order.
        /// </summary>
        public IReadOnlyList<FDLAttribute> Attributes => attributes;

        /// <summary>
        /// Gets the source location of the section name.
        /// </summary>
        public SourceLocation Location { get; }

        /// <summary>
        /// Gets every attribute matching the supplied name, case-insensitively.
        /// </summary>
        /// <param name="name">The full or normalized FDL attribute name.</param>
        /// <returns>All matching attributes in source order.</returns>
        public IReadOnlyList<FDLAttribute> GetAttributes(string name)
        {
            FDLText.ThrowIfNullOrWhiteSpace(name, nameof(name));

            return attributes
                .Where(attribute => string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        /// <summary>
        /// Gets the final occurrence of an attribute, or <see langword="null"/> when it is absent.
        /// </summary>
        /// <param name="name">The full or normalized FDL attribute name.</param>
        /// <returns>The final matching attribute, if present.</returns>
        public FDLAttribute? GetAttribute(string name)
        {
            FDLText.ThrowIfNullOrWhiteSpace(name, nameof(name));

            return attributes.LastOrDefault(attribute =>
                string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets an attribute's logical string value, or <see langword="null"/> when it is absent.
        /// </summary>
        public string? GetString(string name) => GetAttribute(name)?.Value;

        /// <summary>
        /// Gets an attribute's decimal integer value, or <see langword="null"/> when it is absent or not numeric.
        /// </summary>
        public int? GetInt32(string name) => GetAttribute(name)?.Int32Value;

        /// <summary>
        /// Gets an attribute's FDL switch value, or <see langword="null"/> when it is absent or not a switch value.
        /// </summary>
        public bool? GetBoolean(string name) => GetAttribute(name)?.BooleanValue;

        internal void SetRawValue(string rawValue)
        {
            RawValue = rawValue;
        }

        internal void AddAttribute(FDLAttribute attribute)
        {
            attributes.Add(attribute);
        }
    }
}
