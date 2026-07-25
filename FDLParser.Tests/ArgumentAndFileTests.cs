namespace FDLParser.Tests;

[TestClass]
public sealed class ArgumentAndFileTests
{
    private readonly FDLFileParser parser = new();

    [TestMethod]
    public void Parse_NullText_ThrowsArgumentNullException()
    {
        var exception = Assert.ThrowsExactly<ArgumentNullException>(() => parser.Parse(null!));

        Assert.AreEqual("fdl", exception.ParamName);
    }

    [TestMethod]
    public void ParseFile_NullPath_ThrowsArgumentNullException()
    {
        var exception = Assert.ThrowsExactly<ArgumentNullException>(() => parser.ParseFile(null!));

        Assert.AreEqual("path", exception.ParamName);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t\r\n")]
    public void ParseFile_EmptyOrWhitespacePath_ThrowsArgumentException(string path)
    {
        var exception = Assert.ThrowsExactly<ArgumentException>(() => parser.ParseFile(path));

        Assert.AreEqual("path", exception.ParamName);
    }

    [TestMethod]
    public void ParseFile_ExistingFile_ParsesItsCompleteContents()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fdl-parser-{Guid.NewGuid():N}.fdl");

        try
        {
            File.WriteAllText(
                path,
                "TITLE \"From disk\"\nFILE\nNAME disk.dat\nRECORD\nFORMAT FIXED\nSIZE 80");

            var document = parser.ParseFile(path);

            Assert.AreEqual("From disk", document.Title);
            Assert.AreEqual("disk.dat", document.File!.Name);
            Assert.AreEqual(FDLRecordFormat.Fixed, document.Record!.Format);
            Assert.AreEqual(80, document.Record.Size);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [TestMethod]
    public void ParseFile_MissingFile_PropagatesFileNotFoundException()
    {
        var path = Path.Combine(Path.GetTempPath(), $"missing-fdl-{Guid.NewGuid():N}.fdl");

        Assert.ThrowsExactly<FileNotFoundException>(() => parser.ParseFile(path));
    }

    [TestMethod]
    public void DocumentLookupMethods_RejectNullEmptyAndWhitespaceNames()
    {
        var document = parser.Parse("FILE");

        AssertInvalidName(() => document.GetSection(null!));
        AssertInvalidName(() => document.GetSection(string.Empty));
        AssertInvalidName(() => document.GetSection(" "));
        AssertInvalidName(() => document.GetSections(null!));
        AssertInvalidName(() => document.GetSections(string.Empty));
        AssertInvalidName(() => document.GetSections("\t"));
    }

    [TestMethod]
    public void SectionLookupMethods_RejectNullEmptyAndWhitespaceNames()
    {
        var section = parser.Parse("FILE").File!.Section;

        AssertInvalidName(() => section.GetAttribute(null!));
        AssertInvalidName(() => section.GetAttribute(string.Empty));
        AssertInvalidName(() => section.GetAttribute(" "));
        AssertInvalidName(() => section.GetAttributes(null!));
        AssertInvalidName(() => section.GetAttributes(string.Empty));
        AssertInvalidName(() => section.GetAttributes("\r\n"));
        AssertInvalidName(() => section.GetString(null!));
        AssertInvalidName(() => section.GetInt32(string.Empty));
        AssertInvalidName(() => section.GetBoolean(" "));
    }

    [TestMethod]
    public void TypedModelLookupMethods_RejectInvalidNames()
    {
        var document = parser.Parse(
            "FILE\nNAME x\nRECORD\nSIZE 1\nKEY 0\nSEG0_POSITION 0");

        AssertInvalidName(() => document.File!.GetAttribute(null!));
        AssertInvalidName(() => document.File!.GetAttribute(string.Empty));
        AssertInvalidName(() => document.Record!.GetAttribute(" "));
        AssertInvalidName(() => document.Keys.Single().GetAttribute(null!));
        AssertInvalidName(
            () => document.Keys.Single().Segments.Single().GetAttribute("\t"),
            "suffix");
    }

    private static void AssertInvalidName(Action action, string expectedParameterName = "name")
    {
        try
        {
            action();
            Assert.Fail("Expected an argument exception.");
        }
        catch (ArgumentException exception)
        {
            Assert.AreEqual(expectedParameterName, exception.ParamName);
        }
    }
}
