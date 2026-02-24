// See https://aka.ms/new-console-template for more information


using System.Diagnostics;
using System.Text;
using System.Text.Json;
using RStein.TOML;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;
string json = String.Empty;

//Console.WriteLine("xxx");
//Debugger.Launch();

while (Console.ReadLine() is { } line)
{
  json += line + Environment.NewLine;
}

var tomlRootTable = new TomlTable(new TomlKey(TomlTable.ROOT_TABLE_NAME));
Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(json));
Stack<TomlToken> tomlContainers = new();
Stack<bool> tomlTablesCreated = new();
Stack<string> propertyNames = new();
tomlContainers.Push(tomlRootTable);
tomlTablesCreated.Push(true);
TomlValueType? lastDataType = null;
var firstObject = true;

while (reader.Read())
{
  switch (reader.TokenType)
  {
    case JsonTokenType.None:
      break;
    case JsonTokenType.StartObject:
      if (firstObject)
      {
        firstObject = false;
        break;
      }

      var hasName = propertyNames.Count > 0;
      TomlTable? newTable;
      if (tomlContainers.Peek() is TomlArray || tomlContainers.Any(tomlTable => tomlTable is TomlTable{Name: TomlTable.ANONYMOUS_TABLE_NAME}))
      {
        newTable = new TomlInlineTable(new TomlKey(hasName ? propertyNames.Pop() : TomlTable.ANONYMOUS_TABLE_NAME));
      }
      else
      {
        newTable = new TomlTable(new TomlKey(hasName ? propertyNames.Pop() : TomlTable.ANONYMOUS_TABLE_NAME))
        {
          HasTopLevelDefinition = hasName
        };
      }

      addToTomlContainer(tomlContainers, newTable.FullName, newTable);
      tomlContainers.Push(newTable);
      tomlTablesCreated.Push(true);

      break;

    case JsonTokenType.EndObject:
      if (tomlTablesCreated.Pop())
      {
        tomlContainers.Pop();
      }

      break;
    case JsonTokenType.StartArray:
      var newTomlArray = propertyNames.Count > 0
        ? new TomlArray(new TomlKey(propertyNames.Pop()))
        : new TomlArray(new TomlKey(TomlArray.ANONYMOUS_ARRAY_NAME));

      //addToTomlContainer(tomlContainers, newTomlArray.FullName, newTomlArray);
      tomlContainers.Push(newTomlArray);
      break;
    case JsonTokenType.EndArray:
      var arr = (TomlArray) tomlContainers.Pop();
      addToTomlContainer(tomlContainers, arr.FullName, arr);

      break;
    case JsonTokenType.PropertyName:
      var propname = reader.GetString();
     
      propertyNames.Push(propname!);
      break;
    case JsonTokenType.Comment:
      break;
    case JsonTokenType.String:
      var propertyName = propertyNames.Peek();
      if ((propertyName.Equals("value", StringComparison.Ordinal) || propertyName.Equals("type", StringComparison.Ordinal)) &&
          tomlTablesCreated.Peek() &&
          (tomlContainers.Peek().TokenType == TomlTokenType.Table || tomlContainers.Peek().TokenType == TomlTokenType.InlineTable))
      {
        var lastTable = (TomlTable)tomlContainers.Pop();
        if (tomlContainers.Peek() is TomlTable parent)
        {
          parent.Remove(lastTable.FullName);
        }
        else if (tomlContainers.Peek() is TomlArray parentArray)
        {
          parentArray.Remove(lastTable);
        }

        tomlTablesCreated.Pop();
        tomlTablesCreated.Push(false);
        if (!lastTable.Name.Equals(TomlTable.ANONYMOUS_TABLE_NAME, StringComparison.Ordinal))
        {
          var current =  propertyNames.Pop();
          propertyNames.Push(lastTable.FullName.LastKeyPart.RawKey);
          propertyNames.Push(current);
        }
      }

      if (propertyNames.Peek().Equals("type", StringComparison.Ordinal))
      {
        var rawDataType = getTomlValueType(reader.GetString()!);

        lastDataType = Enum.Parse<TomlValueType>(rawDataType, ignoreCase: true);
        propertyNames.Pop();
      }
      else if (propertyNames.Peek().Equals("value", StringComparison.OrdinalIgnoreCase))
      {
        propertyNames.Pop();
        Debug.Assert(lastDataType != null);
        var rawData = reader.GetString();
        var tomlValue = new TomlPrimitiveValue(rawData!, lastDataType.Value);
        addToTomlContainer(tomlContainers, new TomlKey(propertyNames.Count > 0 ? propertyNames.Pop() : "value"), tomlValue);
        lastDataType = null;
      }

      break;
    case JsonTokenType.Number:
    case JsonTokenType.True:
    case JsonTokenType.False:
    case JsonTokenType.Null:
    default:
      throw new ArgumentOutOfRangeException();
  }
}
var toml = await TomlSerializer.SerializeToStringAsync(tomlRootTable).ConfigureAwait(false);
Console.WriteLine(toml);

string getTomlValueType(string rawValue) =>
  rawValue switch
  {
      "date-local" or "datetime" or "datetime-local" or "local-time" or  "time-local" => TomlValueType.DateTime.ToString(),
      "bool" => TomlValueType.Boolean.ToString(),
      _ => rawValue
  };
void addToTomlContainer(Stack<TomlToken> stack,
                        TomlKey tomlKey,
                        TomlToken tomlToken)
{
  switch (stack.Peek())
  {
    case TomlTable parentTomlTable:
    {
      parentTomlTable.Add(tomlKey.LastKeyPart, tomlToken);
      break;
    }
    case TomlArray tomlArray:
    {
      tomlArray.Add(tomlToken);
      break;
    }
    default:
      throw new ArgumentOutOfRangeException();
  }
}

