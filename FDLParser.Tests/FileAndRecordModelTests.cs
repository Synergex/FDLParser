namespace FDLParser.Tests;

[TestClass]
public sealed class FileAndRecordModelTests
{
    private readonly FDLFileParser parser = new();

    [TestMethod]
    public void FileModel_ExposesAllTypedPropertiesAndUnderlyingSection()
    {
        const string source = """
            FILE
                NAME "CUSTOMER.DAT"
                DEFAULT_NAME "SYS$DISK:[DATA].DAT"
                ORGANIZATION INDEXED
                ALLOCATION 9223372036854775807
                BUCKET_SIZE 63
                EXTENSION 200
                GLOBAL_BUFFER_COUNT 32
                MAX_RECORD_NUMBER 9223372036854775806
                OWNER 'OPERATIONS'
                PROTECTION "(S:RWED,O:RWED,G:RE,W:)"
                BEST_TRY_CONTIGUOUS T
                CONTIGUOUS NO
                CUSTOM_FILE_ATTRIBUTE "retained"
            """;

        var document = parser.Parse(source);
        var file = document.File!;

        Assert.AreSame(document.GetSection("FILE"), file.Section);
        Assert.AreSame(file.Section.Attributes, file.Attributes);
        Assert.AreEqual(13, file.Attributes.Count);
        Assert.AreEqual("CUSTOMER.DAT", file.Name);
        Assert.AreEqual("SYS$DISK:[DATA].DAT", file.DefaultName);
        Assert.AreEqual("INDEXED", file.OrganizationText);
        Assert.AreEqual(FileOrganization.Indexed, file.Organization);
        Assert.AreEqual(long.MaxValue, file.Allocation);
        Assert.AreEqual(63, file.BucketSize);
        Assert.AreEqual(200, file.Extension);
        Assert.AreEqual(32, file.GlobalBufferCount);
        Assert.AreEqual(9223372036854775806L, file.MaximumRecordNumber);
        Assert.AreEqual("OPERATIONS", file.Owner);
        Assert.AreEqual("(S:RWED,O:RWED,G:RE,W:)", file.Protection);
        Assert.AreEqual(true, file.BestTryContiguous);
        Assert.AreEqual(false, file.Contiguous);
        Assert.AreEqual("retained", file.GetAttribute("custom_file_attribute")!.Value);
    }

    [TestMethod]
    [DataRow("SEQUENTIAL", FileOrganization.Sequential)]
    [DataRow("sequential", FileOrganization.Sequential)]
    [DataRow("RELATIVE", FileOrganization.Relative)]
    [DataRow("relative", FileOrganization.Relative)]
    [DataRow("INDEXED", FileOrganization.Indexed)]
    [DataRow("indexed", FileOrganization.Indexed)]
    public void FileModel_OrganizationMapsKnownValues(
        string value,
        FileOrganization expected)
    {
        var file = parser.Parse($"FILE\nORGANIZATION {value}").File!;

        Assert.AreEqual(value, file.OrganizationText);
        Assert.AreEqual(expected, file.Organization);
    }

    [TestMethod]
    public void FileModel_UnknownOrganizationRetainsTextAndReturnsNullEnum()
    {
        var file = parser.Parse("FILE\nORGANIZATION HASHED").File!;

        Assert.AreEqual("HASHED", file.OrganizationText);
        Assert.IsNull(file.Organization);
    }

    [TestMethod]
    public void FileModel_MissingAttributesReturnNull()
    {
        var file = parser.Parse("FILE").File!;

        Assert.IsNull(file.Name);
        Assert.IsNull(file.DefaultName);
        Assert.IsNull(file.OrganizationText);
        Assert.IsNull(file.Organization);
        Assert.IsNull(file.Allocation);
        Assert.IsNull(file.BucketSize);
        Assert.IsNull(file.Extension);
        Assert.IsNull(file.GlobalBufferCount);
        Assert.IsNull(file.MaximumRecordNumber);
        Assert.IsNull(file.Owner);
        Assert.IsNull(file.Protection);
        Assert.IsNull(file.BestTryContiguous);
        Assert.IsNull(file.Contiguous);
    }

    [TestMethod]
    public void FileModel_InvalidTypedValuesReturnNullWithoutLosingAttributes()
    {
        const string source = """
            FILE
                ALLOCATION enormous
                BUCKET_SIZE 1.5
                EXTENSION 2147483648
                GLOBAL_BUFFER_COUNT none
                MAX_RECORD_NUMBER 9223372036854775808
                CONTIGUOUS SOMETIMES
            """;

        var file = parser.Parse(source).File!;

        Assert.IsNull(file.Allocation);
        Assert.IsNull(file.BucketSize);
        Assert.IsNull(file.Extension);
        Assert.IsNull(file.GlobalBufferCount);
        Assert.IsNull(file.MaximumRecordNumber);
        Assert.IsNull(file.Contiguous);
        Assert.AreEqual(6, file.Attributes.Count);
        Assert.AreEqual("enormous", file.GetAttribute("ALLOCATION")!.Value);
    }

    [TestMethod]
    public void FileModel_DuplicatePropertiesUseFinalAttribute()
    {
        var file = parser.Parse(
            "FILE\nALLOCATION 1\nALLOCATION 2\nNAME first\nNAME final").File!;

        Assert.AreEqual(2L, file.Allocation);
        Assert.AreEqual("final", file.Name);
        Assert.AreEqual(2, file.Section.GetAttributes("ALLOCATION").Count);
    }

    [TestMethod]
    public void RecordModel_ExposesAllTypedPropertiesAndUnderlyingSection()
    {
        const string source = """
            RECORD
                BLOCK_SPAN YES
                CARRIAGE_CONTROL CARRIAGE_RETURN
                CONTROL_FIELD 4
                FORMAT VFC
                SIZE 512
                CUSTOM_RECORD_ATTRIBUTE retained
            """;

        var document = parser.Parse(source);
        var record = document.Record!;

        Assert.AreSame(document.GetSection("RECORD"), record.Section);
        Assert.AreSame(record.Section.Attributes, record.Attributes);
        Assert.AreEqual(6, record.Attributes.Count);
        Assert.AreEqual(true, record.BlockSpan);
        Assert.AreEqual("CARRIAGE_RETURN", record.CarriageControl);
        Assert.AreEqual(4, record.ControlFieldSize);
        Assert.AreEqual("VFC", record.FormatText);
        Assert.AreEqual(FDLRecordFormat.Vfc, record.Format);
        Assert.AreEqual(512, record.Size);
        Assert.AreEqual("retained", record.GetAttribute("custom_record_attribute")!.Value);
    }

    [TestMethod]
    [DataRow("FIXED", FDLRecordFormat.Fixed)]
    [DataRow("fixed", FDLRecordFormat.Fixed)]
    [DataRow("VARIABLE", FDLRecordFormat.Variable)]
    [DataRow("variable", FDLRecordFormat.Variable)]
    [DataRow("VFC", FDLRecordFormat.Vfc)]
    [DataRow("vfc", FDLRecordFormat.Vfc)]
    [DataRow("STREAM", FDLRecordFormat.Stream)]
    [DataRow("stream", FDLRecordFormat.Stream)]
    [DataRow("STREAM_CR", FDLRecordFormat.StreamCr)]
    [DataRow("stream_cr", FDLRecordFormat.StreamCr)]
    [DataRow("STREAM_LF", FDLRecordFormat.StreamLf)]
    [DataRow("stream_lf", FDLRecordFormat.StreamLf)]
    [DataRow("UNDEFINED", FDLRecordFormat.Undefined)]
    [DataRow("undefined", FDLRecordFormat.Undefined)]
    public void RecordModel_FormatMapsKnownValues(string value, FDLRecordFormat expected)
    {
        var record = parser.Parse($"RECORD\nFORMAT {value}").Record!;

        Assert.AreEqual(value, record.FormatText);
        Assert.AreEqual(expected, record.Format);
    }

    [TestMethod]
    public void RecordModel_UnknownFormatRetainsTextAndReturnsNullEnum()
    {
        var record = parser.Parse("RECORD\nFORMAT CUSTOM").Record!;

        Assert.AreEqual("CUSTOM", record.FormatText);
        Assert.IsNull(record.Format);
    }

    [TestMethod]
    public void RecordModel_MissingAttributesReturnNull()
    {
        var record = parser.Parse("RECORD").Record!;

        Assert.IsNull(record.BlockSpan);
        Assert.IsNull(record.CarriageControl);
        Assert.IsNull(record.ControlFieldSize);
        Assert.IsNull(record.FormatText);
        Assert.IsNull(record.Format);
        Assert.IsNull(record.Size);
    }

    [TestMethod]
    public void RecordModel_InvalidTypedValuesReturnNullWithoutLosingAttributes()
    {
        var record = parser.Parse(
            "RECORD\nBLOCK_SPAN MAYBE\nCONTROL_FIELD -\nSIZE 2147483648").Record!;

        Assert.IsNull(record.BlockSpan);
        Assert.IsNull(record.ControlFieldSize);
        Assert.IsNull(record.Size);
        Assert.AreEqual(3, record.Attributes.Count);
    }

    [TestMethod]
    public void RecordModel_DuplicatePropertiesUseFinalAttribute()
    {
        var record = parser.Parse(
            "RECORD\nFORMAT FIXED\nFORMAT VARIABLE\nSIZE 10\nSIZE 20").Record!;

        Assert.AreEqual(FDLRecordFormat.Variable, record.Format);
        Assert.AreEqual(20, record.Size);
    }
}
