using RStein.TOML.Test.Helpers;
using System.Net.NetworkInformation;

namespace RStein.TOML.Test;

[TestFixture]
internal partial class TomlSerializerTest
{
  [Test]
  public async Task SerializeToStringAsync_When_Complex_Toml_Then_Returns_Expected_Data()
  {
    var tomlTable = new TomlTable()
    {
      {
        "inlineTable1", new TomlInlineTable(new TomlKey("inlineTable", TomlKeyType.SimpleUnquoted))
        {
          HasFromInlineTableDefinition = true,
          ["key1"] = new TomlPrimitiveValue("inlineValue", TomlValueType.String, TomlDataType.LiteralString),
          ["key2"] = new TomlPrimitiveValue(8, TomlDataType.IntegerHex)
        }
      },
      {"key1", new TomlPrimitiveValue("value1", TomlValueType.String, TomlDataType.BasicString)},
      {"key2", new TomlPrimitiveValue(10)},
      {"keyhex", new TomlPrimitiveValue(10, TomlDataType.IntegerHex)},
      {"keyoct", new TomlPrimitiveValue(10, TomlDataType.IntegerOct)},
      {"keybin", new TomlPrimitiveValue(10, TomlDataType.IntegerBin)},
      {"keyf", new TomlPrimitiveValue(10.1111)},
      {"keyd", new TomlPrimitiveValue(10.1111d)},
      {"keydecimal", new TomlPrimitiveValue((decimal) 10.6666)},
      {"dt", new TomlPrimitiveValue(DateTime.Now)},
      {"arrInt", new TomlArray() {new TomlPrimitiveValue(1), new TomlPrimitiveValue(2), new TomlPrimitiveValue(3)}},
      {"hybridArr", new TomlArray() {new TomlPrimitiveValue(1), new TomlPrimitiveValue("arrStringVal", TomlValueType.String, TomlDataType.LiteralString), new TomlPrimitiveValue(true)}},
      {
        "nested.Table", new TomlTable(new TomlKey("nested.Table", TomlKeyType.SimpleQuotedBasicString))
        {
          HasTopLevelDefinition = true,
          ["key1"] = new TomlPrimitiveValue("tl1Value", TomlValueType.String, TomlDataType.BasicString),
          ["key2"] = new TomlPrimitiveValue(32)
        }
      },
      {
        "nested.Table2", new TomlTable(new TomlKey("nested.Table2", TomlKeyType.SimpleQuotedBasicString))
        {
          HasTopLevelDefinition = true,
          ["key1"] = new TomlPrimitiveValue("tl3Value", TomlValueType.String, TomlDataType.BasicString),
          ["key2"] = new TomlPrimitiveValue(8, TomlDataType.IntegerBin)
        }
      }
    };

    var toml = await TomlSerializer.SerializeToStringAsync(tomlTable).ConfigureAwait(false);
    await TestContext.Out.WriteLineAsync(toml).ConfigureAwait(false);
    var deserializedToml = await TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false);
    var areTomlEquals = await tomlTable.DeepEqualsAsync(deserializedToml).ConfigureAwait(false);

    Assert.That(areTomlEquals);
  }

  [Test]
  public async Task SerializeToStringAsync_When_Complex_Toml_Without_Explicit_Tagging_Then_Returns_Expected_Data()
  {
    var tomlTable = new TomlTable()
    {
      {
        "inlineTable1", new TomlInlineTable(new TomlKey("inlineTable", TomlKeyType.SimpleUnquoted))
        {
          ["key1"] = new TomlPrimitiveValue("inlineValue", TomlValueType.String, TomlDataType.LiteralString),
          ["key2"] = new TomlPrimitiveValue(8, TomlDataType.IntegerHex)
        }
      },
      {"key1", new TomlPrimitiveValue("value1", TomlValueType.String, TomlDataType.BasicString)},
      {"key2", new TomlPrimitiveValue(10)},
      {"keyhex", new TomlPrimitiveValue(10, TomlDataType.IntegerHex)},
      {"keyoct", new TomlPrimitiveValue(10, TomlDataType.IntegerOct)},
      {"keybin", new TomlPrimitiveValue(10, TomlDataType.IntegerBin)},
      {"keyf", new TomlPrimitiveValue(10.1111)},
      {"keyd", new TomlPrimitiveValue(10.1111d)},
      {"keydecimal", new TomlPrimitiveValue((decimal) 10.6666)},
      {"dt", new TomlPrimitiveValue(DateTime.Now)},
      {"arrInt", new TomlArray() {new TomlPrimitiveValue(1), new TomlPrimitiveValue(2), new TomlPrimitiveValue(3)}},
      {"hybridArr", new TomlArray() {new TomlPrimitiveValue(1), new TomlPrimitiveValue("arrStringVal", TomlValueType.String, TomlDataType.LiteralString), new TomlPrimitiveValue(true)}},
      {
        "nested.Table", new TomlTable(new TomlKey("nested.Table", TomlKeyType.SimpleQuotedBasicString))
        {
          ["key1"] = new TomlPrimitiveValue("tl1Value", TomlValueType.String, TomlDataType.BasicString),
          ["key2"] = new TomlPrimitiveValue(32)
        }
      },
      {
        "nested.Table2", new TomlTable(new TomlKey("nested.Table2", TomlKeyType.SimpleQuotedBasicString))
        {
          ["key1"] = new TomlPrimitiveValue("tl3Value", TomlValueType.String, TomlDataType.BasicString),
          ["key2"] = new TomlPrimitiveValue(8, TomlDataType.IntegerBin)
        }
      }
    };

    var toml = await TomlSerializer.SerializeToStringAsync(tomlTable).ConfigureAwait(false);
    await TestContext.Out.WriteLineAsync(toml).ConfigureAwait(false);
    var deserializedToml = await TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false);
    var areTomlEquals = await tomlTable.DeepEqualsAsync(deserializedToml).ConfigureAwait(false);

    Assert.That(areTomlEquals);
  }

  [Test]
  public async Task SerializeToStringAsync_When_Complex_Toml_Without_Explicit_Primitive_Values_Then_Returns_Expected_Data()
  {
    var tomlTable = new TomlTable()
    {
      {
        "inlineTable1", new TomlInlineTable(new TomlKey("inlineTable", TomlKeyType.SimpleUnquoted))
        {
          ["key1"] = "inlineValue",
          ["key2"] = 8
        }
      },
      {"key1","value1"},
      {"key2", 10},
      {"keyf", 10.1111f},
      {"keyd", 10.1111d},
      {"keydecimal", (decimal) 10.6666},
      {"dt", DateTime.Now},
      {"arrInt", new TomlArray {1, 2, 3}},
      {"hybridArr", new TomlArray {1, "arrStringVal", true, 10.01f, 100.25d, 45.788888M, "\tsasasasass\n\r"}},
      {
        "nested.Table", new TomlTable(new TomlKey("nested.Table", TomlKeyType.SimpleQuotedBasicString))
        {
          ["key1"] = "tl1Value",
          ["key2"] = 32
        }
      },
      {
        "nested.Table2", new TomlTable(new TomlKey("nested.Table2", TomlKeyType.SimpleQuotedBasicString))
        {
          ["key1"] = "tl3Value",
          ["key2"] = 8
        }
      }
    };

    var toml = await TomlSerializer.SerializeToStringAsync(tomlTable).ConfigureAwait(false);
    await TestContext.Out.WriteLineAsync(toml).ConfigureAwait(false);
    var deserializedToml = await TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false);
    var areTomlEquals = await tomlTable.DeepEqualsAsync(deserializedToml).ConfigureAwait(false);

    Assert.That(areTomlEquals);
  }

  [TestCase("""
            # This is a full-line comment
            key = "value"  # This is a comment at the end of a line
            another = "# This is not a comment"
            """)]
  [TestCase("key = \"value\"")]
  [TestCase("""
            key = "value"
            bare_key = "value"
            bare-key = "value"
            1234 = "value"
            """)]
  [TestCase("""
            "127.0.0.1" = "value"
            "character encoding" = "value"
            "ʎǝʞ" = "value"
            'key2' = "value"
            'quoted "value"' = "value"
            """)]
  [TestCase("""
            "" = "blank"     # VALID but discouraged
            """)]
  [TestCase("""
            '' = "blank"     # VALID but discouraged
            """)]
  [TestCase("""
            name = "Orange"
            physical.color = "orange"
            physical.shape = "round"
            site."google.com" = true
            """)]
  [TestCase("""
            # This makes the key "fruit" into a table.
            fruit.apple.smooth = true

            # So then you can add to the table "fruit" like so:
            fruit.orange = 2
            """)]
  [TestCase("""

            apple.type = "fruit"
            orange.type = "fruit"

            apple.skin = "thin"
            orange.skin = "thick"

            apple.color = "red"
            orange.color = "orange"
            """)]

  [TestCase("""
            # RECOMMENDED

            apple.type = "fruit"
            apple.skin = "thin"
            apple.color = "red"

            orange.type = "fruit"
            orange.skin = "thick"
            orange.color = "orange"
            """)]
  [TestCase("""
            3.14159 = "pi"
            """)]
  [TestCase("""
            str = "I'm a string. \"You can quote me\". Name\tJos\u00E9\nLocation\tSF."
            """)]
  [TestCase(""""
            str1 = """
            Roses are red
            Violets are blue"""
            """")]
  [TestCase("""
            # On a Windows system, it will most likely be equivalent to:
            str3 = "Roses are red\r\nViolets are blue"
            """)]
  [TestCase(""""
            str2 = """
            The quick brown \
            
            
              fox jumps over \
                the lazy dog."""
            """")]
  [TestCase(""""
            str3 = """\
                   The quick brown \
                   fox jumps over \
                   the lazy dog.\
                   """
            """")]
  [TestCase("""""
            str4 = """Here are two quotation marks: "". Simple enough."""
            # str5 = """Here are three quotation marks: """."""  # INVALID
            str5 = """Here are three quotation marks: ""\"."""
            str6 = """Here are fifteen quotation marks: ""\"""\"""\"""\"""\"."""

            # "This," she said, "is just a pointless statement."
            str7 = """"This," she said, "is just a pointless statement.""""
            """"")]
  [TestCase("""
            # What you see is what you get.
            winpath  = 'C:\Users\nodejs\templates'
            winpath2 = '\\ServerX\admin$\system32\'
            quoted   = 'Tom "Dubs" Preston-Werner'
            regex    = '<\i\c*\s*>'
            """)]
  [TestCase("""
            regex2 = '''I [dw]on't need \d{2} apples'''
            lines  = '''
            The first newline is
            trimmed in raw strings.
               All other whitespace
               is preserved.
            '''
            """)]
  [TestCase(""""""""""""""""
            quot15 = '''Here are fifteen quotation marks: """""""""""""""'''

            # apos15 = '''Here are fifteen apostrophes: ''''''''''''''''''  # INVALID
            apos15 = "Here are fifteen apostrophes: '''''''''''''''"

            # 'That,' she said, 'is still pointless.'
            str = ''''That,' she said, 'is still pointless.''''
            """""""""""""""")]
  [TestCase("""
            int1 = +99
            int2 = 42
            int3 = 0
            int4 = -17
            """)]
  [TestCase("""
            int5 = 1_000
            int6 = 5_349_221
            int7 = 53_49_221  # Indian number system grouping
            int8 = 1_2_3_4_5  # VALID but discouraged
            """)]

  [TestCase("""
            # hexadecimal with prefix `0x`
            hex1 = 0xDEADBEEF
            hex2 = 0xdeadbeef
            hex3 = 0xdead_beef

            # octal with prefix `0o`
            oct1 = 0o01234567
            oct2 = 0o755 # useful for Unix file permissions

            # binary with prefix `0b`
            bin1 = 0b11010110
            """)]
  [TestCase("""
            # fractional
            flt1 = +1.0
            flt2 = 3.1415
            flt3 = -0.01

            # exponent
            flt4 = 5e+22
            flt5 = 1e06
            flt6 = -2E-2

            # both
            flt7 = 6.626e-34
            """)]
  [TestCase("""
            flt8 = 224_617.445_991_228
            """)]
  [TestCase("""
            # infinity
            sf1 = inf  # positive infinity
            sf2 = +inf # positive infinity
            sf3 = -inf # negative infinity

            # not a number
            sf4 = nan  # actual sNaN/qNaN encoding is implementation-specific
            sf5 = +nan # same as `nan`
            sf6 = -nan # valid, actual encoding is implementation-specific
            """)]
  [TestCase("""
            bool1 = true
            bool2 = false
            """)]
  [TestCase("""
            odt1 = 1979-05-27T07:32:00Z
            odt2 = 1979-05-27T00:32:00-07:00
            odt3 = 1979-05-27T00:32:00.999999-07:00
            """)]
  [TestCase("""
            odt4 = 1979-05-27 07:32:00Z
            """)]
  [TestCase("""
            ldt1 = 1979-05-27T07:32:00
            ldt2 = 1979-05-27T00:32:00.999999
            """)]
  [TestCase("""
            ld1 = 1979-05-27
            """)]
  [TestCase("""
            lt1 = 07:32:00
            lt2 = 00:32:00.999999
            """)]
  [TestCase(""""
            integers = [ 1, 2, 3 ]
            colors = [ "red", "yellow", "green" ]
            nested_arrays_of_ints = [ [ 1, 2 ], [3, 4, 5] ]
            nested_mixed_array = [ [ 1, 2 ], ["a", "b", "c"] ]
            string_array = [ "all", 'strings', """are the same""", '''type''' ]
            """")]
  [TestCase("""
            # Mixed-type arrays are allowed
            numbers = [ 0.1, 0.2, 0.5, 1, 2, 5 ]
            contributors = [
              "Foo Bar <foo@example.com>",
              { name = "Baz Qux", email = "bazqux@example.com", url = "https://example.com/bazqux" }
            ]
            """)]
  [TestCase("""
            integers2 = [
              1, 2, 3
            ]

            integers3 = [
              1,
              2, # this is ok
            ]
            """)]
  [TestCase("""
            [table-1]
            key1 = "some string"
            key2 = 123

            [table-2]
            key1 = "another string"
            key2 = 456
            """)]
  [TestCase("""
            [dog."tater.man"]
            type.name = "pug"
            """)]
  [TestCase("""
            [a.b.c]            # this is best practice
            [ d.e.f ]          # same as [d.e.f]
            [ g .  h  . i ]    # same as [g.h.i]
            [ j . "ʞ" . 'l' ]  # same as [j."ʞ".'l']
            """)]
  [TestCase("""
            # [x] you
            # [x.y] don't
            # [x.y.z] need these
            [x.y.z.w] # for this to work

            [x] # defining a super-table afterward is ok
            """)]
  [TestCase("""
            # VALID BUT DISCOURAGED
            [fruit.apple]
            [animal]
            [fruit.orange]
            """)]
  [TestCase("""
            # RECOMMENDED
            [fruit.apple]
            [fruit.orange]
            [animal]
            """)]
  [TestCase("""
            # Top-level table begins.
            name = "Fido"
            breed = "pug"

            # Top-level table ends.
            [owner]
            name = "Regina Dogman"
            member_since = 1999-08-04
            """)]
  [TestCase("""
            fruit.apple.color = "red"
            # Defines a table named fruit
            # Defines a table named fruit.apple

            fruit.apple.taste.sweet = true
            # Defines a table named fruit.apple.taste
            # fruit and fruit.apple were already created
            """)]
  [TestCase("""
            [fruit]
            apple.color = "red"
            apple.taste.sweet = true

            # [fruit.apple]  # INVALID
            # [fruit.apple.taste]  # INVALID

            [fruit.apple.texture]  # you can add sub-tables
            smooth = true
            """)]
  [TestCase("""
            name = { first = "Tom", last = "Preston-Werner" }
            point = { x = 1, y = 2 }
            animal = { type.name = "pug" }
            """)]
  [TestCase("""
            [product]
            type = { name = "Nail" }
            """)]
  [TestCase("""
            [[products]]
            name = "Hammer"
            sku = 738594937

            [[products]]  # empty table within the array

            [[products]]
            name = "Nail"
            sku = 284758393

            color = "gray"
            """)]
  [TestCase("""
            [[fruits]]
            name = "apple"

            [fruits.physical]  # subtable
            color = "red"
            shape = "round"

            [[fruits.varieties]]  # nested array of tables
            name = "red delicious"

            [[fruits.varieties]]
            name = "granny smith"


            [[fruits]]
            name = "banana"

            [[fruits.varieties]]
            name = "plantain"
            """)]

  [TestCase("""
            [[fruits]]
            name = "apple"

            [fruits.physical]  # subtable
            color = "red"
            shape = "round"

            [[fruits.varieties]]  # nested array of tables
            name = "red delicious"

            [[fruits.varieties]]
            name = "granny smith"


            [[fruits]]
            name = "banana"
            type = {a = 1, b = 2, c = "3"}

            [[fruits.varieties]]
            name = "plantain"

            [[fruits.varieties.new_table]]

            [new_table]

            [[fruits.varieties.new_table2]]

            """)]

  [TestCase("""
            points = [ { x = 1, y = 2, z = 3 },
            { x = 7, y = 8, z = 9 },
            { x = 2, y = 4, z = 8 } ]
            """)]

  [TestCase("""
            # This is a TOML document. Boom.

            title = "TOML Example"

            [owner]
            name = "Lance Uppercut"
            dob = 1979-05-27T07:32:00-08:00 # First class dates? Why not?

            [database]
            server = "192.168.1.1"
            ports = [ 8001, 8001, 8002 ]
            connection_max = 5000
            enabled = true

            [servers]
            
              # You can indent as you please. Tabs or spaces. TOML don't care.
              [servers.alpha]
              ip = "10.0.0.1"
              dc = "eqdc10"
            
              [servers.beta]
              ip = "10.0.0.2"
              dc = "eqdc10"

            [clients]
            data = [ ["gamma", "delta"], [1, 2] ]

            # Line breaks are OK when inside arrays
            hosts = [
              "alpha",
              "omega"
            ]
            """)]
  [TestCase("""
            [a.b.c]
            answer = 42

            [a]
            better = 43
            """)]
  public async Task SerializeToStringAsync_When_Serialize_Parsed_Data_Then_Serialized_Toml_Is_Equal_To_Parsed_Toml(string srcToml)
  {
    var parsedToml = await TomlSerializer.DeserializeAsync(srcToml).ConfigureAwait(false);

    var serializedToml = await TomlSerializer.SerializeToStringAsync(parsedToml).ConfigureAwait(false);
    await TestContext.Out.WriteLineAsync(serializedToml).ConfigureAwait(false);

    var parsedToml2 = await TomlSerializer.DeserializeAsync(serializedToml).ConfigureAwait(false);
    var areParsedTomlEquals = await parsedToml2.DeepEqualsAsync(parsedToml).ConfigureAwait(false);

    Assert.That(areParsedTomlEquals);
  }

  [Test]
  public async Task SerializeToStringAsync_When_Not_Configured_Table_Added_To_Root_Table_Then_Has_Top_Level_Definition()
  {
    var tomlRootTable = new TomlTable();
    const string notConfiguredTableName = "NotConfiguredTable";
    var notConfiguredTable = new TomlTable(notConfiguredTableName);
    notConfiguredTable.Add("X",  new TomlPrimitiveValue("Y"));
    tomlRootTable.Add(notConfiguredTable.FullName, notConfiguredTable);

    var rawToml = await TomlSerializer.SerializeToStringAsync(tomlRootTable).ConfigureAwait(false);

    await TestContext.Out.WriteLineAsync(rawToml).ConfigureAwait(false);
    var deserializedRootTable = await TomlSerializer.DeserializeAsync(rawToml).ConfigureAwait(false);
    var deserializedNotConfiguredTable = (TomlTable) deserializedRootTable[notConfiguredTableName];
    Assert.That(deserializedNotConfiguredTable.HasTopLevelDefinition, Is.True);
  }

  [Test]
  public async Task SerializeToStringAsync_When_Nested_Not_Configured_Table_Added_To_Root_Table_Then_Has_Top_Level_Definition()
  {
    var tomlRootTable = new TomlTable();
    const string notConfiguredTableName = "NotConfiguredTable";
    const string nestedNotConfiguredTableName = "NestedNotConfiguredTable.ABC";
    var notConfiguredTable = new TomlTable(notConfiguredTableName);
    tomlRootTable.Add(notConfiguredTable);
    var nestedNotConfiguredTable = new TomlTable(nestedNotConfiguredTableName);
    notConfiguredTable.Add(nestedNotConfiguredTable);

    var rawToml = await TomlSerializer.SerializeToStringAsync(tomlRootTable).ConfigureAwait(false);

    await TestContext.Out.WriteLineAsync(rawToml).ConfigureAwait(false);
    var deserializedRootTable = await TomlSerializer.DeserializeAsync(rawToml).ConfigureAwait(false);
    var deserializedNotConfiguredTable = (TomlTable) deserializedRootTable[notConfiguredTableName];
    var deserializedNestedNotConfiguredTable = (TomlTable) deserializedNotConfiguredTable[nestedNotConfiguredTableName];
    Assert.That(deserializedNestedNotConfiguredTable.HasTopLevelDefinition, Is.True);
  }

  [Test]
  public async Task SerializeToStringAsync_When_Nested_Not_Configured_Table_Added_Later_To_Root_Table_Then_Has_Top_Level_Definition()
  {
    var tomlRootTable = new TomlTable();
    const string notConfiguredTableName = "NotConfiguredTable";
    const string nestedNotConfiguredTableName = "NestedNotConfiguredTable.ABC";
    var notConfiguredTable = new TomlTable(notConfiguredTableName);
    var nestedNotConfiguredTable = new TomlTable(nestedNotConfiguredTableName);
    notConfiguredTable.Add(nestedNotConfiguredTable);
    //Add now
    tomlRootTable.Add(notConfiguredTable);

    var rawToml = await TomlSerializer.SerializeToStringAsync(tomlRootTable).ConfigureAwait(false);

    await TestContext.Out.WriteLineAsync(rawToml).ConfigureAwait(false);
    var deserializedRootTable = await TomlSerializer.DeserializeAsync(rawToml).ConfigureAwait(false);
    var deserializedNotConfiguredTable = (TomlTable) deserializedRootTable[notConfiguredTableName];
    var deserializedNestedNotConfiguredTable = (TomlTable) deserializedNotConfiguredTable[nestedNotConfiguredTableName];
    Assert.That(deserializedNestedNotConfiguredTable.HasTopLevelDefinition, Is.True);
  }

  [Test]
  public async Task SerializeToStringAsync_When_Empty_Nested_From_Key_Table_Added_To_Root_Table_Then_HAsFromKeyDefinition_Returns_True()
  {
    var tomlRootTable = new TomlTable();
    const string notConfiguredTableName = "NotConfiguredTable";
    const string nestedNotConfiguredTableName = "NestedNotConfiguredTable.ABC";
    var notConfiguredTable = new TomlTable(notConfiguredTableName);
    var nestedNotConfiguredTable = new TomlTable(nestedNotConfiguredTableName)
    {
      HasFromKeyDefinition = true
    };

    notConfiguredTable.Add(nestedNotConfiguredTable);
    tomlRootTable.Add(notConfiguredTable);

    var rawToml = await TomlSerializer.SerializeToStringAsync(tomlRootTable).ConfigureAwait(false);

    await TestContext.Out.WriteLineAsync(rawToml).ConfigureAwait(false);
    var deserializedRootTable = await TomlSerializer.DeserializeAsync(rawToml).ConfigureAwait(false);
    var deserializedNotConfiguredTable = (TomlTable) deserializedRootTable[notConfiguredTableName];
    var deserializedNestedNotConfiguredTable = (TomlTable) deserializedNotConfiguredTable[nestedNotConfiguredTableName];
    Assert.That(deserializedNestedNotConfiguredTable.HasFromKeyDefinition, Is.True);
  }

  [Test]
  public async Task SerializeToStringAsync_When_CancellationToken_Cancelled_Then_Throws_OperationCanceledException()
  {
    var tomlTable = new TomlTable();
    var cts = new CancellationTokenSource();

    await cts.CancelAsync().ConfigureAwait(false);
    Assert.CatchAsync<OperationCanceledException>(async () =>
    {
      await TomlSerializer.SerializeToStringAsync(tomlTable, cts.Token).ConfigureAwait(false);
    });
  }

  [Test]
  public async Task SerializeToStringAsync_ReadmeMd2()
  {
    var EXPECTED_TOML = """
                        
                        name = "MyApp"
                        version = "1.0.0"
                        """;

    var table = new TomlTable();
    table.Add("name", "MyApp");
    table.Add("version", "1.0.0");

    string tomlOutput = await TomlSerializer.SerializeToStringAsync(table).ConfigureAwait(false);
    Console.WriteLine(tomlOutput);

    Assert.That(tomlOutput, Is.EqualTo(EXPECTED_TOML.ReplaceLineEndings()));
  }

  [Test]
  public async Task SerializeToStringAsync_ReadmeMd3()
  {
    const string EXPECTED_TOML = """
                                 
                                 title = "TOML Example"
                                 version = "1.0.0"
                                 count = 42
                                 enabled = true
                                 [owner]
                                 name = "Tom Preston-Werner"
                                 email = "tom@example.com"
                                 
                                 """;
    var config = new TomlTable();
    // Add simple key-value pairs
    config.Add("title", "TOML Example");
    config.Add("version", "1.0.0");
    config.Add("count", 42);
    config.Add("enabled", true);

    // Add nested tables
    var owner = new TomlTable("owner");
    owner.Add("name", "Tom Preston-Werner");
    owner.Add("email", "tom@example.com");
    config.Add(owner.FullName, owner);

    string output = await TomlSerializer.SerializeToStringAsync(config).ConfigureAwait(false);
    Console.WriteLine(output);

    Assert.That(output, Is.EqualTo(EXPECTED_TOML.ReplaceLineEndings()));
  }

  [Test]
  public async Task SerializeToStringAsync_ReadmeMd4()
  {

    const string EXPECTED_TOML = """
                                 
                                 [app]
                                 name = "MyApp"

                                 [logging]
                                 level = "info"

                                 [server]
                                 port = 8080
                                 hosts = ["localhost", "127.0.0.1", "example.com", ]

                                 """;
    // appsettings.toml
    string configContent = """
                           [app]
                           name = "MyApp"

                           [logging]
                           level = "info"

                           [server]
                           port = 8000
                           hosts = ["localhost", "127.0.0.1", "example.com"]
                           """;

    // Read configuration
    TomlTable config = await TomlSerializer.DeserializeAsync(configContent).ConfigureAwait(false);

    // Extract and cast values to specific types
    var appNameNode = config["app"]["name"];
    var logLevelNode = config["logging"]["level"];
    var portNode = config["server"]["port"];
    var hostsNode = config["server"]["hosts"];

    // Use explicit cast operators to convert to int, string, etc.
    string appName = (string?)appNameNode ?? "DefaultApp";
    int port = ((int?)portNode) ?? 8000;

    Console.WriteLine($"App: {appName}");
    Console.WriteLine($"Port: {port}");

    // Access array elements
    if (hostsNode is TomlArray hostsArray)
    {
      Console.WriteLine("Hosts:");
      foreach (var host in hostsArray)
      {
        Console.WriteLine($"  - {(string?)host}");
      }
    }

    // Modify and save
    config["server"]["port"] = 8080;
    string updatedConfig = await TomlSerializer.SerializeToStringAsync(config).ConfigureAwait(false);
    await TestContext.Out.WriteLineAsync(updatedConfig).ConfigureAwait(false);

    Assert.That(updatedConfig, Is.EqualTo(EXPECTED_TOML.ReplaceLineEndings()));
  }


  //[Test]
  //public async Task ParseFile()
  //{
  //  var tomlStream= File.ReadAllText("test_ini.toml");
  //  var toml = await TomlSerializer.DeserializeAsync(tomlStream).ConfigureAwait(false);
  //  await TestContext.Out.WriteAsync(toml.Keys.First().ToString()).ConfigureAwait(false);
  //}

  //[Test]
  //public void GenerateFile()
  //{
  //  FakeTomlDataGenerator.GenerateBigIniFile("test_ini.toml");
  //}
}