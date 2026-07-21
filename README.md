# FDLParser

FDLParser is a .NET Standard 2.0 library for reading OpenVMS File Definition Language (FDL) text into a navigable .NET object model. It is useful for inspecting generated FDL files, migrating file definitions, building reporting or validation tools, and extracting RMS file characteristics without writing an FDL lexer.

FDL describes the characteristics of an OpenVMS RMS data file. An FDL document is organized into primary sections, such as `FILE`, `RECORD`, and `KEY`, followed by their secondary attributes. VSI's [OpenVMS Record Management Utilities Reference Manual](https://docs.vmssoftware.com/vsi-openvms-record-management-utilities-reference-manual/) is the authoritative reference for FDL syntax, attributes, values, and validity rules.

## Add the library

Install [FDLParser from NuGet](https://www.nuget.org/packages/FDLParser) in a .NET Standard 2.0-compatible project:

```powershell
dotnet add package FDLParser
```

In Visual Studio's Package Manager Console, use:

```powershell
Install-Package FDLParser
```

For local source development, reference the project directly instead:

```xml
<ItemGroup>
  <ProjectReference Include="..\FDLParser\FDLParser.csproj" />
</ItemGroup>
```

Then import its namespace:

```csharp
using FDLParser;
```

## FDL at a glance

The following abbreviated FDL definition describes an indexed RMS file with fixed-length records and one key:

```text
TITLE
    "Customer master file"

FILE
    ORGANIZATION            INDEXED
    NAME                    "CUSTOMER.DAT"
    ALLOCATION              100
    BUCKET_SIZE             12

RECORD
    FORMAT                  FIXED
    SIZE                    128

KEY 0
    NAME                    "CUSTOMER_ID"
    POSITION                0
    LENGTH                  10
    TYPE                    STRING
```

`FILE`, `RECORD`, and `KEY 0` are primary sections. `ORGANIZATION`, `FORMAT`, `SIZE`, and `LENGTH` are secondary attributes belonging to the preceding section. The `KEY` value identifies the key: VSI specifies `KEY 0` for an indexed file's primary key and `KEY 1` through `KEY 254` for secondary keys. See the VSI manual's [FILE section](https://docs.vmssoftware.com/vsi-openvms-record-management-utilities-reference-manual/), [KEY section](https://docs.vmssoftware.com/vsi-openvms-record-management-utilities-reference-manual/), and [RECORD section](https://docs.vmssoftware.com/vsi-openvms-record-management-utilities-reference-manual/) for the complete definitions.

An FDL statement may end at the end of a source line or at a semicolon. This permits a compact form when that is more convenient:

```text
FILE; ORGANIZATION SEQUENTIAL; NAME "REPORT.DAT";
RECORD; FORMAT STREAM_LF;
```

For the rules governing section order, statement delimiters, legal values, and which attributes are create-time or run-time attributes, use VSI's [FDL facility documentation](https://docs.vmssoftware.com/vsi-openvms-record-management-utilities-reference-manual/). FDLParser reads the document; it does not replace OpenVMS semantic validation or create an RMS file.

## Parse an FDL file

Use `FDLParser.ParseFile` to read an `.FDL` file, or `FDLParser.Parse` when the FDL text is already in memory.

```csharp
using FDLParser;

var parser = new FDLParser();
FDLFile document = parser.ParseFile("customer.fdl");

Console.WriteLine(document.Title);                  // Customer master file
Console.WriteLine(document.File?.Organization);     // Indexed
Console.WriteLine(document.Record?.Format);         // Fixed
Console.WriteLine(document.Record?.Size);           // 128
```

For example, this parses an FDL string directly:

```csharp
var document = new FDLParser().Parse(@"
FILE
    ORGANIZATION SEQUENTIAL
RECORD
    FORMAT STREAM_LF");

bool? blockSpan = document.Record?.BlockSpan;
```

`Parse` throws `ArgumentNullException` for a null FDL string. `ParseFile` throws `ArgumentNullException` or `ArgumentException` when its path is null, empty, or whitespace, and it propagates file-system exceptions from `File.ReadAllText`.

## Typed access to FILE and RECORD sections

`FDLFile.File` exposes an `FDLFileAttributes` model for the main `FILE` section. It provides typed access to common attributes while retaining the original section and every attribute:

```csharp
FDLFileAttributes? file = document.File;

if (file is not null)
{
    Console.WriteLine(file.Name);              // CUSTOMER.DAT
    Console.WriteLine(file.Organization);      // Indexed
    Console.WriteLine(file.Allocation);        // 100
    Console.WriteLine(file.BucketSize);        // 12
    Console.WriteLine(file.Contiguous);        // True, False, or null
}
```

`FDLFileAttributes` includes typed properties for the file name, default name, organization, allocation, bucket size, extension, global buffer count, maximum record number, owner, protection, and the `BEST_TRY_CONTIGUOUS` and `CONTIGUOUS` switches. Use `file.GetAttribute("ATTRIBUTE_NAME")` for any other FILE attribute.

`FDLFile.Record` supplies an `FDLRecordAttributes` model for `RECORD`:

```csharp
FDLRecordAttributes? record = document.Record;

if (record is not null)
{
    Console.WriteLine(record.FormatText);       // FIXED
    Console.WriteLine(record.Format);           // FDLRecordFormat.Fixed
    Console.WriteLine(record.Size);             // 128
    Console.WriteLine(record.CarriageControl);  // CARRIAGE_RETURN, for example
}
```

The `FDLRecordFormat` enum recognizes `FIXED`, `VARIABLE`, `VFC`, `STREAM`, `STREAM_CR`, `STREAM_LF`, and `UNDEFINED`. Values outside that set remain available through `FormatText`, while `Format` is `null`.

## Work with indexed keys and segments

`FDLFile.Keys` returns each `KEY` section in source order as an `FDLKeyDefinition`. It exposes common key attributes and groups segmented-key attributes into `FDLKeySegment` objects.

```text
KEY 1
    NAME            "LAST_FIRST"
    TYPE            STRING
    DUPLICATES      YES
    SEG0_POSITION   1
    SEG0_LENGTH     20
    SEG1_POSITION   21
    SEG1_LENGTH     20
```

```csharp
foreach (FDLKeyDefinition key in document.Keys)
{
    Console.WriteLine($"Key {key.Number}: {key.Name}");
    Console.WriteLine($"Allows duplicates: {key.Duplicates}");

    foreach (FDLKeySegment segment in key.Segments)
    {
        Console.WriteLine(
            $"  Segment {segment.Number}: position {segment.Position}, length {segment.Length}");
    }
}
```

The `FDLKeyDefinition` model also exposes `Type`, `Position`, `Length`, `Changes`, `NullKey`, `NullValue`, `CollatingSequence`, area and fill values, `Prolog`, and compression flags. Unmodeled key attributes remain available through `key.GetAttribute("ATTRIBUTE_NAME")`; `key.Attributes` contains the non-segment attributes.

## Inspect every section and attribute

FDLParser preserves the complete primary-section and secondary-attribute tree, including supported sections without a specialized typed model. These recognized sections are `TITLE`, `IDENT`, `SYSTEM`, `FILE`, `DATE`, `RECORD`, `ACCESS`, `NETWORK`, `SHARING`, `CONNECT`, `AREA`, `KEY`, `JOURNAL`, `ANALYSIS_OF_AREA`, and `ANALYSIS_OF_KEY`.

```csharp
foreach (FDLSection section in document.Sections)
{
    Console.WriteLine($"{section.Name} at line {section.Location.Line}");

    foreach (FDLAttribute attribute in section.Attributes)
    {
        Console.WriteLine(
            $"  {attribute.Name} = {attribute.Value} " +
            $"(source: {attribute.Location.Line}:{attribute.Location.Column})");
    }
}

FDLSection? access = document.GetSection("ACCESS");
string? mode = access?.GetString("ACCESS_MODE");
int? buffers = access?.GetInt32("MULTIBUFFER_COUNT");
bool? shared = document.GetSection("SHARING")?.GetBoolean("USER_INTERLOCK");
```

`GetSection`, `GetSections`, `GetAttribute`, and `GetAttributes` match names case-insensitively. `GetSection` and `GetAttribute` return the final matching item; the plural methods return every match in source order.

Each `FDLAttribute` provides:

- `RawValue` — the source value after surrounding whitespace is removed.
- `Value` — `RawValue` with one matching pair of enclosing quotes removed and doubled internal quotes reduced to one.
- `BooleanValue` — `true` for `YES`, `TRUE`, `Y`, or `T`; `false` for `NO`, `FALSE`, `N`, or `F`; otherwise `null`.
- `Int32Value` and `Int64Value` — invariant-culture decimal conversions, or `null` when the value is not in range or not an integer.
- `Location` — the line and column at which the attribute begins.

Quoted title, identification, and attribute values are handled directly. Both single and double quotes are supported, with a doubled quote escaping the quote within the value:

```text
TITLE
    "The ""production"" customer file"
FILE
    OWNER 'OPERATIONS'
```

In this example, `document.Title` is `The "production" customer file`, and `document.File?.Owner` is `OPERATIONS`.

## FDL abbreviations and original source text

The parser treats FDL keywords case-insensitively. For known `FILE`, `RECORD`, and `KEY` attributes, it expands an unambiguous abbreviation to the normalized full name:

```text
FILE
    ORG     INDEXED
    BUCK    12
RECORD
    FOR     FIXED
KEY 0
    POS     0
    LEN     10
```

The resulting names are `ORGANIZATION`, `BUCKET_SIZE`, `FORMAT`, `POSITION`, and `LENGTH`. `OriginalName` retains the source spelling, so `document.File?.GetAttribute("ORGANIZATION")?.OriginalName` is `ORG` in this example. If an abbreviation is ambiguous or an attribute is not in those known sets, its uppercase source name is retained.

## Error handling and scope

Malformed quoted text and an attribute that appears before any primary section cause `FDLParseException`. Its `Location` property identifies the source line and column:

```csharp
try
{
    var document = new FDLParser().ParseFile("input.fdl");
}
catch (FDLParseException exception)
{
    Console.Error.WriteLine(
        $"FDL error at {exception.Location.Line}:{exception.Location.Column}: {exception.Message}");
}
```

The parser is intentionally a reader and model builder. It retains sections and attributes but does not enforce all OpenVMS rules, such as valid attribute values, section ordering, key-number density, or compatibility among RMS attributes. Check the result against the [VSI FDL facility reference](https://docs.vmssoftware.com/vsi-openvms-record-management-utilities-reference-manual/) when semantic validation is required.

## Related VSI documentation

- [VSI OpenVMS Record Management Utilities Reference Manual](https://docs.vmssoftware.com/vsi-openvms-record-management-utilities-reference-manual/) — the primary FDL reference, including the FILE, KEY, RECORD, AREA, ACCESS, and other sections, plus the Create/FDL and Edit/FDL utilities.
- [VSI OpenVMS Guide to File Applications](https://docs.vmssoftware.com/docs/guide-to-openvms-file-applications.pdf) — background on designing sequential, relative, and indexed files, with FDL examples.
- [VSI Utility Routines Manual: FDL routines](https://docs.vmssoftware.com/vsi-openvms-utility-routines/) — callable OpenVMS routines such as `FDL$CREATE`, `FDL$GENERATE`, and `FDL$PARSE` for applications that use FDL with RMS.

## Versioning and NuGet packages

The repository-level [VERSION](VERSION) file contains the editable major and minor base version in `X.Y` form. Each build appends its UTC timestamp, producing `X.Y.YYDDD.HHMM` for the assemblies and NuGet packages. For example, a build at 16:45 UTC on the 172nd day of 2026 produces `1.0.26172.1645`.

The requested format has minute precision, so builds started within the same UTC minute receive the same version. Use a seconds field or an incrementing build counter if every individual build must have a distinct package version.

To begin the next major or minor release line, edit only `VERSION`:

```text
2.0
```

The SDK-style project contains the package metadata. Create matching release and symbol packages with:

```powershell
dotnet pack FDLParser/FDLParser.csproj --configuration Release
```

Do not use `--no-build` when creating a package: packing performs the build and ensures the package version and the version embedded in its assembly are identical.

## License

This project is licensed under the BSD 2-Clause License. See [LICENSE](LICENSE).
