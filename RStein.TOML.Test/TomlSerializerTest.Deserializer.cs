using System.Text;
using System.Xml.Serialization;

namespace RStein.TOML.Test;

[TestFixture]
internal partial class TomlSerializerTest
{
  [TestCase(null!)]
  public void Deserialize_When_Toml_Argument_Is_Invalid_Then_Throws_ArgumentException(string invalidToml)
  {

    Assert.CatchAsync<ArgumentException>(async () => _ = await TomlSerializer.DeserializeAsync(invalidToml).ConfigureAwait(false));
  }

  [Test]
  public async Task Deserialize_When_Valid_Key_Value_Then_Returns_Expected_Data()
  {
    const string TOML = "key = \"value\"";

    var result = await TomlSerializer.DeserializeAsync(TOML).ConfigureAwait(false);

    Assert.That(result.ContainsKey("key"));
    Assert.That(result["key"].TokenType, Is.EqualTo(TomlTokenType.PrimitiveValue));
    Assert.That(((TomlPrimitiveValue) result["key"]).Value, Is.EqualTo("value"));
  }

  [Test]
  public async Task Deserialize_When_CancellationToken_Cancelled_Then_Throws_OperationCancelledException()
  {
    const string TOML = "key = \"value\"";

    var cts = new CancellationTokenSource();
    await cts.CancelAsync().ConfigureAwait(false);

    Assert.CatchAsync<OperationCanceledException>(async () =>
    {
      _ = await TomlSerializer.DeserializeAsync(TOML, cts.Token).ConfigureAwait(false);
    });
  }

  [Test]
  public void Deserialize_When_Key_Without_Value_Then_Throws_TomlSerializerException()
  {
    const string INVALID_TOML = "key = ";

    Assert.CatchAsync<TomlSerializerException>(async () => _ = await TomlSerializer.DeserializeAsync(INVALID_TOML).ConfigureAwait(false));
  }

  [Test]
  public void Deserialize_When_More_KeyValue_In_One_Line_Then_Throws_TomlSerializerException()
  {
    const string INVALID_TOML = "first = \"Tom\" last = \"Preston-Werner\"";

    Assert.CatchAsync<TomlSerializerException>(async () => _ = await TomlSerializer.DeserializeAsync(INVALID_TOML).ConfigureAwait(false));
  }

  [Test]
  public async Task Deserialize_When_Valid_Key_Value_And_Comment_Then_Returns_Expected_Data()
  {
    const string TOML = "key = \"value\" # my comment ";

    var result = await TomlSerializer.DeserializeAsync(TOML).ConfigureAwait(false);

    Assert.That(result.ContainsKey("key"));
    Assert.That(result["key"].TokenType, Is.EqualTo(TomlTokenType.PrimitiveValue));
    Assert.That(((TomlPrimitiveValue) result["key"]).Value, Is.EqualTo("value"));
  }

  [TestCase("bare_key", "value")]
  [TestCase("bare-key", "value")]
  [TestCase("1234", "value")]
  public async Task Deserialize_When_Valid_Key_Value_Then_Returns_Expected_Data(string expectedKey,
                                                                               string expectedValue)
  {
    string TOML = $"{expectedKey} = \"{expectedValue}\"";

    var result = await TomlSerializer.DeserializeAsync(TOML).ConfigureAwait(false);

    Assert.That(result.ContainsKey(expectedKey));
    Assert.That(result[expectedKey].TokenType, Is.EqualTo(TomlTokenType.PrimitiveValue));
    Assert.That(((TomlPrimitiveValue) result[expectedKey]).Value, Is.EqualTo(expectedValue));
  }

  [TestCase("character encoding", "value")]
  [TestCase("127.0.0.1", "value")]
  [TestCase("character encoding", "value")]
  [TestCase("ʎǝʞ", "value")]
  [TestCase("", "blank")]

  public async Task Deserialize_When_Valid_Double_Quote_Key_Value_Then_Returns_Expected_Data(string expectedKey,
                                                                                            string expectedValue)
  {
    string TOML = $"\"{expectedKey}\" = \"{expectedValue}\"";

    var result = await TomlSerializer.DeserializeAsync(TOML).ConfigureAwait(false);

    Assert.That(result.ContainsKey(expectedKey));
    Assert.That(result[expectedKey].TokenType, Is.EqualTo(TomlTokenType.PrimitiveValue));
    Assert.That(((TomlPrimitiveValue) result[expectedKey]).Value, Is.EqualTo(expectedValue));
  }


  [TestCase("key2", "value")]
  [TestCase("""
            quoted "value"
            """, "value")]
  [TestCase("", "value")]
  public async Task Deserialize_When_Valid_Single_Quote_Key_Value_Then_Returns_Expected_Data(string expectedKey,
                                                                                            string expectedValue)
  {
    string TOML = $"'{expectedKey}' = \"{expectedValue}\"";

    var result = await TomlSerializer.DeserializeAsync(TOML).ConfigureAwait(false);

    Assert.That(result.ContainsKey(expectedKey));
    Assert.That(result[expectedKey].TokenType, Is.EqualTo(TomlTokenType.PrimitiveValue));
    Assert.That(((TomlPrimitiveValue) result[expectedKey]).Value, Is.EqualTo(expectedValue));
  }

  [TestCase("""
            name = "Orange"
            physical.color = "orange"
            physical.shape = "round"
            site."google.com" = true
            """)]
  public async Task Deserialize_When_Valid_Dotted_Key_Value_Then_Returns_Expected_Data(string toml)
  {
    var result = await  TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false);

    Assert.That(result.ContainsKey("name"));
    Assert.That(result["name"].TokenType, Is.EqualTo(TomlTokenType.PrimitiveValue));
    Assert.That(((TomlPrimitiveValue) result["name"]).Value, Is.EqualTo("Orange"));
    Assert.That(result.ContainsKey("physical"));
    Assert.That(result["physical"].TokenType, Is.EqualTo(TomlTokenType.Table));
    var physicalTable = (TomlTable) result["physical"];
    Assert.That(physicalTable.Count, Is.EqualTo(2));
    Assert.That(((TomlPrimitiveValue) physicalTable["color"]).Value, Is.EqualTo("orange"));
    Assert.That(((TomlPrimitiveValue) physicalTable["shape"]).Value, Is.EqualTo("round"));
    Assert.That(result.ContainsKey("site"));
    Assert.That(result["site"].TokenType, Is.EqualTo(TomlTokenType.Table));
    var siteTable = (TomlTable) result["site"];
    Assert.That(siteTable.Count, Is.EqualTo(1));
    Assert.That(((TomlPrimitiveValue) siteTable["google.com"]).Value, Is.EqualTo("true"));
  }

  [TestCase("""
            fruit.name = "banana"     # this is best practice
            fruit. color = "yellow"    # same as fruit.color
            fruit . flavor = "banana"   # same as fruit.flavor
            """)]
  public async Task Deserialize_When_Valid_Dotted_Key_With_Spaces_Value_Then_Returns_Expected_Data(string toml)
  {
    var result = await  TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false);
    Assert.That(result.ContainsKey("fruit"));
    Assert.That(result["fruit"].TokenType, Is.EqualTo(TomlTokenType.Table));
    var fruitTable = (TomlTable) result["fruit"];
    Assert.That(fruitTable.Count, Is.EqualTo(3));
    Assert.That(((TomlPrimitiveValue) fruitTable["name"]).Value, Is.EqualTo("banana"));
    Assert.That(((TomlPrimitiveValue) fruitTable["color"]).Value, Is.EqualTo("yellow"));
    Assert.That(((TomlPrimitiveValue) fruitTable["flavor"]).Value, Is.EqualTo("banana"));
  }

  [TestCase("""
            # DO NOT DO THIS
            name = "Tom"
            name = "Pradyun"
            """)]
  public void Deserialize_When_Duplicate_Keys_Then_Throws_TomlSerializerException(string invalidToml)
  {
    
    Assert.CatchAsync<TomlSerializerException>(async () => _ = await  TomlSerializer.DeserializeAsync(invalidToml).ConfigureAwait(false));
  }

  [TestCase("""
            # THIS WILL NOT WORK
            spelling = "favorite"
            "spelling" = "favourite"
            """)]
  public void Deserialize_When_Duplicate_Quoted_And_Unquoted_Keys_Then_Throws_TomlSerializerException(string invalidToml)
  {

    Assert.CatchAsync<TomlSerializerException>(async () => _ = await  TomlSerializer.DeserializeAsync(invalidToml).ConfigureAwait(false));
  }

  [TestCase("""
            # This makes the key "fruit" into a table.
            fruit.apple.smooth = true

            # So then you can add to the table "fruit" like so:
            fruit.orange = 2
            """)]
  public async Task Deserialize_When_Added_Table_And_Value_To_Root_Table_Then_Returns_Expected_Data(string toml)
  {
    var result = await  TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false);
    Assert.That(result.ContainsKey("fruit"));
    Assert.That(result["fruit"].TokenType, Is.EqualTo(TomlTokenType.Table));
    var fruitTable = (TomlTable) result["fruit"];
    Assert.That(fruitTable.Count, Is.EqualTo(2));
    Assert.That(((TomlPrimitiveValue) fruitTable["orange"]).Value, Is.EqualTo("2"));
    Assert.That(fruitTable["apple"].TokenType, Is.EqualTo(TomlTokenType.Table));
    var appleTable = (TomlTable) fruitTable["apple"];
    Assert.That(appleTable.Count, Is.EqualTo(1));
    Assert.That(((TomlPrimitiveValue) appleTable["smooth"]).Value, Is.EqualTo("true"));
  }

  [TestCase("""
            # THE FOLLOWING IS INVALID

            # This defines the value of fruit.apple to be an integer.
            fruit.apple = 1

            # But then this treats fruit.apple like it's a table.
            # You can't turn an integer into a table.
            fruit.apple.smooth = true
            """)]
  public void ParseAsyncWhen_Int_To_Table_Redefinition_Then_Throws_TomlSerializerException(string invalidToml)
  {
    Assert.CatchAsync<TomlSerializerException>(async () => _ = await  TomlSerializer.DeserializeAsync(invalidToml).ConfigureAwait(false));
  }

  [TestCase("""
            # VALID BUT DISCOURAGED

            apple.type = "fruit"
            orange.type = "fruit"

            apple.skin = "thin"
            orange.skin = "thick"

            apple.color = "red"
            orange.color = "orange"
            """)]
  public async Task Deserialize_When_Dotted_Keys_Defined_Out_Of_Order_Then_Returns_Expected_Data(string toml)
  {
    
    var result = await  TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false);

    Assert.That(result.ContainsKey("apple"));
    Assert.That(result["apple"].TokenType, Is.EqualTo(TomlTokenType.Table));
    var appleTable = (TomlTable) result["apple"];
    Assert.That(appleTable.Count, Is.EqualTo(3));
    Assert.That(((TomlPrimitiveValue) appleTable["type"]).Value, Is.EqualTo("fruit"));
    Assert.That(((TomlPrimitiveValue) appleTable["skin"]).Value, Is.EqualTo("thin"));
    Assert.That(((TomlPrimitiveValue) appleTable["color"]).Value, Is.EqualTo("red"));
    var orangeTable = (TomlTable) result["orange"];
    Assert.That(orangeTable.Count, Is.EqualTo(3));
    Assert.That(((TomlPrimitiveValue) orangeTable["type"]).Value, Is.EqualTo("fruit"));
    Assert.That(((TomlPrimitiveValue) orangeTable["skin"]).Value, Is.EqualTo("thick"));
    Assert.That(((TomlPrimitiveValue) orangeTable["color"]).Value, Is.EqualTo("orange"));
  }


  [TestCase("""
            # RECOMMENDED

            apple.type = "fruit"
            apple.skin = "thin"
            apple.color = "red"

            orange.type = "fruit"
            orange.skin = "thick"
            orange.color = "orange"
            """)]
  public async Task Deserialize_When_Dotted_Keys_Defined_In_Order_Then_Returns_Expected_Data(string toml)
  {
    var result = await  TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false);

    Assert.That(result.ContainsKey("apple"));
    Assert.That(result["apple"].TokenType, Is.EqualTo(TomlTokenType.Table));
    var appleTable = (TomlTable) result["apple"];
    Assert.That(appleTable.Count, Is.EqualTo(3));
    Assert.That(((TomlPrimitiveValue) appleTable["type"]).Value, Is.EqualTo("fruit"));
    Assert.That(((TomlPrimitiveValue) appleTable["skin"]).Value, Is.EqualTo("thin"));
    Assert.That(((TomlPrimitiveValue) appleTable["color"]).Value, Is.EqualTo("red"));
    var orangeTable = (TomlTable) result["orange"];
    Assert.That(orangeTable.Count, Is.EqualTo(3));
    Assert.That(((TomlPrimitiveValue) orangeTable["type"]).Value, Is.EqualTo("fruit"));
    Assert.That(((TomlPrimitiveValue) orangeTable["skin"]).Value, Is.EqualTo("thick"));
    Assert.That(((TomlPrimitiveValue) orangeTable["color"]).Value, Is.EqualTo("orange"));
  }

  [TestCase("""
            3.14159 = "pi"
            """)]
  public async Task Deserialize_When_Bare_Key_With_Digits_And_Dot_Then_Returns_Expected_Data(string toml)
  {
    var result = await  TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false);
    Assert.That(result.ContainsKey("3"));
    Assert.That(result["3"].TokenType, Is.EqualTo(TomlTokenType.Table));
    var table3 = (TomlTable) result["3"];
    Assert.That(table3.ContainsKey("14159"));
    Assert.That(((TomlPrimitiveValue) table3["14159"]).Value, Is.EqualTo("pi"));
  }

  [TestCase("str1",
            """"
            """Roses are red
            Violets are blue"""
            """",
            """
            Roses are red
            Violets are blue
            """)]
  [TestCase("str1",
            """""""
            """"""
            """"""", "")]
  [TestCase("str1", """"
                    """\
                    The quick brown \
                    fox jumps over \
                    the lazy dog.\
                    """
                    """",
            "The quick brown fox jumps over the lazy dog.")]
  [TestCase("str1", """"
                    """The quick brown \
                    
                    
                      fox jumps over \
                        the lazy dog."""
                    """",
            "The quick brown fox jumps over the lazy dog.")]
  [TestCase("str1", """"
                    """Here are two quotation marks: "". Simple enough."""
                    """",
            """"
            Here are two quotation marks: "". Simple enough.
            """")]
  [TestCase("str1", """"
                    """Here are three quotation marks: ""\"."""
                    """",
            """"
            Here are three quotation marks: """.
            """")]
  [TestCase("str1", """""
                    """Here are fifteen quotation marks: ""\"""\"""\"""\"""\"."""
                    """"",
            """"""""""""""""""""""""""""""""
            Here are fifteen quotation marks: """"""""""""""".
            """""""""""""""""""""""""""""""")]
  [TestCase("str1", """""
                    """"This," she said, "is just a pointless statement.""""
                    """"",
            """
            "This," she said, "is just a pointless statement."
            """)]
  public async Task Deserialize_When_Key_And_BasicML_Value_Then_Returns_Expected_Data(string expectedKey,
                                                                                     string multiLineValue,
                                                                                     string expectedValue)
  {
    string TOML = $"{expectedKey} = {multiLineValue}";
    
    var result = await  TomlSerializer.DeserializeAsync(TOML).ConfigureAwait(false);

    Assert.That(result.ContainsKey(expectedKey));
    Assert.That(result[expectedKey].TokenType, Is.EqualTo(TomlTokenType.PrimitiveValue));
    Assert.That(((TomlPrimitiveValue) result[expectedKey]).Value, Is.EqualTo(expectedValue));
  }


  [TestCase("winpath", """'C:\Users\nodejs\templates'""", """C:\Users\nodejs\templates""")]
  [TestCase("winpath2", """'C:\Users\nodejs\templates'""", """C:\Users\nodejs\templates""")]
  [TestCase("quoted", """'Tom "Dubs" Preston-Werner'""", """Tom "Dubs" Preston-Werner""")]
  [TestCase("regex", """'<\i\c*\s*>'""", """<\i\c*\s*>""")]
  [TestCase("str1", """''''That,' she said, 'is still pointless.''''""", "'That,' she said, 'is still pointless.'")]
  [TestCase("apos15", """""""""""""""" '''Here are fifteen quotation marks: """""""""""""""'''"""""""""""""""", """"""""""""""""
                                                                                                                Here are fifteen quotation marks: """""""""""""""
                                                                                                                """""""""""""""")]
  [TestCase("regex2", """ '''I [dw]on't need \d{2} apples'''""", """I [dw]on't need \d{2} apples""")]
  [TestCase("lines", """
                     '''
                     The first newline is
                     trimmed in raw strings.
                        All other whitespace
                        is preserved.
                     '''
                     """,
            """ 
            The first newline is
            trimmed in raw strings.
               All other whitespace
               is preserved.

            """)]
  [TestCase("escaped",
            """""""
            """lol\""""""
            """"""",
            """"
            lol"""
            """")]
  public async Task Deserialize_When_Valid_Key_And_Literal_String_Then_Returns_Expected_Data(string expectedKey,
                                                                                            string literalValue,
                                                                                            string expectedValue)
  {
    string TOML = $"{expectedKey} = {literalValue}";
    
    var result = await  TomlSerializer.DeserializeAsync(TOML).ConfigureAwait(false);

    Assert.That(result.ContainsKey(expectedKey));
    Assert.That(result[expectedKey].TokenType, Is.EqualTo(TomlTokenType.PrimitiveValue));
    Assert.That(((TomlPrimitiveValue) result[expectedKey]).Value, Is.EqualTo(expectedValue));
  }

  [TestCase("apos15 = '''Here are fifteen apostrophes: ''''''''''''''''''")]
  public void Deserialize_When_Literal_String_With_Many_Single_Quites_Then_Throws_TomlSerializerException(string toml)
  {
    

    Assert.CatchAsync<TomlSerializerException>(async () => _ = await  TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false));
  }

  [TestCase("int1", "+99", "+99")]
  [TestCase("int2", "42", "42")]
  [TestCase("int3", "0", "0")]
  [TestCase("int4", "17", "17")]
  [TestCase("int5", "1_000", "1000")]
  [TestCase("int6", "5_349_221", "5349221")]
  [TestCase("int7", "53_49_221", "5349221")]
  [TestCase("int8", "1_2_3_4_5", "12345")]
  [TestCase("int9", "+0", "0")]
  [TestCase("int9", "-0", "0")]
  [TestCase("hex1", "0xDEADBEEF", "0xDEADBEEF")]
  [TestCase("hex2", "0xdeadbeef", "0xdeadbeef")]
  [TestCase("hex3", "0xdead_beef", "0xdeadbeef")]
  [TestCase("oct1", "0o01234567", "0o01234567")]
  [TestCase("oct1", "0o0123_456_7", "0o01234567")]
  [TestCase("oct2", "0o755", "0o755")]
  [TestCase("bin1", "0b11010110", "0b11010110")]
  [TestCase("bin2", "0b1_1010_110", "0b11010110")]
  public async Task Deserialize_When_Int_Value_Then_Returns_Expected_Value(string expectedKey,
                                                                          string literalValue,
                                                                          string expectedValue)
  {
    string TOML = $"{expectedKey} = {literalValue}";
    
    var result = await  TomlSerializer.DeserializeAsync(TOML).ConfigureAwait(false);

    Assert.That(result.ContainsKey(expectedKey));
    Assert.That(result[expectedKey].TokenType, Is.EqualTo(TomlTokenType.PrimitiveValue));
    Assert.That(((TomlPrimitiveValue) result[expectedKey]).Type, Is.EqualTo(TomlValueType.Integer));
    Assert.That(((TomlPrimitiveValue) result[expectedKey]).Value, Is.EqualTo(expectedValue));
  }

  [TestCase("intDecBad = 00")]
  [TestCase("intDecBad = 000")]
  [TestCase("intDecBad = +00")]
  [TestCase("intDecBad = -00")]
  [TestCase("intDecBad = 01")]
  [TestCase("intDecBad = 001")]
  [TestCase("intDecBad = 0009")]
  [TestCase("intDecBad = _9")]
  [TestCase("intDecBad = 9_")]
  [TestCase("intDecBad = _9_")]
  [TestCase("intDecBad = _9__")]
  [TestCase("intHexBad = +0xDEADBEEF")]
  [TestCase("intHexBad = -0xDEADBEEF")]
  [TestCase("intHexBad = 0x_DEADBEEF")]
  [TestCase("intHexBad = 0xDEADBEEF_")]
  [TestCase("intHexBad = 0x_DEADBEEF_")]
  [TestCase("intOctBad = -0o755")]
  [TestCase("intOctBad = +0o755")]
  [TestCase("intOctBad = 0o_755")]
  [TestCase("intOctBad = 0o755_")]
  [TestCase("intOctBad = 0o_755_")]
  [TestCase("intBinBad = +0b11010110")]
  [TestCase("intBinBad = -0b11010110")]
  [TestCase("intBinBad = 0b_11010110")]
  [TestCase("intBinBad = 0b11010110_")]
  [TestCase("intBinBad = 0b_11010110_")]
  [TestCase("intBinBad = 0b_11010110__")]
  [TestCase("intBinBad = 0b__11010110__")]
  public void Deserialize_When_Bad_Int_Value_Then_Throws_TomlException(string toml)
  {
    Assert.CatchAsync<TomlSerializerException>(async () => _ = await TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false));
  }

  [TestCase("flt1", "+1.0", "+1.0")]
  [TestCase("flt2", "3.1415", "3.1415")]
  [TestCase("flt3", "-0.01", "-0.01")]
  [TestCase("flt4", "5e+22", "5e+22")]
  [TestCase("flt5", "1e06", "1e06")]
  [TestCase("flt6", "-2E-2", "-2E-2")]
  [TestCase("flt7", "6.626e-34", "6.626e-34")]
  [TestCase("flt8", "224_617.445_991_228", "224617.445991228")]
  [TestCase("flt9", "0.0", "0.0")]
  [TestCase("flt10", "+0.0", "+0.0")]
  [TestCase("flt11", "-0.0", "-0.0")]
  [TestCase("sf1", "inf", "inf")]
  [TestCase("sf2", "+inf", "+inf")]
  [TestCase("sf3", "-inf", "-inf")]
  [TestCase("sf4", "nan", "nan")]
  [TestCase("sf4", "+nan", "+nan")]
  [TestCase("sf4", "-nan", "-nan")]
  public async Task Deserialize_When_Float_Value_Then_Returns_Expected_Value(string expectedKey,
                                                                            string literalValue,
                                                                            string expectedValue)
  {
    string TOML = $"{expectedKey} = {literalValue}";
    

    var result = await  TomlSerializer.DeserializeAsync(TOML).ConfigureAwait(false);

    Assert.That(result.ContainsKey(expectedKey));
    Assert.That(result[expectedKey].TokenType, Is.EqualTo(TomlTokenType.PrimitiveValue));
    Assert.That(((TomlPrimitiveValue) result[expectedKey]).Type, Is.EqualTo(TomlValueType.Float));
    Assert.That(((TomlPrimitiveValue) result[expectedKey]).Value, Is.EqualTo(expectedValue));
  }

  [TestCase("floatBad = .7")]
  [TestCase("floatBad = 7.")]
  [TestCase("floatBad = 3.e+20")]
  [TestCase("floatBad = 1_e_06_")]
  [TestCase("floatBad = 1_e_06_")]
  [TestCase("floatBad = _1e06")]
  [TestCase("floatBad = Inf")]
  [TestCase("floatBad = iNNf")]
  [TestCase("floatBad = iNf")]
  [TestCase("floatBad = +iNf")]
  [TestCase("floatBad = -iNf")]
  [TestCase("floatBad = INF")]
  [TestCase("floatBad = NAN")]
  [TestCase("floatBad = NaN")]
  [TestCase("floatBad = Nan")]
  [TestCase("floatBad = naN")]
  [TestCase("floatBad = +naN")]
  [TestCase("floatBad = -naN")]
  public void Deserialize_When_Bad_Float_Value_Then_Throws_TomlException(string toml)
  {
    Assert.CatchAsync<TomlSerializerException>(async () => _ = await  TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false));
  }

  [TestCase("bool1", "true", "true")]
  [TestCase("bool2", "false", "false")]
  public async Task Deserialize_When_Bool_Value_Then_Returns_Expected_Value(string expectedKey,
                                                                           string literalValue,
                                                                           string expectedValue)
  {
    string TOML = $"{expectedKey} = {literalValue}";
    
    var result = await  TomlSerializer.DeserializeAsync(TOML).ConfigureAwait(false);

    Assert.That(result.ContainsKey(expectedKey));
    Assert.That(result[expectedKey].TokenType, Is.EqualTo(TomlTokenType.PrimitiveValue));
    Assert.That(((TomlPrimitiveValue) result[expectedKey]).Type, Is.EqualTo(TomlValueType.Boolean));
    Assert.That(((TomlPrimitiveValue) result[expectedKey]).Value, Is.EqualTo(expectedValue));
  }

  [TestCase("boolBad = True")]
  [TestCase("boolBad = tRue")]
  [TestCase("boolBad = False")]
  [TestCase("boolBad = fALse")]
  [TestCase("boolBad = TRUE")]
  [TestCase("boolBad = FALSE")]
  [TestCase("boolBad = notbool")]
  [TestCase("boolBad = fals")]
  [TestCase("boolBad = tru")]
  public void Deserialize_When_Bad_Bool_Value_Then_Throws_TomlException(string toml)
  {
    Assert.CatchAsync<TomlSerializerException>(async () => _ = await  TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false));
  }


  [TestCase("integers", "[ 1, 2, 3 ]")]
  [TestCase("empty", "[]")]
  [TestCase("emptyWithSpaces", "[         ]")]
  [TestCase("colors", """[ "red", "yellow", "green" ]""")]
  [TestCase("string_array", """"[ "all", 'strings', """are the same""", '''type''' ]"""")]
  [TestCase("numbers", "[0.1, 0.2, 0.5, 1, 2, 5]")]
  [TestCase("integers2", """
                         [
                           1, 2, 3
                         ]
                         """)]
  [TestCase("integers2", """
                         [
                           1,
                           2,
                         ]
                         """)]

  public async Task Deserialize_When_Array_Then_Returns_Expected_Value(string expectedKey,
                                                                      string rawArrayValue)
  {
    string TOML = $"{expectedKey} = {rawArrayValue}";
    var expectedArrayValues = rawArrayValue.Split(['[', ',', '\'', '"', ']'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(val => val.Trim()).ToArray();
    
    var rootTable = await  TomlSerializer.DeserializeAsync(TOML).ConfigureAwait(false);

    Assert.That(rootTable.ContainsKey(expectedKey));
    var tomlArray = (TomlArray) rootTable[expectedKey];
    Assert.That(tomlArray.Count, Is.EqualTo(expectedArrayValues.Length));
    Assert.That(tomlArray.Select(token => ((TomlPrimitiveValue) token).Value).SequenceEqual(expectedArrayValues));
  }

  [TestCase("nested_arrays_of_ints", """[ [ 1, 2 ], [3, 4, 5] ]""")]
  [TestCase("nested_mixed_array", """[ [ 1, 2 ], ["a", "b", "c"] ]""")]
  public async Task Deserialize_When_Array_With_Nested_Arrays_Then_Returns_Expected_Value(string expectedKey,
                                                                                         string rawArrayValue)
  {
    string TOML = $"{expectedKey} = {rawArrayValue}";
    var expectedArrayValues = rawArrayValue.Split(['[', ',', '\'', '"', ']'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(val => val.Trim()).ToArray();
    
    var rootTable = await  TomlSerializer.DeserializeAsync(TOML).ConfigureAwait(false);

    Assert.That(rootTable.ContainsKey(expectedKey));
    var tomlArray = (TomlArray) rootTable[expectedKey];
    Assert.That(tomlArray.Count, Is.EqualTo(2));
    var flattenTomlArray = tomlArray.SelectMany(token => token).ToArray();
    Assert.That(flattenTomlArray.Select(token => ((TomlPrimitiveValue) token).Value).SequenceEqual(expectedArrayValues));
  }

  [TestCase("""
            [table-1]
            key1 = "some string"
            key2 = 123

            [table-2]
            key1 = "another string"
            key2 = 456
            """)]
  public async Task Deserialize_When_Valid_Tables_Then_Returns_Expected_Value(string toml)
  {
    
    var rootTable = await  TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false);

    Assert.That(rootTable.Count, Is.EqualTo(2));
    Assert.That(rootTable.ContainsKey("table-1"));
    Assert.That(rootTable.ContainsKey("table-2"));
    var table1 = (TomlTable) rootTable["table-1"];
    Assert.That(table1.Count, Is.EqualTo(2));
    Assert.That(((TomlPrimitiveValue) table1["key1"]).Value, Is.EqualTo("some string"));
    Assert.That(((TomlPrimitiveValue) table1["key2"]).Value, Is.EqualTo("123"));
    var table2 = (TomlTable) rootTable["table-2"];
    Assert.That(table2.Count, Is.EqualTo(2));
    Assert.That(((TomlPrimitiveValue) table2["key1"]).Value, Is.EqualTo("another string"));
    Assert.That(((TomlPrimitiveValue) table2["key2"]).Value, Is.EqualTo("456"));
  }


  [TestCase("""
            [table-1]
            key1 = "some string"
            key2 = 123

            [table-2]
            key1 = "another string"
            key2 = 456
            """)]
  public async Task Deserialize_When_Valid_Tables_And_Using_Stream_Then_Returns_Expected_Value(string toml)
  {
    var tomlStream = new MemoryStream(Encoding.UTF8.GetBytes(toml));
    await using (tomlStream.ConfigureAwait(false))
    {
      tomlStream.Position = 0;

      var rootTable = await TomlSerializer.DeserializeAsync(tomlStream).ConfigureAwait(false);

      Assert.That(rootTable.Count, Is.EqualTo(2));
      Assert.That(rootTable.ContainsKey("table-1"));
      Assert.That(rootTable.ContainsKey("table-2"));
      var table1 = (TomlTable) rootTable["table-1"];
      Assert.That(table1.Count, Is.EqualTo(2));
      Assert.That(((TomlPrimitiveValue) table1["key1"]).Value, Is.EqualTo("some string"));
      Assert.That(((TomlPrimitiveValue) table1["key2"]).Value, Is.EqualTo("123"));
      var table2 = (TomlTable) rootTable["table-2"];
      Assert.That(table2.Count, Is.EqualTo(2));
      Assert.That(((TomlPrimitiveValue) table2["key1"]).Value, Is.EqualTo("another string"));
      Assert.That(((TomlPrimitiveValue) table2["key2"]).Value, Is.EqualTo("456"));
    }
  }

  [TestCase("""
            [dog."tater.man"]
            type.name = "pug"
            """)]
  public async Task Deserialize_When_Valid_Nested_Table_Then_Returns_Expected_Value(string toml)
  {
    
    var rootTable = await  TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false);

    Assert.That(rootTable.Count, Is.EqualTo(1));
    Assert.That(rootTable.ContainsKey("dog"));
    var dogTable = (TomlTable) rootTable["dog"];
    Assert.That(dogTable.Count, Is.EqualTo(1));
    Assert.That(dogTable.ContainsKey("tater.man"));
    var taterManTable = (TomlTable) dogTable["tater.man"];
    Assert.That(taterManTable.Count, Is.EqualTo(1));
    Assert.That(taterManTable.ContainsKey("type"));
    var typeTable = (TomlTable) taterManTable["type"];
    Assert.That(typeTable.Count, Is.EqualTo(1));
    Assert.That(((TomlPrimitiveValue) typeTable["name"]).Value, Is.EqualTo("pug"));
  }

  [TestCase("""
            [a.b.c]            # this is best practice
            [ d.e.f ]          # same as [d.e.f]
            [ g .  h  . i ]    # same as [g.h.i]
            [ j . "ʞ" . 'l' ]
            """)]
  public async Task Deserialize_When_Table_Name_With_Whitespace_Then_Returns_Expected_Value(string toml)
  {

    var rootTable = await  TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false);

    Assert.That(rootTable.ContainsKey("a"));
    Assert.That(rootTable.ContainsKey("d"));
    Assert.That(rootTable.ContainsKey("g"));
    Assert.That(rootTable.ContainsKey("j"));

    var tomlTableA = (TomlTable) rootTable["a"];
    Assert.That(tomlTableA.ContainsKey("b"));
    var tomlTableb = (TomlTable) tomlTableA["b"];
    Assert.That(tomlTableb.ContainsKey("c"));

    var tomlTabled = (TomlTable) rootTable["d"];
    Assert.That(tomlTabled.ContainsKey("e"));
    var tomltablee = (TomlTable) tomlTabled["e"];
    Assert.That(tomltablee.ContainsKey("f"));

    var tomlTableg = (TomlTable) rootTable["g"];
    Assert.That(tomlTableg.ContainsKey("h"));
    var tomlTableh = (TomlTable) tomlTableg["h"];
    Assert.That(tomlTableh.ContainsKey("i"));

    Assert.That(((TomlTable) rootTable["j"]).ContainsKey("ʞ"));
    var tomlTableJ = (TomlTable) rootTable["j"];
    Assert.That(tomlTableJ.ContainsKey("ʞ"));
    var tomlTableʞ = (TomlTable) tomlTableJ["ʞ"];
    Assert.That(tomlTableʞ.ContainsKey("l"));
  }

  [TestCase("""
            # [x] you
            # [x.y] don't
            # [x.y.z] need these
            [x.y.z.w] # for this to work
            [x] # defining a super-table afterward is ok
            """)]
  public async Task Deserialize_When_Super_Table_Defined_After_SubTables_Returns_Expected_Value(string toml)
  {
    
    var rootTable = await  TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false);

    Assert.That(rootTable.ContainsKey("x"));
  }

  [TestCase(""""
            [fruit]
            apple.color = "red"
            apple.taste.sweet = true

            [fruit.apple]  # INVALID
            """")]
  [TestCase("""
            [fruit]
            apple.color = "red"
            apple.taste.sweet = true

            [fruit.apple.taste]  # INVALID
            """)]
  [TestCase("""
            [fruit]
            apple = "red"

            [fruit]
            orange = "orange"
            """)]
  [TestCase("""
            [fruit]
            apple = "red"

            [fruit.apple]
            texture = "smooth"
            """)]
  public void Deserialize_When_Invalid_Table_Definitions_Then_Throws_TomlSerializerException(string toml)
  {
    
    Assert.CatchAsync<TomlSerializerException>(async () => _ = await  TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false));
  }

  [TestCase("""
            name = { first = "Tom", last = "Preston-Werner" }
            """)]
  public async Task Deserialize_When_Valid_Inline_Table_Then_Returns_Expected_Data(string toml)
  {
    
    var rootTable = await  TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false);

    Assert.That(rootTable.ContainsKey("name"));
    Assert.That(rootTable.Count, Is.EqualTo(1));
    var nameTable = (TomlTable) rootTable["name"];
    Assert.That(((TomlPrimitiveValue) nameTable["first"]).Value, Is.EqualTo("Tom"));
    Assert.That(((TomlPrimitiveValue) nameTable["last"]).Value, Is.EqualTo("Preston-Werner"));
  }

  [TestCase("""
            name = {}
            """)]

  [TestCase("""
            name = {        }
            """)]
  public async Task Deserialize_When_Empty_InlineArray_Then_Returns_Expected_data(string toml)
  {
    var rootTable = await  TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false);

    Assert.That(rootTable.ContainsKey("name"));
    Assert.That(rootTable.Count, Is.EqualTo(1));
    var nameTable = rootTable["name"] as TomlTable;
    Assert.That(nameTable, Is.Empty);
  }

  [TestCase("""
            animal = { type.name = "pug", type.x = "abx" }
            """)]
  public async Task Deserialize_When_Valid_Nested_Inline_Table_Then_Returns_Expected_Data(string toml)
  {
    var rootTable = await  TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false);

    Assert.That(rootTable.ContainsKey("animal"));
    Assert.That(rootTable.Count, Is.EqualTo(1));
    var animalTable = (TomlTable) rootTable["animal"];
    Assert.That(animalTable.Count, Is.EqualTo(1));
    var typeTable = (TomlTable) animalTable["type"];
    Assert.That(typeTable.Count, Is.EqualTo(2));
    Assert.That(((TomlPrimitiveValue) typeTable["name"]).Value, Is.EqualTo("pug"));
  }

  [TestCase("""
            [product]
            type = { name = "Nail" }
            type.edible = false  # INVALID
            """)]
  [TestCase("""
            [product]
            type.name = "Nail"
            type = { edible = false }  # INVALID
            """)]
  [TestCase("""
            animal = { type.name = "pug", } #trailing comma not allowed
            """)]
  [TestCase("""
            animal = {,} #empty array with comma
            """)]
  public void Deserialize_When_Invalid_Inline_Table_Definitions_In_Toml_1_0_Then_Throws_TomlSerializerException(string toml)
  {
    var toml10Settings = new TomlSettings(TomlVersion.Toml10);
    Assert.CatchAsync<TomlSerializerException>(async () => _ = await  TomlSerializer.DeserializeAsync(toml, toml10Settings).ConfigureAwait(false));
  }

  [TestCase("""
            [product]
            type = { name = "Nail" }
            type.edible = false  # INVALID
            """)]
  [TestCase("""
            [product]
            type.name = "Nail"
            type = { edible = false }  # INVALID
            """)]
  [TestCase("""
            animal = {,} #empty array with comma
            """)]
  public void Deserialize_When_Invalid_Inline_Table_Definitions_In_Toml_1_1_Then_Throws_TomlSerializerException(string toml)
  {
    var toml11Settings = new TomlSettings(TomlVersion.Toml11);
    Assert.CatchAsync<TomlSerializerException>(async () => _ = await  TomlSerializer.DeserializeAsync(toml, toml11Settings).ConfigureAwait(false));
  }

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
  public async Task Deserialize_When_Different_Table_Order_Then_Returns_Expected_data(string toml)
  {

    var rootTable = await  TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false);

    Assert.That(rootTable.Count, Is.EqualTo(2));
    Assert.That(rootTable.ContainsKey("fruit"));
    Assert.That(rootTable.ContainsKey("animal"));
    var fruitTable = (TomlTable) rootTable["fruit"];
    Assert.That(fruitTable.Count, Is.EqualTo(2));
    Assert.That(fruitTable.ContainsKey("orange"));
    var orangeTable = fruitTable["orange"] as TomlTable;
    Assert.That(orangeTable, Is.Empty);
    var appleTable = fruitTable["apple"] as TomlTable;
    Assert.That(appleTable, Is.Empty);
  }

  [TestCase("""
            # Top-level table begins.
            name = "Fido"
            breed = "pug"

            # Top-level table ends.
            [owner]
            name = "Regina Dogman"
            member_since = 1999
            """)]
  public async Task Deserialize_When_Root_Table_And_Named_Table_Then_Returns_Expected_data(string toml)
  {
    var rootTable = await  TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false);

    Assert.That(rootTable.Count, Is.EqualTo(3));
    Assert.That(((TomlPrimitiveValue) rootTable["name"]).Value, Is.EqualTo("Fido"));
    Assert.That(((TomlPrimitiveValue) rootTable["breed"]).Value, Is.EqualTo("pug"));
    var ownerTable = (TomlTable) rootTable["owner"];
    Assert.That(((TomlPrimitiveValue) ownerTable["name"]).Value, Is.EqualTo("Regina Dogman"));
    Assert.That(((TomlPrimitiveValue) ownerTable["member_since"]).Value, Is.EqualTo("1999"));

  }

  [TestCase("""
            fruit.apple.color = "red"
            # Defines a table named fruit
            # Defines a table named fruit.apple

            fruit.apple.taste.sweet = true
            # Defines a table named fruit.apple.taste
            # fruit and fruit.apple were already created
            """)]
  public async Task Deserialize_When_Tables_Defined_In_Keys_Then_Returns_Expected_data(string toml)
  {
    var rootTable = await  TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false);

    Assert.That(rootTable.Count, Is.EqualTo(1));
    var fruitTable = (TomlTable) rootTable["fruit"];
    Assert.That(fruitTable.Count, Is.EqualTo(1));
    var appleTable = (TomlTable) fruitTable["apple"];
    Assert.That(appleTable.Count, Is.EqualTo(2));
    Assert.That(((TomlPrimitiveValue) appleTable["color"]).Value, Is.EqualTo("red"));
    var tasteTable = (TomlTable) appleTable["taste"];
    Assert.That(((TomlPrimitiveValue) tasteTable["sweet"]).Value, Is.EqualTo("true"));
  }

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
  public async Task Deserialize_When_Valid_Array_Of_Table_Then_Returns_Expected_data(string toml)
  {

    var rootTable = await  TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false);

    Assert.That(rootTable.Count, Is.EqualTo(1));
    var productsArray = (TomlArray) rootTable["products"];
    Assert.That(productsArray.Count, Is.EqualTo(3));
    var product0 = (TomlTable) productsArray[0];
    Assert.That(product0.Count, Is.EqualTo(2));
    Assert.That(((TomlPrimitiveValue) product0["name"]).Value, Is.EqualTo("Hammer"));
    Assert.That(((TomlPrimitiveValue) product0["sku"]).Value, Is.EqualTo("738594937"));
    var product1 = (TomlTable) productsArray[1];
    Assert.That(product1.Count, Is.EqualTo(0));
    var product2 = (TomlTable) productsArray[2];
    Assert.That(product2.Count, Is.EqualTo(3));
    Assert.That(((TomlPrimitiveValue) product2["name"]).Value, Is.EqualTo("Nail"));
    Assert.That(((TomlPrimitiveValue) product2["sku"]).Value, Is.EqualTo("284758393"));
    Assert.That(((TomlPrimitiveValue) product2["color"]).Value, Is.EqualTo("gray"));
  }

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
  public async Task Deserialize_When_Valid_Array_Of_Table_With_Nested_Array_Of_Tables_Then_Returns_Expected_data(string toml)
  {
    
    var rootTable = await  TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false);

    Assert.That(rootTable.Count, Is.EqualTo(1));
    var fruitsArray = (TomlArray) rootTable["fruits"];
    Assert.That(fruitsArray.Count, Is.EqualTo(2));
    var fruit0Table = (TomlTable) fruitsArray[0];
    Assert.That(fruit0Table.Count, Is.EqualTo(3));
    Assert.That(((TomlPrimitiveValue) fruit0Table["name"]).Value, Is.EqualTo("apple"));
    var fruit0Physical0Table = (TomlTable) fruit0Table["physical"];
    Assert.That(fruit0Physical0Table.Count, Is.EqualTo(2));
    Assert.That(((TomlPrimitiveValue) fruit0Physical0Table["color"]).Value, Is.EqualTo("red"));
    Assert.That(((TomlPrimitiveValue) fruit0Physical0Table["shape"]).Value, Is.EqualTo("round"));
    var fruit0VarietiesArray = (TomlArray) fruit0Table["varieties"];
    Assert.That(fruit0VarietiesArray.Count, Is.EqualTo(2));
    var fruit0Varieties0 = (TomlTable) fruit0VarietiesArray[0];
    Assert.That(((TomlPrimitiveValue) fruit0Varieties0["name"]).Value, Is.EqualTo("red delicious"));
    var fruit0Varieties1 = (TomlTable) fruit0VarietiesArray[1];
    Assert.That(((TomlPrimitiveValue) fruit0Varieties1["name"]).Value, Is.EqualTo("granny smith"));
    var fruit1Table = (TomlTable) fruitsArray[1];
    Assert.That(fruit1Table.Count, Is.EqualTo(2));
    Assert.That(((TomlPrimitiveValue) fruit1Table["name"]).Value, Is.EqualTo("banana"));
    var fruit1VarietiesArray = (TomlArray) fruit1Table["varieties"];
    Assert.That(fruit1VarietiesArray.Count, Is.EqualTo(1));
    var fruit1Varieties0 = (TomlTable) fruit1VarietiesArray[0];
    Assert.That(((TomlPrimitiveValue) fruit1Varieties0["name"]).Value, Is.EqualTo("plantain"));
  }


  [TestCase("""
            # INVALID TOML DOC
            [fruit.physical]  # subtable, but to which parent element should it belong?
            color = "red"
            shape = "round"
            
            [[fruit]]  # parser must throw an error upon discovering that "fruit" is
                       # an array rather than a table
            name = "apple"
            """)]
  [TestCase("""
            # INVALID TOML DOC
            fruits = []
            
            [[fruits]] # Not allowed
            """)]
  [TestCase("""
            # INVALID TOML DOC
            [[fruits]]
            name = "apple"
            
            [[fruits.varieties]]
            name = "red delicious"
            
            # INVALID: This table conflicts with the previous array of tables
            [fruits.varieties]
            name = "granny smith"
            
            [fruits.physical]
            color = "red"
            shape = "round"
            """)]
  [TestCase("""
            [[fruits]]
            name = "apple"
            
            [[fruits.varieties]]
            name = "red delicious"
            
            [fruits.physical]
            color = "red"
            shape = "round"
            
            # INVALID: This array of tables conflicts with the previous table
            [[fruits.physical]]
            color = "green"
            """)]
  public void Deserialize_When_Invalid_Array_Of_Tables_Then_Throws_TomlSerializerException(string toml)
  {
    Assert.CatchAsync<TomlSerializerException>(async () => _ = await  TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false));
  }

  [TestCase("""
            points = [ { x = 1, y = 2, z = 3 },
            { x = 7, y = 8, z = 9 },
            { x = 2, y = 4, z = 8 } ]
            """)]
  public async Task Deserialize_When_Valid_Inline_Array_Of_Tables_Then_Returns_Expected_data(string toml)
  {

    var rootTable = await  TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false);

    Assert.That(rootTable.Count, Is.EqualTo(1));
    Assert.That(rootTable.ContainsKey("points"));
    var points = (TomlArray) rootTable["points"];
    Assert.That(points.TokenType == TomlTokenType.Array);
    var point0 = (TomlTable) points[0];
    Assert.That(((TomlPrimitiveValue)point0["x"]).Value, Is.EqualTo("1"));
    Assert.That(((TomlPrimitiveValue)point0["y"]).Value, Is.EqualTo("2"));
    Assert.That(((TomlPrimitiveValue)point0["z"]).Value, Is.EqualTo("3"));
    var point1 = (TomlTable) points[1];
    Assert.That(((TomlPrimitiveValue)point1["x"]).Value, Is.EqualTo("7"));
    Assert.That(((TomlPrimitiveValue)point1["y"]).Value, Is.EqualTo("8"));
    Assert.That(((TomlPrimitiveValue)point1["z"]).Value, Is.EqualTo("9"));
    var point2 = (TomlTable) points[2];
    Assert.That(((TomlPrimitiveValue)point2["x"]).Value, Is.EqualTo("2"));
    Assert.That(((TomlPrimitiveValue)point2["y"]).Value, Is.EqualTo("4"));
    Assert.That(((TomlPrimitiveValue)point2["z"]).Value, Is.EqualTo("8"));
  }


  [TestCase("odt1", "1979-05-27T07:32:00Z", "1979-05-27T07:32:00Z", TomlDataType.OffsetDateTime)]
  [TestCase("odt2", "1979-05-27T00:32:00-07:00", "1979-05-27T00:32:00-07:00", TomlDataType.OffsetDateTime)]
  [TestCase("odt3", "1979-05-27T00:32:00.999999-07:00", "1979-05-27T00:32:00.999999-07:00", TomlDataType.OffsetDateTime)]
  [TestCase("odt4", "1979-05-27 07:32:00Z", "1979-05-27 07:32:00Z", TomlDataType.OffsetDateTime)]
  [TestCase("ldt1", "1979-05-27T07:32:00", "1979-05-27T07:32:00", TomlDataType.LocalDateTime)]
  [TestCase("ldt2", "1979-05-27T00:32:00.999999", "1979-05-27T00:32:00.999999", TomlDataType.LocalDateTime)]
  [TestCase("ld1", "1979-05-27", "1979-05-27", TomlDataType.LocalDate),]
  [TestCase("lt1", "07:32:00", "07:32:00", TomlDataType.LocalTime)]
  [TestCase("lt2", "00:32:00.999999", "00:32:00.999999", TomlDataType.LocalTime)]
  public async Task Deserialize_When_DateTime_Value_Then_Returns_Expected_Value(string expectedKey,
                                                                                string literalValue,
                                                                                string expectedValue,
                                                                                TomlDataType expectedSubtype)
  {
    string TOML = $"{expectedKey} = {literalValue}";
    
    var result = await  TomlSerializer.DeserializeAsync(TOML).ConfigureAwait(false);

    Assert.That(result.ContainsKey(expectedKey));
    Assert.That(result[expectedKey].TokenType, Is.EqualTo(TomlTokenType.PrimitiveValue));
    Assert.That(((TomlPrimitiveValue)result[expectedKey]).Type, Is.EqualTo(TomlValueType.DateTime));
    Assert.That(((TomlPrimitiveValue)result[expectedKey]).SubType, Is.EqualTo(expectedSubtype));
    Assert.That(((TomlPrimitiveValue)result[expectedKey]).Value, Is.EqualTo(expectedValue));
  }

  [TestCase("odt1", "1979-13-27T07:32:00Z")]
  [TestCase("odt2", "1979-05-32T00:32:00-07:00")]
  [TestCase("odt3", "79-05-27T00:32:00.999999-07:00")]
  [TestCase("odt4", "1979-05-27 25:32:00Z")]
  [TestCase("ldt1", "1979-05-27-07:32:00")]
  [TestCase("ldt2", "1979-05-27T00:60:00.999999")]
  [TestCase("ld1", "1979-05--1")]
  [TestCase("lt1", "07:32:")]
  [TestCase("lt2", "00:32:00.")]
  public void Deserialize_When_DateTime_Value_Invalid_Then_Throws_TomlSerializerException(string key, string invalidValue)
   
  {
    string TOML = $"{key} = {invalidValue}";
    
    Assert.CatchAsync<TomlSerializerException>(async () => {_ = await  TomlSerializer.DeserializeAsync(TOML).ConfigureAwait(false);});
  }

  [Test]
  public async Task DeserializeAsync_ReadmeMdTest1()
  {
    string tomlContent = """
                         [package]
                         name = "MyApp"
                         version = "1.0.0"
                         """;

    using var cts = new CancellationTokenSource();
    var table = await TomlSerializer.DeserializeAsync(tomlContent, cts.Token).ConfigureAwait(false);
    Assert.That((string)table["package"]["name"]!, Is.EqualTo("MyApp"));
    Assert.That((string)table["package"]["version"]!, Is.EqualTo("1.0.0"));
  }

}
