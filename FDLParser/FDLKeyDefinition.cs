/*
 * Title: KeyDefinition.cs
 *
 * Description: Models an indexed-file KEY section and its key segments in an OpenVMS FDL file.
 *
 * Author: Steve Ives, Synergex Professional Services Group
 *
 * Copyright: (c) 2026 Synergex International Corporation, Inc. All rights reserved.
 *
 * Licensed under the BSD 2-Clause License. See LICENSE file for full terms.
 */

using System.Globalization;
using System.Text.RegularExpressions;

namespace FDLParser
{
    /// <summary>
    /// Represents one segment of a segmented RMS indexed-file key.
    /// </summary>
    public sealed class FDLKeySegment
    {
        private readonly IReadOnlyList<FDLAttribute> attributes;

        internal FDLKeySegment(int number, IEnumerable<FDLAttribute> attributes)
        {
            Number = number;
            this.attributes = attributes.ToArray();
        }

        /// <summary>Gets the zero-based key-segment number.</summary>
        public int Number { get; }

        /// <summary>Gets the complete SEGn attributes in source order.</summary>
        public IReadOnlyList<FDLAttribute> Attributes => attributes;

        /// <summary>Gets SEGn_POSITION, in bytes from the start of the record.</summary>
        public int? Position => GetAttribute("POSITION")?.Int32Value;

        /// <summary>Gets SEGn_LENGTH, in bytes.</summary>
        public int? Length => GetAttribute("LENGTH")?.Int32Value;

        /// <summary>
        /// Gets a segment attribute by its suffix. For example, <c>GetAttribute("POSITION")</c> returns
        /// the SEGn_POSITION attribute.
        /// </summary>
        /// <param name="suffix">The portion after the SEGn_ prefix.</param>
        /// <returns>The final matching segment attribute, if present.</returns>
        public FDLAttribute? GetAttribute(string suffix)
        {
            FDLText.ThrowIfNullOrWhiteSpace(suffix, nameof(suffix));
            var name = $"SEG{Number}_{suffix}";

            return attributes.LastOrDefault(attribute =>
                string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Represents an RMS indexed-file key described by an FDL KEY section.
    /// </summary>
    public sealed class FDLKeyDefinition
    {
        private static readonly Regex SegmentNamePattern = new(
            "^SEG(?<number>\\d+)_(?<suffix>.+)$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        internal FDLKeyDefinition(FDLSection section)
        {
            Section = section;
            Segments = CreateSegments(section.Attributes);
        }

        /// <summary>Gets the generic KEY-section model.</summary>
        public FDLSection Section { get; }

        /// <summary>Gets the key number from the KEY section header.</summary>
        public int? Number => int.TryParse(
            Section.Value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var value)
            ? value
            : null;

        /// <summary>Gets all non-segment KEY attributes in source order.</summary>
        public IReadOnlyList<FDLAttribute> Attributes => Section.Attributes
            .Where(attribute => !SegmentNamePattern.IsMatch(attribute.Name))
            .ToArray();

        /// <summary>Gets all key segments in ascending segment-number order.</summary>
        public IReadOnlyList<FDLKeySegment> Segments { get; }

        /// <summary>Gets KEY NAME.</summary>
        public string? Name => Section.GetString("NAME");

        /// <summary>Gets KEY TYPE.</summary>
        public string? Type => Section.GetString("TYPE");

        /// <summary>Gets KEY POSITION for an unsegmented key.</summary>
        public int? Position => Section.GetInt32("POSITION");

        /// <summary>Gets KEY LENGTH for an unsegmented key.</summary>
        public int? Length => Section.GetInt32("LENGTH");

        /// <summary>Gets KEY DUPLICATES.</summary>
        public bool? Duplicates => Section.GetBoolean("DUPLICATES");

        /// <summary>Gets KEY CHANGES.</summary>
        public bool? Changes => Section.GetBoolean("CHANGES");

        /// <summary>Gets KEY NULL_KEY.</summary>
        public bool? NullKey => Section.GetBoolean("NULL_KEY");

        /// <summary>Gets KEY NULL_VALUE.</summary>
        public string? NullValue => Section.GetString("NULL_VALUE");

        /// <summary>Gets KEY COLLATING_SEQUENCE.</summary>
        public string? CollatingSequence => Section.GetString("COLLATING_SEQUENCE");

        /// <summary>Gets KEY DATA_AREA.</summary>
        public int? DataArea => Section.GetInt32("DATA_AREA");

        /// <summary>Gets KEY LEVEL1_INDEX_AREA.</summary>
        public int? Level1IndexArea => Section.GetInt32("LEVEL1_INDEX_AREA");

        /// <summary>Gets KEY INDEX_AREA.</summary>
        public int? IndexArea => Section.GetInt32("INDEX_AREA");

        /// <summary>Gets KEY DATA_FILL.</summary>
        public int? DataFill => Section.GetInt32("DATA_FILL");

        /// <summary>Gets KEY INDEX_FILL.</summary>
        public int? IndexFill => Section.GetInt32("INDEX_FILL");

        /// <summary>Gets KEY PROLOG.</summary>
        public int? Prolog => Section.GetInt32("PROLOG");

        /// <summary>Gets KEY DATA_KEY_COMPRESSION.</summary>
        public bool? DataKeyCompression => Section.GetBoolean("DATA_KEY_COMPRESSION");

        /// <summary>Gets KEY DATA_RECORD_COMPRESSION.</summary>
        public bool? DataRecordCompression => Section.GetBoolean("DATA_RECORD_COMPRESSION");

        /// <summary>Gets KEY INDEX_COMPRESSION.</summary>
        public bool? IndexCompression => Section.GetBoolean("INDEX_COMPRESSION");

        /// <summary>Gets an arbitrary KEY attribute, including attributes not exposed above.</summary>
        /// <param name="name">The full FDL attribute name.</param>
        /// <returns>The final matching attribute, if present.</returns>
        public FDLAttribute? GetAttribute(string name) => Section.GetAttribute(name);

        private static IReadOnlyList<FDLKeySegment> CreateSegments(IEnumerable<FDLAttribute> attributes)
        {
            return attributes
                .Select(attribute => new { Attribute = attribute, Match = SegmentNamePattern.Match(attribute.Name) })
                .Where(item => item.Match.Success
                    && int.TryParse(
                        item.Match.Groups["number"].Value,
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out _))
                .GroupBy(item => int.Parse(
                    item.Match.Groups["number"].Value,
                    CultureInfo.InvariantCulture))
                .OrderBy(group => group.Key)
                .Select(group => new FDLKeySegment(group.Key, group.Select(item => item.Attribute)))
                .ToArray();
        }
    }
}
