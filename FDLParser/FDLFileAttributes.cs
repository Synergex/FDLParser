/*
 * Title: FileAttributes.cs
 *
 * Description: Provides typed access to the main FILE section of an OpenVMS FDL file.
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
    /// Identifies an RMS file organization declared by the FILE ORGANIZATION attribute.
    /// </summary>
    public enum FileOrganization
    {
        /// <summary>A sequential RMS file.</summary>
        Sequential,

        /// <summary>A relative RMS file.</summary>
        Relative,

        /// <summary>An indexed RMS file.</summary>
        Indexed
    }

    /// <summary>
    /// Provides convenient typed access to the main FILE section while retaining every original attribute.
    /// </summary>
    public sealed class FDLFileAttributes
    {
        internal FDLFileAttributes(FDLSection section)
        {
            Section = section;
        }

        /// <summary>
        /// Gets the generic FILE-section model, including attributes without a dedicated property.
        /// </summary>
        public FDLSection Section { get; }

        /// <summary>Gets all FILE attributes in source order.</summary>
        public IReadOnlyList<FDLAttribute> Attributes => Section.Attributes;

        /// <summary>Gets the output file specification specified by FILE NAME.</summary>
        public string? Name => Section.GetString("NAME");

        /// <summary>Gets the default file specification specified by FILE DEFAULT_NAME.</summary>
        public string? DefaultName => Section.GetString("DEFAULT_NAME");

        /// <summary>Gets the textual RMS organization value.</summary>
        public string? OrganizationText => Section.GetString("ORGANIZATION");

        /// <summary>Gets the RMS organization when it is SEQUENTIAL, RELATIVE, or INDEXED.</summary>
        public FileOrganization? Organization => OrganizationText?.ToUpperInvariant() switch
        {
            "SEQUENTIAL" => FileOrganization.Sequential,
            "RELATIVE" => FileOrganization.Relative,
            "INDEXED" => FileOrganization.Indexed,
            _ => null
        };

        /// <summary>Gets FILE ALLOCATION, in blocks.</summary>
        public long? Allocation => Section.GetAttribute("ALLOCATION")?.Int64Value;

        /// <summary>Gets FILE BUCKET_SIZE, in blocks.</summary>
        public int? BucketSize => Section.GetInt32("BUCKET_SIZE");

        /// <summary>Gets FILE EXTENSION, in blocks.</summary>
        public int? Extension => Section.GetInt32("EXTENSION");

        /// <summary>Gets FILE GLOBAL_BUFFER_COUNT.</summary>
        public int? GlobalBufferCount => Section.GetInt32("GLOBAL_BUFFER_COUNT");

        /// <summary>Gets FILE MAX_RECORD_NUMBER.</summary>
        public long? MaximumRecordNumber => Section.GetAttribute("MAX_RECORD_NUMBER")?.Int64Value;

        /// <summary>Gets FILE OWNER.</summary>
        public string? Owner => Section.GetString("OWNER");

        /// <summary>Gets FILE PROTECTION.</summary>
        public string? Protection => Section.GetString("PROTECTION");

        /// <summary>Gets FILE BEST_TRY_CONTIGUOUS.</summary>
        public bool? BestTryContiguous => Section.GetBoolean("BEST_TRY_CONTIGUOUS");

        /// <summary>Gets FILE CONTIGUOUS.</summary>
        public bool? Contiguous => Section.GetBoolean("CONTIGUOUS");

        /// <summary>
        /// Gets an arbitrary FILE attribute, including less common run-time and tape attributes.
        /// </summary>
        /// <param name="name">The full FDL attribute name.</param>
        /// <returns>The final matching attribute, if present.</returns>
        public FDLAttribute? GetAttribute(string name) => Section.GetAttribute(name);
    }
}
