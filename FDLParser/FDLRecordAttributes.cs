/*
 * Title: RecordAttributes.cs
 *
 * Description: Provides typed access to the RECORD section of an OpenVMS FDL file.
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
    /// Identifies an RMS record format declared by the RECORD FORMAT attribute.
    /// </summary>
    public enum FDLRecordFormat
    {
        /// <summary>Fixed-length records.</summary>
        Fixed,

        /// <summary>Variable-length records.</summary>
        Variable,

        /// <summary>Variable-length records with a fixed control field.</summary>
        Vfc,

        /// <summary>Stream records.</summary>
        Stream,

        /// <summary>Stream records delimited by carriage return.</summary>
        StreamCr,

        /// <summary>Stream records delimited by line feed.</summary>
        StreamLf,

        /// <summary>A sequential byte stream with no record terminator.</summary>
        Undefined
    }

    /// <summary>
    /// Provides convenient typed access to a RECORD section while retaining every original attribute.
    /// </summary>
    public sealed class FDLRecordAttributes
    {
        internal FDLRecordAttributes(FDLSection section)
        {
            Section = section;
        }

        /// <summary>Gets the generic RECORD-section model.</summary>
        public FDLSection Section { get; }

        /// <summary>Gets all RECORD attributes in source order.</summary>
        public IReadOnlyList<FDLAttribute> Attributes => Section.Attributes;

        /// <summary>Gets RECORD BLOCK_SPAN.</summary>
        public bool? BlockSpan => Section.GetBoolean("BLOCK_SPAN");

        /// <summary>Gets the textual RECORD CARRIAGE_CONTROL value.</summary>
        public string? CarriageControl => Section.GetString("CARRIAGE_CONTROL");

        /// <summary>Gets RECORD CONTROL_FIELD, in bytes.</summary>
        public int? ControlFieldSize => Section.GetInt32("CONTROL_FIELD");

        /// <summary>Gets the textual RECORD FORMAT value.</summary>
        public string? FormatText => Section.GetString("FORMAT");

        /// <summary>Gets the record format when it is one of the defined RMS record formats.</summary>
        public FDLRecordFormat? Format => FormatText?.ToUpperInvariant() switch
        {
            "FIXED" => FDLRecordFormat.Fixed,
            "VARIABLE" => FDLRecordFormat.Variable,
            "VFC" => FDLRecordFormat.Vfc,
            "STREAM" => FDLRecordFormat.Stream,
            "STREAM_CR" => FDLRecordFormat.StreamCr,
            "STREAM_LF" => FDLRecordFormat.StreamLf,
            "UNDEFINED" => FDLRecordFormat.Undefined,
            _ => null
        };

        /// <summary>Gets RECORD SIZE, in bytes.</summary>
        public int? Size => Section.GetInt32("SIZE");

        /// <summary>Gets an arbitrary RECORD attribute.</summary>
        /// <param name="name">The full FDL attribute name.</param>
        /// <returns>The final matching attribute, if present.</returns>
        public FDLAttribute? GetAttribute(string name) => Section.GetAttribute(name);
    }
}
