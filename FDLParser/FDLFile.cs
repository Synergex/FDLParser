/*
 * Title: FDLFile.cs
 *
 * Description: Represents the complete parsed contents of an OpenVMS FDL file.
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
    /// Represents a complete OpenVMS File Definition Language document.
    /// </summary>
    public sealed class FDLFile
    {
        private readonly List<FDLSection> sections = [];

        internal FDLFile()
        {
        }

        /// <summary>
        /// Gets every primary section in the order in which it occurred in the source document.
        /// This is the lossless attribute tree for all supported FDL sections, including sections that do
        /// not have a specialized model.
        /// </summary>
        public IReadOnlyList<FDLSection> Sections => sections;

        /// <summary>
        /// Gets the TITLE value, when present.
        /// </summary>
        public string? Title => GetSection("TITLE")?.Value;

        /// <summary>
        /// Gets the IDENT value, when present.
        /// </summary>
        public string? Ident => GetSection("IDENT")?.Value;

        /// <summary>
        /// Gets the main FILE section through a typed convenience model, when present.
        /// </summary>
        public FDLFileAttributes? File => GetSection("FILE") is { } section
            ? new FDLFileAttributes(section)
            : null;

        /// <summary>
        /// Gets the RECORD section through a typed convenience model, when present.
        /// </summary>
        public FDLRecordAttributes? Record => GetSection("RECORD") is { } section
            ? new FDLRecordAttributes(section)
            : null;

        /// <summary>
        /// Gets every indexed-file key definition in source order.
        /// </summary>
        public IReadOnlyList<FDLKeyDefinition> Keys => sections
            .Where(section => string.Equals(section.Name, "KEY", StringComparison.OrdinalIgnoreCase))
            .Select(section => new FDLKeyDefinition(section))
            .ToArray();

        /// <summary>
        /// Gets every section with the supplied primary name, case-insensitively.
        /// </summary>
        /// <param name="name">The full FDL primary-section name.</param>
        /// <returns>The matching sections in source order.</returns>
        public IReadOnlyList<FDLSection> GetSections(string name)
        {
            FDLText.ThrowIfNullOrWhiteSpace(name, nameof(name));

            return sections
                .Where(section => string.Equals(section.Name, name, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        /// <summary>
        /// Gets the final occurrence of a section, or <see langword="null"/> if it is absent.
        /// </summary>
        /// <param name="name">The full FDL primary-section name.</param>
        /// <returns>The final matching section, if present.</returns>
        public FDLSection? GetSection(string name)
        {
            FDLText.ThrowIfNullOrWhiteSpace(name, nameof(name));

            return sections.LastOrDefault(section =>
                string.Equals(section.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        internal void AddSection(FDLSection section)
        {
            sections.Add(section);
        }

    }
}
