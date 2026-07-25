namespace FDLParser.Tests;

[TestClass]
public sealed class KeyModelTests
{
    private readonly FDLFileParser parser = new();

    [TestMethod]
    public void KeyModel_ExposesAllTypedPropertiesAndUnderlyingSection()
    {
        const string source = """
            KEY 7
                NAME "CUSTOMER_ID"
                TYPE STRING
                POSITION 0
                LENGTH 10
                DUPLICATES YES
                CHANGES NO
                NULL_KEY TRUE
                NULL_VALUE "0000000000"
                COLLATING_SEQUENCE MULTINATIONAL
                DATA_AREA 1
                LEVEL1_INDEX_AREA 2
                INDEX_AREA 3
                DATA_FILL 80
                INDEX_FILL 70
                PROLOG 3
                DATA_KEY_COMPRESSION T
                DATA_RECORD_COMPRESSION F
                INDEX_COMPRESSION Y
                CUSTOM_KEY_ATTRIBUTE retained
            """;

        var document = parser.Parse(source);
        var key = document.Keys.Single();

        Assert.AreSame(document.GetSection("KEY"), key.Section);
        Assert.AreEqual(7, key.Number);
        Assert.AreEqual("CUSTOMER_ID", key.Name);
        Assert.AreEqual("STRING", key.Type);
        Assert.AreEqual(0, key.Position);
        Assert.AreEqual(10, key.Length);
        Assert.AreEqual(true, key.Duplicates);
        Assert.AreEqual(false, key.Changes);
        Assert.AreEqual(true, key.NullKey);
        Assert.AreEqual("0000000000", key.NullValue);
        Assert.AreEqual("MULTINATIONAL", key.CollatingSequence);
        Assert.AreEqual(1, key.DataArea);
        Assert.AreEqual(2, key.Level1IndexArea);
        Assert.AreEqual(3, key.IndexArea);
        Assert.AreEqual(80, key.DataFill);
        Assert.AreEqual(70, key.IndexFill);
        Assert.AreEqual(3, key.Prolog);
        Assert.AreEqual(true, key.DataKeyCompression);
        Assert.AreEqual(false, key.DataRecordCompression);
        Assert.AreEqual(true, key.IndexCompression);
        Assert.AreEqual("retained", key.GetAttribute("custom_key_attribute")!.Value);
        Assert.AreEqual(19, key.Attributes.Count);
        Assert.AreEqual(0, key.Segments.Count);
    }

    [TestMethod]
    [DataRow("0", 0)]
    [DataRow("254", 254)]
    [DataRow("+12", 12)]
    [DataRow("-1", -1)]
    [DataRow("\"42\"", 42)]
    public void KeyModel_NumberParsesInvariantIntegerValues(string sourceValue, int expected)
    {
        var key = parser.Parse($"KEY {sourceValue}").Keys.Single();

        Assert.AreEqual(expected, key.Number);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("primary")]
    [DataRow("1.5")]
    [DataRow("2147483648")]
    public void KeyModel_InvalidOrMissingNumberReturnsNull(string sourceValue)
    {
        var key = parser.Parse($"KEY {sourceValue}").Keys.Single();

        Assert.IsNull(key.Number);
    }

    [TestMethod]
    public void Document_KeysPreserveSourceOrder()
    {
        var document = parser.Parse(
            "KEY 2\nNAME two\nKEY 0\nNAME zero\nKEY 1\nNAME one");

        CollectionAssert.AreEqual(
            new int?[] { 2, 0, 1 },
            document.Keys.Select(key => key.Number).ToArray());
        CollectionAssert.AreEqual(
            new[] { "two", "zero", "one" },
            document.Keys.Select(key => key.Name).ToArray());
    }

    [TestMethod]
    public void KeyModel_GroupsSegmentsAndOrdersThemByNumber()
    {
        const string source = """
            KEY 1
                NAME COMPOSITE
                SEG2_POSITION 30
                SEG0_LENGTH 10
                SEG1_POSITION 10
                SEG0_POSITION 0
                SEG2_LENGTH 5
                SEG1_LENGTH 20
                CUSTOM retained
            """;

        var key = parser.Parse(source).Keys.Single();

        CollectionAssert.AreEqual(
            new[] { 0, 1, 2 },
            key.Segments.Select(segment => segment.Number).ToArray());
        Assert.AreEqual(0, key.Segments[0].Position);
        Assert.AreEqual(10, key.Segments[0].Length);
        Assert.AreEqual(10, key.Segments[1].Position);
        Assert.AreEqual(20, key.Segments[1].Length);
        Assert.AreEqual(30, key.Segments[2].Position);
        Assert.AreEqual(5, key.Segments[2].Length);
        CollectionAssert.AreEqual(
            new[] { "NAME", "CUSTOM" },
            key.Attributes.Select(attribute => attribute.Name).ToArray());
    }

    [TestMethod]
    public void KeySegment_PreservesItsAttributesInSourceOrder()
    {
        const string source = """
            KEY 0
                SEG3_LENGTH 8
                SEG3_CUSTOM "extra"
                SEG3_POSITION 40
            """;

        var segment = parser.Parse(source).Keys.Single().Segments.Single();

        Assert.AreEqual(3, segment.Number);
        CollectionAssert.AreEqual(
            new[] { "SEG3_LENGTH", "SEG3_CUSTOM", "SEG3_POSITION" },
            segment.Attributes.Select(attribute => attribute.Name).ToArray());
        Assert.AreEqual("extra", segment.GetAttribute("custom")!.Value);
        Assert.AreEqual(8, segment.Length);
        Assert.AreEqual(40, segment.Position);
    }

    [TestMethod]
    public void KeySegment_DuplicateSuffixUsesFinalAttribute()
    {
        var segment = parser.Parse(
            "KEY 0\nSEG0_POSITION 1\nSEG0_POSITION 2\nSEG0_LENGTH 3\nSEG0_LENGTH 4")
            .Keys.Single()
            .Segments.Single();

        Assert.AreEqual(2, segment.Position);
        Assert.AreEqual(4, segment.Length);
        Assert.AreSame(segment.Attributes[1], segment.GetAttribute("POSITION"));
        Assert.AreSame(segment.Attributes[3], segment.GetAttribute("length"));
    }

    [TestMethod]
    public void KeySegment_MissingAndInvalidTypedValuesReturnNull()
    {
        var segments = parser.Parse(
            "KEY 0\nSEG0_CUSTOM x\nSEG1_POSITION invalid\nSEG1_LENGTH 2147483648")
            .Keys.Single()
            .Segments;

        Assert.IsNull(segments[0].Position);
        Assert.IsNull(segments[0].Length);
        Assert.IsNull(segments[0].GetAttribute("POSITION"));
        Assert.IsNull(segments[1].Position);
        Assert.IsNull(segments[1].Length);
    }

    [TestMethod]
    public void KeyModel_MissingAttributesReturnNull()
    {
        var key = parser.Parse("KEY").Keys.Single();

        Assert.IsNull(key.Number);
        Assert.IsNull(key.Name);
        Assert.IsNull(key.Type);
        Assert.IsNull(key.Position);
        Assert.IsNull(key.Length);
        Assert.IsNull(key.Duplicates);
        Assert.IsNull(key.Changes);
        Assert.IsNull(key.NullKey);
        Assert.IsNull(key.NullValue);
        Assert.IsNull(key.CollatingSequence);
        Assert.IsNull(key.DataArea);
        Assert.IsNull(key.Level1IndexArea);
        Assert.IsNull(key.IndexArea);
        Assert.IsNull(key.DataFill);
        Assert.IsNull(key.IndexFill);
        Assert.IsNull(key.Prolog);
        Assert.IsNull(key.DataKeyCompression);
        Assert.IsNull(key.DataRecordCompression);
        Assert.IsNull(key.IndexCompression);
        Assert.AreEqual(0, key.Attributes.Count);
        Assert.AreEqual(0, key.Segments.Count);
    }

    [TestMethod]
    public void KeyModel_InvalidTypedValuesReturnNullWithoutLosingAttributes()
    {
        const string source = """
            KEY x
                POSITION x
                LENGTH 1.5
                DUPLICATES MAYBE
                CHANGES SOMETIMES
                NULL_KEY UNKNOWN
                DATA_AREA enormous
                DATA_KEY_COMPRESSION ENABLED
            """;

        var key = parser.Parse(source).Keys.Single();

        Assert.IsNull(key.Number);
        Assert.IsNull(key.Position);
        Assert.IsNull(key.Length);
        Assert.IsNull(key.Duplicates);
        Assert.IsNull(key.Changes);
        Assert.IsNull(key.NullKey);
        Assert.IsNull(key.DataArea);
        Assert.IsNull(key.DataKeyCompression);
        Assert.AreEqual(7, key.Attributes.Count);
    }
}
