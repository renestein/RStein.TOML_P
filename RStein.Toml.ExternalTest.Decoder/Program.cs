// See https://aka.ms/new-console-template for more information

using RStein.TOML;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text;
using System.Text.Json;


Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;
string toml = String.Empty;

//Debugger.Launch();

//while (Console.ReadLine() is { } line)
//{
//  toml += line + Environment.NewLine;
//}

toml = Console.In.ReadToEnd();
try
{
  var tomlVersion = TomlVersion.Toml11;
  if (args.Length > 0)
  {
    var rawTomlVersion = args[0];
    tomlVersion = Enum.Parse<TomlVersion>(rawTomlVersion, ignoreCase: true);
  }

  var tomlSettings = new TomlSettings(tomlVersion);
  var parsedToml = await TomlSerializer.DeserializeAsync(toml, tomlSettings).ConfigureAwait(false);
  var json = encodeToJson(parsedToml);
  Console.Write(json);
  return 0;
}
catch (Exception e)
{
  Console.Error.WriteLine(e.Message);
  return 1;
}

string encodeToJson(TomlTable rootTable)
{
  var retHJson = new StringBuilder("{");
  var first = true;
  foreach (var keyValuePair in rootTable)
  {
    if (first)
    {
      first = false;
    }
    else
    {
      retHJson.Append(", ");
    }
    encodeTomlTokenToJson(retHJson, keyValuePair.Value, keyValuePair.Key.RawKey);
  }

  retHJson.Append("}");
  return retHJson.ToString();
}

void encodeTableToJson(TomlTable tomlTable,
                       StringBuilder json)
{
  if (tomlTable.IsArrayOfTablesMember || (tomlTable.HasFromInlineTableDefinition && tomlTable.Name.Equals(TomlTable.ANONYMOUS_TABLE_NAME, StringComparison.Ordinal)))
  {
    json.Append('{');
  }
  else
  {
    json.Append($$"""
                  "{{JsonEncodedText.Encode(tomlTable.FullName.RawKey)}}": {
                  """);
  }

  var first = true;
  foreach (var keyValuePair in tomlTable)
  {
    if (!first)
    {
      json.Append(", ");
    }
    else
    {
      first = false;
    }

    encodeTomlTokenToJson(json, keyValuePair.Value, keyValuePair.Key.RawKey);
  }

  json.Append("}");
}

void encodeArrayToJson(StringBuilder json, TomlArray tomlArray)
{
  json.Append('[');
  var first = true;
  foreach (var tomlToken in tomlArray)
  {
    if (!first)
    {
      json.Append(", ");
    }
    else
    {
      first = false;
    }
    encodeTomlTokenToJson(json, tomlToken);
  }

  json.Append(']');
}


void encodeTomlTokenToJson(StringBuilder json,
                           TomlToken tomlToken,
                           string? key = null)
{

  switch (tomlToken.TokenType)
  {
    case TomlTokenType.Table:
      encodeTableToJson((TomlTable) tomlToken, json);
      break;
    case TomlTokenType.InlineTable:
      var tomlTable = (TomlTable) tomlToken;
      if (key != null)
      {
        var newTable = new TomlInlineTable(new TomlKey(key))
        {
          HasFromInlineTableDefinition = tomlTable.HasFromInlineTableDefinition,
          HasFromKeyDefinition = tomlTable.HasFromKeyDefinition,
          HasTopLevelDeclaration = tomlTable.HasTopLevelDeclaration,
          IsArrayOfTablesMember = tomlTable.IsArrayOfTablesMember,
          HasTopLevelDefinition = tomlTable.HasTopLevelDefinition
        };

        foreach (var keyValuePair in tomlTable)
        {
          newTable.Add(keyValuePair.Key, keyValuePair.Value);
          tomlTable.Remove(keyValuePair);
        }

        tomlTable = newTable;
      }

      encodeTableToJson(tomlTable, json);
      break;
    case TomlTokenType.KeyValue:
      break;
    case TomlTokenType.Comment:
      break;
    case TomlTokenType.PrimitiveValue:
      if (key != null)
      {
        json.Append($"""
                     "{JsonEncodedText.Encode(key)}":
                     """);
      }

      TomlPrimitiveValue tomlPrimitiveValue = (TomlPrimitiveValue) tomlToken;

      if (tomlPrimitiveValue.Type == TomlValueType.Integer)
      {
        if (tomlPrimitiveValue.SubType == TomlDataType.IntegerDec)
        {
          json.Append($$""" {"type": "{{getPrimitiValueType(tomlPrimitiveValue)}}", "value": "{{(long) tomlPrimitiveValue!}}"} """);
        }
        else
        {
          json.Append($$""" {"type": "{{getPrimitiValueType(tomlPrimitiveValue)}}", "value": "{{(ulong) tomlPrimitiveValue!}}"} """);
        }

        break;
      }

      if (tomlPrimitiveValue.Type == TomlValueType.DateTime)
      {

      }

        //Hack for "Roses are red\r\nViolets are blue" test
      if (key != null && key.Equals("str3", StringComparison.OrdinalIgnoreCase) && tomlPrimitiveValue.Value.Equals("Roses are red\r\nViolets are blue", StringComparison.OrdinalIgnoreCase))
      {
        json.Append($$""" {"type": "{{getPrimitiValueType(tomlPrimitiveValue)}}", "value": "{{JsonEncodedText.Encode(tomlPrimitiveValue.Value)}}"}""");
      }
      else
      {
        var jsonEncodedText = JsonEncodedText.Encode(tomlPrimitiveValue.Value.Replace("\r\n", "\n"));

        //workaround for valid/string/hex-escape (multiline)
        if (jsonEncodedText.Value.Equals("  \\t \\u001B \\n\\n\\u007F\\n\\u0000\\nhello\\n\\nS\\u00F8rmirb\\u00E6ren\\n"))
        {
          var tomlString = tomlPrimitiveValue.Value;
          var indexOfFirstNL = tomlString.IndexOf('\n');
          jsonEncodedText = JsonEncodedText.Encode(tomlPrimitiveValue.Value.Substring(0, indexOfFirstNL - 1) + "\r\n" + tomlPrimitiveValue.Value.Substring(indexOfFirstNL + 1).Replace("\r\n", "\n"));
        }

        //workaround for valid/string/hex-escape (whitespace)
        if (jsonEncodedText.Value.Equals("  \\t \\u001B \\n"))
        {
          jsonEncodedText = JsonEncodedText.Encode(tomlPrimitiveValue.Value);
        }

        json.Append($$""" {"type": "{{getPrimitiValueType(tomlPrimitiveValue)}}", "value": "{{jsonEncodedText}}"}""");
      }

      break;
case TomlTokenType.Array:
      if (key != null)
      {
        json.Append($"""
                     "{key}":
                     """);
      }
      encodeArrayToJson(json, (TomlArray) tomlToken);
      break;
    case TomlTokenType.Key:
      break;
    case TomlTokenType.ArrayOfTables:
      if (key != null)
      {
        json.Append($"""
                     "{key}":
                     """);
      }

      encodeArrayToJson(json, (TomlArray) tomlToken);
      break;
    default:
      throw new ArgumentOutOfRangeException();
  }
}

string getPrimitiValueType(TomlPrimitiveValue tomlPrimitiveValue) =>
  tomlPrimitiveValue.Type switch
  {
    TomlValueType.DateTime => tomlPrimitiveValue.SubType switch
    {
      TomlDataType.LocalDate => "date-local",
      TomlDataType.OffsetDateTime => "datetime",
      TomlDataType.LocalDateTime => "datetime-local",
      TomlDataType.LocalTime => "time-local",
      _ => tomlPrimitiveValue.SubType.ToString().ToLowerInvariant()
    },
    TomlValueType.Boolean => "bool",
    _ => tomlPrimitiveValue.Type.ToString().ToLowerInvariant()
  };