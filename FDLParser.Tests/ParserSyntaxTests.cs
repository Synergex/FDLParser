namespace FDLParser.Tests;

[TestClass]
public sealed class ParserSyntaxTests
{
    private readonly FDLFileParser parser = new();

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t\r\n  \n")]
    [DataRow(";;;")]
    [DataRow("! comment only")]
    public void Parse_EmptyOrCommentOnlyInput_ReturnsEmptyDocument(string source)
    {
        var document = parser.Parse(source);

        Assert.AreEqual(0, document.Sections.Count);
        Assert.IsNull(document.Title);
        Assert.IsNull(document.Ident);
        Assert.IsNull(document.File);
        Assert.IsNull(document.Record);
        Assert.AreEqual(0, document.Keys.Count);
    }

    [TestMethod]
    public void Parse_CompleteDocument_PreservesSectionsInSourceOrder()
    {
        const string source = """
            TITLE "Customer file"
            IDENT 'V1'
            SYSTEM
                SOURCE VMS
            FILE
                NAME "CUSTOMER.DAT"
            DATE
            RECORD
                FORMAT FIXED
            ACCESS
            NETWORK
            SHARING
            CONNECT
            AREA 0
            KEY 0
            JOURNAL
            ANALYSIS_OF_AREA 0
            ANALYSIS_OF_KEY 0
            """;

        var document = parser.Parse(source);

        CollectionAssert.AreEqual(
            new[]
            {
                "TITLE", "IDENT", "SYSTEM", "FILE", "DATE", "RECORD", "ACCESS", "NETWORK",
                "SHARING", "CONNECT", "AREA", "KEY", "JOURNAL", "ANALYSIS_OF_AREA",
                "ANALYSIS_OF_KEY"
            },
            document.Sections.Select(section => section.Name).ToArray());
        Assert.AreEqual("Customer file", document.Title);
        Assert.AreEqual("V1", document.Ident);
        Assert.AreEqual("VMS", document.GetSection("SYSTEM")!.GetString("SOURCE"));
    }

    [TestMethod]
    public void Parse_TitleAndIdentValuesMayFollowTheirSectionHeaders()
    {
        const string source = """
            TITLE
                "The ""production"" file"
            IDENT
                'Version ''A'''
            """;

        var document = parser.Parse(source);

        Assert.AreEqual("The \"production\" file", document.Title);
        Assert.AreEqual("Version 'A'", document.Ident);
        Assert.AreEqual("\"The \"\"production\"\" file\"", document.GetSection("TITLE")!.RawValue);
        Assert.AreEqual("'Version ''A'''", document.GetSection("IDENT")!.RawValue);
    }

    [TestMethod]
    public void Parse_SemicolonsSeparateStatementsOnOneLine()
    {
        var document = parser.Parse(
            "FILE; NAME \"REPORT.DAT\"; ORGANIZATION SEQUENTIAL; RECORD; FORMAT STREAM_LF;");

        Assert.AreEqual(2, document.Sections.Count);
        Assert.AreEqual("REPORT.DAT", document.File!.Name);
        Assert.AreEqual(FileOrganization.Sequential, document.File.Organization);
        Assert.AreEqual(FDLRecordFormat.StreamLf, document.Record!.Format);
    }

    [TestMethod]
    public void Parse_CommentsEndAtNewlineOrSemicolon()
    {
        const string source = """
            ! leading comment
            FILE ! comment after a section; NAME "A.DAT" ! comment after an attribute
            RECORD; ! another comment; FORMAT FIXED
            """;

        var document = parser.Parse(source);

        Assert.AreEqual("A.DAT", document.File!.Name);
        Assert.AreEqual(FDLRecordFormat.Fixed, document.Record!.Format);
    }

    [TestMethod]
    public void Parse_CommentMarkersAndDelimitersInsideQuotesAreLiteral()
    {
        const string source = """
            TITLE "A ! title; still a title"
            FILE
                NAME "A!B;C.DAT"
                OWNER 'OPS;!TEAM'
            """;

        var document = parser.Parse(source);

        Assert.AreEqual("A ! title; still a title", document.Title);
        Assert.AreEqual("A!B;C.DAT", document.File!.Name);
        Assert.AreEqual("OPS;!TEAM", document.File.Owner);
    }

    [TestMethod]
    public void Parse_DoubledQuotesDoNotTerminateAQuotedValue()
    {
        const string source = """
            FILE
                NAME "A ""quoted;!"" name.DAT"
                OWNER 'D''ANGELO'
            """;

        var document = parser.Parse(source);

        Assert.AreEqual("A \"quoted;!\" name.DAT", document.File!.Name);
        Assert.AreEqual("D'ANGELO", document.File.Owner);
    }

    [TestMethod]
    public void Parse_AllLineEndingStylesAndTabs_TracksLocations()
    {
        const string source = "\r\n  FILE\r    NAME \"x\"\n\tRECORD;   SIZE 12";

        var document = parser.Parse(source);
        var file = document.GetSection("FILE")!;
        var name = file.GetAttribute("NAME")!;
        var record = document.GetSection("RECORD")!;
        var size = record.GetAttribute("SIZE")!;

        Assert.AreEqual(new SourceLocation(2, 3), file.Location);
        Assert.AreEqual(new SourceLocation(3, 5), name.Location);
        Assert.AreEqual(new SourceLocation(4, 2), record.Location);
        Assert.AreEqual(new SourceLocation(4, 12), size.Location);
    }

    [TestMethod]
    public void Parse_LeadingAndTrailingWhitespace_DoesNotAlterRawValues()
    {
        const string source = "  FILE   \n\tNAME     \"A.DAT\"    \n";

        var section = parser.Parse(source).File!.Section;
        var attribute = section.GetAttribute("NAME")!;

        Assert.AreEqual(string.Empty, section.RawValue);
        Assert.AreEqual("\"A.DAT\"", attribute.RawValue);
        Assert.AreEqual("A.DAT", attribute.Value);
    }

    [TestMethod]
    public void Parse_UnknownNameWithinASection_IsRetainedAsAnAttribute()
    {
        var section = parser.Parse("SYSTEM\ncustom_option some value").GetSection("SYSTEM")!;
        var attribute = section.Attributes.Single();

        Assert.AreEqual("CUSTOM_OPTION", attribute.Name);
        Assert.AreEqual("custom_option", attribute.OriginalName);
        Assert.AreEqual("some value", attribute.RawValue);
    }

    [TestMethod]
    public void Parse_DuplicatePrimarySections_AreAllRetained()
    {
        var document = parser.Parse(
            "FILE\nNAME first.dat\nFILE\nNAME second.dat\nFILE\nNAME third.dat");

        Assert.AreEqual(3, document.GetSections("file").Count);
        Assert.AreEqual("third.dat", document.File!.Name);
        Assert.AreEqual("third.dat", document.GetSection("FiLe")!.GetString("name"));
    }

    [TestMethod]
    public void Parse_AttributeBeforeASection_ThrowsAtAttributeLocation()
    {
        var exception = Assert.ThrowsExactly<FDLParseException>(
            () => parser.Parse("\n  NAME value"));

        Assert.AreEqual(new SourceLocation(2, 3), exception.Location);
        StringAssert.Contains(exception.Message, "'NAME'");
        StringAssert.Contains(exception.Message, "line 2, column 3");
    }

    [TestMethod]
    public void Parse_StandaloneStringOutsideTitleOrIdent_Throws()
    {
        var exception = Assert.ThrowsExactly<FDLParseException>(
            () => parser.Parse("FILE\n  \"unexpected\""));

        Assert.AreEqual(new SourceLocation(2, 3), exception.Location);
        StringAssert.Contains(exception.Message, "cannot begin with a string value");
    }

    [TestMethod]
    public void Parse_SecondStringAfterInlineTitle_Throws()
    {
        var exception = Assert.ThrowsExactly<FDLParseException>(
            () => parser.Parse("TITLE \"first\"\n\"second\""));

        Assert.AreEqual(new SourceLocation(2, 1), exception.Location);
    }

    [TestMethod]
    [DataRow("FILE\n NAME \"unterminated", 2, 7)]
    [DataRow("FILE\n OWNER 'unterminated", 2, 8)]
    [DataRow("\n\n\"unterminated", 3, 1)]
    public void Parse_UnterminatedQuote_ThrowsAtOpeningQuote(
        string source,
        int expectedLine,
        int expectedColumn)
    {
        var exception = Assert.ThrowsExactly<FDLParseException>(() => parser.Parse(source));

        Assert.AreEqual(new SourceLocation(expectedLine, expectedColumn), exception.Location);
        StringAssert.Contains(exception.Message, "not terminated");
    }

    [TestMethod]
    public void Parse_QuotesInsideComments_AreIgnored()
    {
        var document = parser.Parse("! \"unterminated comment\nFILE\nNAME ok.dat");

        Assert.AreEqual("ok.dat", document.File!.Name);
    }

    [TestMethod]
    public void ParseException_IsAFormatException()
    {
        var exception = Assert.ThrowsExactly<FDLParseException>(() => parser.Parse("ORPHAN value"));

        Assert.IsInstanceOfType<FormatException>(exception);
    }
}
