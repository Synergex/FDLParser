namespace FDLParser.Tests;

[TestClass]
public sealed class AttributeAndSectionModelTests
{
    private readonly FDLFileParser parser = new();

    [TestMethod]
    public void Attribute_ExposesNormalizedOriginalRawLogicalAndLocationValues()
    {
        var attribute = parser.Parse("\n FILE\n   custom_Name   \"some value\"")
            .File!
            .Attributes
            .Single();

        Assert.AreEqual("CUSTOM_NAME", attribute.Name);
        Assert.AreEqual("custom_Name", attribute.OriginalName);
        Assert.AreEqual("\"some value\"", attribute.RawValue);
        Assert.AreEqual("some value", attribute.Value);
        Assert.AreEqual(new SourceLocation(3, 4), attribute.Location);
    }

    [TestMethod]
    [DataRow("YES", true)]
    [DataRow("TRUE", true)]
    [DataRow("Y", true)]
    [DataRow("T", true)]
    [DataRow("yes", true)]
    [DataRow("TrUe", true)]
    [DataRow("\" yes \"", true)]
    [DataRow("NO", false)]
    [DataRow("FALSE", false)]
    [DataRow("N", false)]
    [DataRow("F", false)]
    [DataRow("no", false)]
    [DataRow("FaLsE", false)]
    [DataRow("' false '", false)]
    public void Attribute_BooleanValue_RecognizesAllSupportedSwitchForms(
        string sourceValue,
        bool expected)
    {
        var attribute = ParseSingleAttribute(sourceValue);

        Assert.AreEqual(expected, attribute.BooleanValue);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("1")]
    [DataRow("ON")]
    [DataRow("OFF")]
    [DataRow("MAYBE")]
    [DataRow("\"not true\"")]
    public void Attribute_BooleanValue_ReturnsNullForUnsupportedValues(string sourceValue)
    {
        var attribute = ParseSingleAttribute(sourceValue);

        Assert.IsNull(attribute.BooleanValue);
    }

    [TestMethod]
    [DataRow("0", 0)]
    [DataRow("+42", 42)]
    [DataRow("-42", -42)]
    [DataRow("2147483647", int.MaxValue)]
    [DataRow("-2147483648", int.MinValue)]
    [DataRow("\" 123 \"", 123)]
    public void Attribute_Int32Value_ParsesInvariantDecimalIntegers(string sourceValue, int expected)
    {
        var attribute = ParseSingleAttribute(sourceValue);

        Assert.AreEqual(expected, attribute.Int32Value);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("1.0")]
    [DataRow("1,000")]
    [DataRow("0x10")]
    [DataRow("2147483648")]
    [DataRow("-2147483649")]
    [DataRow("not-a-number")]
    public void Attribute_Int32Value_ReturnsNullForInvalidOrOutOfRangeValues(string sourceValue)
    {
        var attribute = ParseSingleAttribute(sourceValue);

        Assert.IsNull(attribute.Int32Value);
    }

    [TestMethod]
    [DataRow("0", 0L)]
    [DataRow("+42", 42L)]
    [DataRow("-42", -42L)]
    [DataRow("9223372036854775807", long.MaxValue)]
    [DataRow("-9223372036854775808", long.MinValue)]
    public void Attribute_Int64Value_ParsesInvariantDecimalIntegers(string sourceValue, long expected)
    {
        var attribute = ParseSingleAttribute(sourceValue);

        Assert.AreEqual(expected, attribute.Int64Value);
    }

    [TestMethod]
    [DataRow("9223372036854775808")]
    [DataRow("-9223372036854775809")]
    [DataRow("10.5")]
    [DataRow("NaN")]
    public void Attribute_Int64Value_ReturnsNullForInvalidOrOutOfRangeValues(string sourceValue)
    {
        var attribute = ParseSingleAttribute(sourceValue);

        Assert.IsNull(attribute.Int64Value);
    }

    [TestMethod]
    public void Section_ExposesHeaderAndAttributesInSourceOrder()
    {
        const string source = """
            AREA 7
                ALLOCATION 100
                BUCKET_SIZE 8
                CUSTOM "value"
            """;

        var section = parser.Parse(source).GetSection("area")!;

        Assert.AreEqual("AREA", section.Name);
        Assert.AreEqual("AREA", section.OriginalName);
        Assert.AreEqual("7", section.RawValue);
        Assert.AreEqual("7", section.Value);
        Assert.AreEqual(new SourceLocation(1, 1), section.Location);
        CollectionAssert.AreEqual(
            new[] { "ALLOCATION", "BUCKET_SIZE", "CUSTOM" },
            section.Attributes.Select(attribute => attribute.Name).ToArray());
    }

    [TestMethod]
    public void Section_LookupsAreCaseInsensitive()
    {
        var section = parser.Parse(
            "FILE\nNAME \"case.dat\"\nALLOCATION 12\nCONTIGUOUS y").File!.Section;

        Assert.AreSame(section.GetAttribute("NAME"), section.GetAttribute("name"));
        Assert.AreEqual("case.dat", section.GetString("nAmE"));
        Assert.AreEqual(12, section.GetInt32("allocation"));
        Assert.AreEqual(true, section.GetBoolean("contiguous"));
    }

    [TestMethod]
    public void Section_DuplicateAttributeLookupReturnsLastAndPluralReturnsAll()
    {
        var section = parser.Parse(
            "FILE\nNAME first.dat\nname second.dat\nNaMe third.dat").File!.Section;

        var names = section.GetAttributes("nAmE");

        Assert.AreEqual(3, names.Count);
        CollectionAssert.AreEqual(
            new[] { "first.dat", "second.dat", "third.dat" },
            names.Select(attribute => attribute.Value).ToArray());
        Assert.AreSame(names[2], section.GetAttribute("NAME"));
        Assert.AreEqual("third.dat", section.GetString("name"));
    }

    [TestMethod]
    public void Section_MissingAttributeLookupsReturnNullOrEmpty()
    {
        var section = parser.Parse("FILE").File!.Section;

        Assert.IsNull(section.GetAttribute("MISSING"));
        Assert.AreEqual(0, section.GetAttributes("MISSING").Count);
        Assert.IsNull(section.GetString("MISSING"));
        Assert.IsNull(section.GetInt32("MISSING"));
        Assert.IsNull(section.GetBoolean("MISSING"));
    }

    [TestMethod]
    public void Document_LookupsAreCaseInsensitiveAndUseLastOccurrence()
    {
        var document = parser.Parse("RECORD\nSIZE 1\nrecord\nSIZE 2");

        var records = document.GetSections("rEcOrD");

        Assert.AreEqual(2, records.Count);
        Assert.AreEqual("1", records[0].GetString("SIZE"));
        Assert.AreEqual("2", records[1].GetString("SIZE"));
        Assert.AreSame(records[1], document.GetSection("RECORD"));
    }

    [TestMethod]
    public void Document_MissingSectionLookupsReturnNullOrEmpty()
    {
        var document = parser.Parse("FILE");

        Assert.IsNull(document.GetSection("RECORD"));
        Assert.AreEqual(0, document.GetSections("RECORD").Count);
        Assert.IsNull(document.Record);
        Assert.AreEqual(0, document.Keys.Count);
    }

    [TestMethod]
    public void SourceLocation_SupportsValueEqualityAndDeconstruction()
    {
        var first = new SourceLocation(12, 34);
        var second = new SourceLocation(12, 34);
        var (line, column) = first;

        Assert.AreEqual(first, second);
        Assert.AreEqual(12, line);
        Assert.AreEqual(34, column);
        Assert.AreEqual("SourceLocation { Line = 12, Column = 34 }", first.ToString());
    }

    private FDLAttribute ParseSingleAttribute(string sourceValue)
    {
        return parser.Parse($"SYSTEM\nVALUE {sourceValue}")
            .GetSection("SYSTEM")!
            .Attributes
            .Single();
    }
}
