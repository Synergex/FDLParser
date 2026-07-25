namespace FDLParser.Tests;

[TestClass]
public sealed class KeywordNormalizationTests
{
    private static readonly string[] FileAttributeNames =
    [
        "ALLOCATION", "ASYNCHRONOUS", "BEST_TRY_CONTIGUOUS", "BUCKET_SIZE", "CLUSTER_SIZE",
        "CONTEXT", "CONTIGUOUS", "CREATE_IF", "DEFAULT_NAME", "DEFERRED_WRITE", "DELETE_ON_CLOSE",
        "DIRECTORY_ENTRY", "EXTENSION", "FILE_MONITORING", "GLOBAL_BUFFER_COUNT",
        "MAX_RECORD_NUMBER", "MAXIMIZE_VERSION", "MT_BLOCK_SIZE", "MT_CLOSE_REWIND",
        "MT_CURRENT_POSITION", "MT_NOT_EOF", "MT_OPEN_REWIND", "MT_PROTECTION", "NAME",
        "NON_FILE_STRUCTURED", "ORGANIZATION", "OUTPUT_FILE_PARSE", "OWNER", "PRINT_ON_CLOSE",
        "PROTECTION", "READ_CHECK", "REVISION", "SEQUENTIAL_ONLY", "STORED_SEMANTICS",
        "SUBMIT_ON_CLOSE", "SUPERSEDE", "TEMPORARY", "TRUNCATE_ON_CLOSE", "USER_FILE_OPEN",
        "WINDOW_SIZE", "WRITE_CHECK"
    ];

    private static readonly string[] RecordAttributeNames =
    [
        "BLOCK_SPAN", "CARRIAGE_CONTROL", "CONTROL_FIELD", "FORMAT", "SIZE"
    ];

    private static readonly string[] KeyAttributeNames =
    [
        "CHANGES", "COLLATING_SEQUENCE", "DATA_AREA", "DATA_FILL", "DATA_KEY_COMPRESSION",
        "DATA_RECORD_COMPRESSION", "DUPLICATES", "INDEX_AREA", "INDEX_COMPRESSION", "INDEX_FILL",
        "LENGTH", "LEVEL1_INDEX_AREA", "NAME", "NULL_KEY", "NULL_VALUE", "POSITION", "PROLOG",
        "TYPE"
    ];

    private readonly FDLFileParser parser = new();

    public static IEnumerable<object[]> PrimarySectionNames()
    {
        yield return ["TITLE"];
        yield return ["IDENT"];
        yield return ["SYSTEM"];
        yield return ["FILE"];
        yield return ["DATE"];
        yield return ["RECORD"];
        yield return ["ACCESS"];
        yield return ["NETWORK"];
        yield return ["SHARING"];
        yield return ["CONNECT"];
        yield return ["AREA"];
        yield return ["KEY"];
        yield return ["JOURNAL"];
        yield return ["ANALYSIS_OF_AREA"];
        yield return ["ANALYSIS_OF_KEY"];
    }

    public static IEnumerable<object[]> PrimarySectionAbbreviations()
    {
        yield return ["TIT", "TITLE"];
        yield return ["IDE", "IDENT"];
        yield return ["SYS", "SYSTEM"];
        yield return ["FIL", "FILE"];
        yield return ["DAT", "DATE"];
        yield return ["REC", "RECORD"];
        yield return ["ACC", "ACCESS"];
        yield return ["NET", "NETWORK"];
        yield return ["SHA", "SHARING"];
        yield return ["CONN", "CONNECT"];
        yield return ["ARE", "AREA"];
        yield return ["KEY", "KEY"];
        yield return ["JOU", "JOURNAL"];
        yield return ["ANALYSIS_OF_A", "ANALYSIS_OF_AREA"];
        yield return ["ANALYSIS_OF_K", "ANALYSIS_OF_KEY"];
    }

    public static IEnumerable<object[]> KnownAttributeNames()
    {
        return AttributeCases("FILE", FileAttributeNames)
            .Concat(AttributeCases("RECORD", RecordAttributeNames))
            .Concat(AttributeCases("KEY", KeyAttributeNames));
    }

    public static IEnumerable<object[]> UniqueAttributeAbbreviations()
    {
        return AbbreviationCases("FILE", FileAttributeNames)
            .Concat(AbbreviationCases("RECORD", RecordAttributeNames))
            .Concat(AbbreviationCases("KEY", KeyAttributeNames));
    }

    [TestMethod]
    [DynamicData(nameof(PrimarySectionNames))]
    public void Parse_PrimarySectionNamesAreCaseInsensitiveAndNormalized(string name)
    {
        var sourceName = AlternateCase(name);

        var section = parser.Parse(sourceName).Sections.Single();

        Assert.AreEqual(name, section.Name);
        Assert.AreEqual(sourceName, section.OriginalName);
    }

    [TestMethod]
    [DynamicData(nameof(PrimarySectionAbbreviations))]
    public void Parse_UniquePrimarySectionAbbreviationsAreExpanded(
        string abbreviation,
        string expectedName)
    {
        var section = parser.Parse(abbreviation.ToLowerInvariant()).Sections.Single();

        Assert.AreEqual(expectedName, section.Name);
        Assert.AreEqual(abbreviation.ToLowerInvariant(), section.OriginalName);
    }

    [TestMethod]
    [DynamicData(nameof(KnownAttributeNames))]
    public void Parse_KnownAttributeNamesAreCaseInsensitiveAndNormalized(
        string sectionName,
        string expectedName)
    {
        var sourceName = AlternateCase(expectedName);
        var section = parser.Parse($"{sectionName}\n{sourceName} value").Sections.Single();
        var attribute = section.Attributes.Single();

        Assert.AreEqual(expectedName, attribute.Name);
        Assert.AreEqual(sourceName, attribute.OriginalName);
    }

    [TestMethod]
    [DynamicData(nameof(UniqueAttributeAbbreviations))]
    public void Parse_ShortestUniqueAttributeAbbreviationsAreExpanded(
        string sectionName,
        string abbreviation,
        string expectedName)
    {
        var sourceName = abbreviation.ToLowerInvariant();
        var section = parser.Parse($"{sectionName}\n{sourceName} value").Sections.Single();
        var attribute = section.Attributes.Single();

        Assert.AreEqual(expectedName, attribute.Name);
        Assert.AreEqual(sourceName, attribute.OriginalName);
    }

    [TestMethod]
    [DataRow("FILE", "M")]
    [DataRow("FILE", "MT_")]
    [DataRow("FILE", "P")]
    [DataRow("KEY", "DATA_")]
    [DataRow("KEY", "INDEX_")]
    [DataRow("KEY", "NULL_")]
    public void Parse_AmbiguousAttributeAbbreviationIsNotExpanded(
        string sectionName,
        string abbreviation)
    {
        var attribute = parser
            .Parse($"{sectionName}\n{abbreviation.ToLowerInvariant()} value")
            .Sections.Single()
            .Attributes.Single();

        Assert.AreEqual(abbreviation, attribute.Name);
    }

    [TestMethod]
    [DataRow("FILE", "D", "DATE")]
    [DataRow("KEY", "D", "DATE")]
    [DataRow("KEY", "I", "IDENT")]
    [DataRow("KEY", "N", "NETWORK")]
    public void Parse_AmbiguousAttributeAbbreviationThatUniquelyIdentifiesPrimarySection_StartsSection(
        string currentSection,
        string abbreviation,
        string expectedSection)
    {
        var document = parser.Parse($"{currentSection}\n{abbreviation.ToLowerInvariant()} value");

        Assert.AreEqual(2, document.Sections.Count);
        Assert.AreEqual(currentSection, document.Sections[0].Name);
        Assert.AreEqual(expectedSection, document.Sections[1].Name);
        Assert.AreEqual("value", document.Sections[1].RawValue);
    }

    [TestMethod]
    public void Parse_AmbiguousPrimaryAbbreviationIsNotASection()
    {
        var exception = Assert.ThrowsExactly<FDLParseException>(
            () => parser.Parse("ANALYSIS_OF 0"));

        StringAssert.Contains(exception.Message, "does not belong to a primary section");
    }

    [TestMethod]
    public void Parse_FileContextIsNotMistakenForConnectSection()
    {
        const string source = """
            FILE
                CONTEXT 42
                CONTIGUOUS YES
            CONNECT
                FACILITY RMS
            """;

        var document = parser.Parse(source);

        Assert.AreEqual(2, document.Sections.Count);
        Assert.AreEqual("42", document.File!.GetAttribute("CONTEXT")!.Value);
        Assert.AreEqual("RMS", document.GetSection("CONNECT")!.GetString("FACILITY"));
    }

    [TestMethod]
    [DataRow("SEG0_P", "SEG0_POSITION")]
    [DataRow("seg1_l", "SEG1_LENGTH")]
    [DataRow("SeG25_PoS", "SEG25_POSITION")]
    [DataRow("SEG254_LEN", "SEG254_LENGTH")]
    public void Parse_KeySegmentSuffixAbbreviationsAreExpanded(
        string sourceName,
        string expectedName)
    {
        var attribute = parser.Parse($"KEY 0\n{sourceName} 10").Keys.Single().Section.Attributes.Single();

        Assert.AreEqual(expectedName, attribute.Name);
        Assert.AreEqual(sourceName, attribute.OriginalName);
    }

    [TestMethod]
    public void Parse_UnknownKeySegmentSuffixIsNormalizedButRetained()
    {
        var attribute = parser.Parse("KEY 0\nseg2_custom value")
            .Keys.Single()
            .Section.Attributes.Single();

        Assert.AreEqual("SEG2_CUSTOM", attribute.Name);
        Assert.AreEqual("seg2_custom", attribute.OriginalName);
    }

    private static IEnumerable<object[]> AttributeCases(
        string sectionName,
        IEnumerable<string> names)
    {
        return names.Select(name => new object[] { sectionName, name });
    }

    private static IEnumerable<object[]> AbbreviationCases(
        string sectionName,
        IReadOnlyCollection<string> names)
    {
        foreach (var name in names)
        {
            var abbreviation = Enumerable
                .Range(1, name.Length)
                .Select(length => name[..length])
                .First(candidate =>
                    names.Count(other => other.StartsWith(candidate, StringComparison.Ordinal)) == 1);

            yield return [sectionName, abbreviation, name];
        }
    }

    private static string AlternateCase(string value)
    {
        return string.Concat(value.Select(
            (character, index) => index % 2 == 0
                ? char.ToLowerInvariant(character)
                : char.ToUpperInvariant(character)));
    }
}
