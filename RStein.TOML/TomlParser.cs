using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RStein.TOML
{
  internal static class TomlParser
  {
    private const string UNEXPECTED_END_OF_TOML_DOCUMENT_ERROR = "Unexpected end of TOML document.";
    private const string UNEXPECTED_END_OF_ESCAPED_CHAR_ERROR = "Unexpected end of escaped char.";
    private const int UNLIMITED = -1;
    private const string INVALID_TOML_VALUE_ERROR = "Invalid TOML value.";
    private const string INVALID_TOML_ARRAY_DEFINITION_ERROR = "Invalid TOML array definition.";
    private const string INVALID_TOML_INLINE_TABLE_DEFINITION_ERROR = "Invalid TOML inline table definition.";
    private const string INVALID_TABLE_DEFINITION_ERROR = "Invalid table definition.";
    private const string INVALID_ARRAY_TABLES_DEFINITION_ERROR = "Invalid array of tables definition.";
    private const string PARSING_FAILED_GENERAL_ERROR = "Parsing failed. See inner exception for details.";
    private const string INVALID_BASIC_ML_STRING_DEFINITION_ERROR = "Invalid basic string on";
    private const string INVALID_LITERAL_ML_STRING_DEFINITION_ERROR = "Invalid literal string definition";
    private const int MAX_SINGLE_QUOTES_IN_LITERAL_ML_STRING = 2;
    private const int NUMBER_OF_OPENING_CLOSE_SINGLE_QUOTES_IN_LITERAL_ML_STRING = 3;
    private const string WINDOWS_NEW_LINE = "\r\n";
    private const string DEFAULT_SECONDS_IN_TIME = ":00";

    public static async ValueTask<TomlTable> ParseAsync(string toml,
                                                        TomlSettings? tomlSettings,
                                                        CancellationToken cancellationToken)
    {

      if (toml == null)
      {
        throw new ArgumentNullException(nameof(toml));
      }


      tomlSettings = tomlSettings ?? TomlSettings.Default;
      var parserState = new ParserState<StringInputReader>(new StringInputReader(toml), tomlSettings)
      {
        CancellationToken = cancellationToken
      };

      return await parseToml(parserState).ConfigureAwait(false);
    }

    public static async ValueTask<TomlTable> ParseAsync(Stream tomlStream,
                                                        TomlSettings tomlSettings,
                                                        CancellationToken cancellationToken)
    {
      if (tomlStream == null)
      {
        throw new ArgumentNullException(nameof(tomlStream));
      }

      tomlSettings = tomlSettings ?? TomlSettings.Default;
      using (var reader = new StreamReader(tomlStream, Encoding.UTF8))
      {
        var parserState = new ParserState<TextInputReader>(new TextInputReader(reader), tomlSettings)
        {
          CancellationToken = cancellationToken
        };

        return await parseToml(parserState).ConfigureAwait(false);
      }
    }

    private static async ValueTask<TomlTable> parseToml<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      try
      {
        await advance(parserState).ConfigureAwait(false);
        while (!parserState.Eof)
        {
          parserState.CancellationToken.ThrowIfCancellationRequested();
          await skipWhitespacesAndNewLines(parserState).ConfigureAwait(false);
          if (parserState.Eof)
          {
            continue;

          }

          var expression = await parseExpression(parserState).ConfigureAwait(false);

          switch (expression)
          {
            case TomlTable table:
              break;
            case TomlArray arrayOfTables:
              if (!arrayOfTables.FullName.IsDottedKey && !parserState.RootTomlTable.ContainsKey(arrayOfTables.Name))
              {
                parserState.RootTomlTable.AddInternal(arrayOfTables.FullName.LastKeyPart, arrayOfTables);
              }

              break;
            case TomlKeyValue tomlKeyValue:
              parserState.CurrentTomlTable.AddInternal(tomlKeyValue.Key.LastKeyPart, tomlKeyValue.Value);
              parserState.TryRestoreLastScopedTomlTable();
              break;
            case TomlComment _:
              break;
            default:
              throw new ArgumentOutOfRangeException();
          }
        }

        return parserState.RootTomlTable;
      }
      catch (TomlSerializerException)
      {
        throw;
      }
      catch (OperationCanceledException)
      {
        throw;
      }
      catch (Exception e)
      {
        throw new TomlSerializerException(PARSING_FAILED_GENERAL_ERROR, e);
      }
    }

    private static async ValueTask<TomlExpression> parseExpression<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      await skipWhitespacesAndNewLines(parserState).ConfigureAwait(false);
      throwIfEof(parserState);
      TomlExpression expression;
      switch (parserState.CurrentChar)
      {
        case '#':
          {
            expression = await parseComment(parserState).ConfigureAwait(false);
            break;
          }
        case '[':
          {
            var peekChar = peek(parserState);
            var parsingArrayOfTables = peekChar == '[';
            parserState.CurrentTomlTable = parserState.RootTomlTable;

            expression = parsingArrayOfTables ? (TomlExpression)await parseArrayOfTables(parserState).ConfigureAwait(false)
                                               : await parseStandardTable(parserState).ConfigureAwait(false);

            break;
          }
        default:
          {
            expression = await parseKeyValueExpression(parserState).ConfigureAwait(false);
            break;
          }
      }

      return expression;
    }

    private static async ValueTask<TomlArray> parseArrayOfTables<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      try
      {
        parserState.KeyParent = TomlTokenType.Array;
        if (parserState.CurrentChar != '[')
        {
          throwTomlSerializerException(parserState, INVALID_ARRAY_TABLES_DEFINITION_ERROR);
        }

        await advance(parserState).ConfigureAwait(false);
        throwIfEof(parserState);
        if (parserState.CurrentChar != '[')
        {
          throwTomlSerializerException(parserState, INVALID_ARRAY_TABLES_DEFINITION_ERROR);
        }

        await advance(parserState).ConfigureAwait(false);
        throwIfEof(parserState);

        var (key, parentTable) = await parseKey(parserState).ConfigureAwait(false);
        var lastKey = key.LastKeyPart;
        await skipWhiteSpaces(parserState).ConfigureAwait(false);
        if (parserState.CurrentChar != ']')
        {
          throwTomlSerializerException(parserState, INVALID_TABLE_DEFINITION_ERROR);
        }

        await advance(parserState).ConfigureAwait(false);
        throwIfEof(parserState);
        if (parserState.CurrentChar != ']')
        {
          throwTomlSerializerException(parserState, INVALID_ARRAY_TABLES_DEFINITION_ERROR);
        }


        var peekChar = peek(parserState);
        Debug.Assert(peekChar != null);
        if (isWhiteSpace(peekChar.Value))
        {
          await advance(parserState).ConfigureAwait(false);
          await skipWhiteSpaces(parserState).ConfigureAwait(false);
          peekChar = parserState.CurrentChar;
        }

        if (!parserState.Eof)
        {
          if (peekChar != '#' && !isNewLineChar(peekChar.Value))
          {
            throwTomlSerializerException(parserState, INVALID_ARRAY_TABLES_DEFINITION_ERROR);
          }

          if (peekChar == '#')
          {
            _ = await parseComment(parserState).ConfigureAwait(false);
          }
        }

        if (parentTable.TryGetValue(lastKey, out var token))
        {
          if (token.TokenType != TomlTokenType.ArrayOfTables)
          {
            throwTomlSerializerException(parserState, $"Token '{key}' exists and has type '{token.TokenType}'. Expected token type: '{nameof(TomlTokenType.ArrayOfTables)}'");
          }
        }
        else
        {
          token = new TomlArrayOfTables(key);
          parentTable[lastKey] = token;
        }


        var arrayToken = (TomlArray) token;
        if (arrayToken.Count > 0 && arrayToken[0].TokenType != TomlTokenType.Table)
        {
          throwTomlSerializerException(parserState, $"Redefinition of array of tables '{key}' is not allowed.");
        }

        //var name = new TomlKey($"{key} - table item {arrayToken.Count}", TomlKeyType.SimpleQuotedBasicString);
        var tableItem = new TomlTable(key)
        {
          IsArrayOfTablesMember = true
        };
        arrayToken.Add(tableItem);
        parserState.CurrentTomlTable = tableItem;
        if (parserState.CurrentChar == ']')
        {
          await advance(parserState).ConfigureAwait(false);
        }

        return arrayToken;
      }
      finally
      {
        parserState.KeyParent = TomlTokenType.Undefined;
      }
    }

    private static async ValueTask<TomlKeyValue> parseKeyValueExpression<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      try
      {
        parserState.KeyParent = parserState.KeyParent == TomlTokenType.Undefined ? TomlTokenType.KeyValue : parserState.KeyParent;
        var (key, table) = await parseKey(parserState).ConfigureAwait(false);

        if (key.IsDottedKey && table.HasTopLevelDefinition)
        {
          throwTomlSerializerException(parserState, $"Appending key '{key.RawKey}' to explicitly defined '{table.FullName}' is not allowed.");
        }

        var lastPartKey = key.LastKeyPart;
        if (table.ContainsKey(lastPartKey.RawKey))
        {
          throwTomlSerializerException(parserState, $"Key '{key.LastKeyPart.RawKey}' already exists. Redefinition is not allowed.");
        }

        await skipWhiteSpaces(parserState).ConfigureAwait(false);
        throwIfEof(parserState);

        if (parserState.CurrentChar != '=')
        {
          throwTomlSerializerException(parserState, $"Expected '=' in key-value definition. Found '{parserState.CurrentChar}'.");
        }

        await advance(parserState).ConfigureAwait(false);
        var value = await parseValue(parserState).ConfigureAwait(false);
        if (table.IsArrayOfTablesMember && key.IsDottedKey && value.TokenType == TomlTokenType.PrimitiveValue)
        {
          throwTomlSerializerException(parserState, $"Key '{key.LastKeyPart.RawKey} is an array of tables. Could not append to array of tables via dotted key.");
        }
        await skipWhiteSpaces(parserState).ConfigureAwait(false);

        if (parserState.CurrentChar == '#')
        {
          _ = await parseComment(parserState).ConfigureAwait(false);
        }

        if (!isNewLineChar(parserState) && parserState.CurrentChar != ',' && parserState.CurrentChar != '}' && !parserState.Eof)
        {
          throwTomlSerializerException(parserState, $"Expected end of line, but found '{parserState.CurrentChar}'.");
        }

        await skipWhiteSpaces(parserState).ConfigureAwait(false);
        var newTomlKeyValue = new TomlKeyValue(key, value);
        return newTomlKeyValue;
      }
      finally
      {
        if (parserState.KeyParent == TomlTokenType.KeyValue)
        {
          parserState.KeyParent = TomlTokenType.Undefined;
        }
      }
    }

    private static async ValueTask<TomlValue> parseValue<TInputReader>(ParserState<TInputReader> parserState)
        where TInputReader : struct, IInputReader 
    {
      await skipWhiteSpaces(parserState).ConfigureAwait(false);
      throwIfEof(parserState);

      switch (parserState.CurrentChar)
      {
        case '"':
          {
            var (parsedString, tomlDataType) = await parseBasicStringOrBasicMlString(parserState).ConfigureAwait(false);
            return new TomlPrimitiveValue(parsedString, TomlValueType.String, tomlDataType);
          }
        case '\'':
          {
            var (parsedString, tomlDataType) = await parseLiteralStringOrMlLiteralString(parserState).ConfigureAwait(false);
            return new TomlPrimitiveValue(parsedString, TomlValueType.String, tomlDataType);
          }
        case '[':
          {
            var tomlArray = await parseArray(parserState).ConfigureAwait(false);
            return tomlArray;
          }
        case '{':
          {
            var tomlInlineTable = await parseInlineTable(parserState).ConfigureAwait(false);
            tomlInlineTable.HasFromInlineTableDefinition = true;
            return tomlInlineTable;
          }
        case 't':
          {
            if (await matchLiteralValue(parserState, TomlPrimitiveValue.TRUE_VALUE_LITERAL).ConfigureAwait(false))
            {
              return new TomlPrimitiveValue(TomlPrimitiveValue.TRUE_VALUE_LITERAL, TomlValueType.Boolean);
            }

            throwTomlSerializerException(parserState, "The value format is not recognized.");
            break;
          }
        case 'f':
          if (await matchLiteralValue(parserState, TomlPrimitiveValue.FALSE_VALUE_LITERAL).ConfigureAwait(false))
          {
            return new TomlPrimitiveValue(TomlPrimitiveValue.FALSE_VALUE_LITERAL, TomlValueType.Boolean);
          }

          throwTomlSerializerException(parserState, "The value format is not recognized.");
          break;
      }

      if (char.IsDigit(parserState.CurrentChar) || parserState.CurrentChar == '+' || parserState.CurrentChar == '-' ||
          parserState.CurrentChar == 'i' || parserState.CurrentChar == 'n')
      {
        var (valueCandidateType, parsedValue, hasNumber) = await parseNumber(parserState).ConfigureAwait(false);
        if (hasNumber)
        {
          return new TomlPrimitiveValue(parsedValue, valueCandidateType.ToTomlValueType(), valueCandidateType);
        }

        Debug.Assert((valueCandidateType & TomlDataType.AllDateTimeTypes) != 0);
        var (subType, dateTimeValue) = await parseDateTime(parserState, parsedValue).ConfigureAwait(false);
        Debug.Assert(subType != TomlDataType.Unspecified);
        return new TomlPrimitiveValue(dateTimeValue, TomlValueType.DateTime, subType);
      }

      throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
      
      return null;
    }

    private static async ValueTask<(TomlDataType subtype, string dateValue)> parseDateTime<TInputReader>(ParserState<TInputReader> parserState,
                                                                                                         string firstDateOrTimePart)
      where TInputReader : struct, IInputReader
    {
      try
      {
        var candidateTypes = TomlDataType.AllDateTimeTypes;
        Debug.Assert(!String.IsNullOrEmpty(firstDateOrTimePart));
        Debug.Assert(parserState.StringBuilder.Length == 0);
        if (parserState.CurrentChar == '-')
        {
          candidateTypes &= ~TomlDataType.LocalTime;
          await parseFullDate(parserState, firstDateOrTimePart).ConfigureAwait(false);
          await advance(parserState).ConfigureAwait(false);
          if (parserState.Eof)
          {
            candidateTypes = TomlDataType.LocalDate;
            return (candidateTypes, parserState.StringBuilder.ToString());
          }

          if (parserState.CurrentChar == 't' || parserState.CurrentChar == 'T' || parserState.CurrentChar == ' ')
          {
            var mustHaveDateTime = false;
            if (parserState.CurrentChar == 't' || parserState.CurrentChar == 'T')
            {
              mustHaveDateTime = true;
              parserState.StringBuilder.Append(parserState.CurrentChar);
              candidateTypes &= ~TomlDataType.LocalDate;
            }

            await advance(parserState).ConfigureAwait(false);
            if (parserState.CurrentChar.Equals('#'))
            {
              if (mustHaveDateTime)
              {
                throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
              }

              candidateTypes = TomlDataType.LocalDate;
              return (candidateTypes, parserState.StringBuilder.ToString());
            }

            if (!mustHaveDateTime)
            {
              //Prev char
              parserState.StringBuilder.Append(' ');
            }

            throwIfEof(parserState);
            await parsePartialTime(parserState).ConfigureAwait(false);
          }
          else
          {
            candidateTypes = TomlDataType.LocalDate;
            return (candidateTypes, parserState.StringBuilder.ToString());
          }

          await advance(parserState).ConfigureAwait(false);
          if (parserState.Eof || (parserState.CurrentChar != 'Z' && parserState.CurrentChar != 'z' && parserState.CurrentChar != '+' && parserState.CurrentChar != '-'))
          {
            candidateTypes = TomlDataType.LocalDateTime;
            return (candidateTypes, parserState.StringBuilder.ToString());
          }

          candidateTypes = TomlDataType.OffsetDateTime;
          await parseTimeOffset(parserState).ConfigureAwait(false);
          await advance(parserState).ConfigureAwait(false);
          return (candidateTypes, parserState.StringBuilder.ToString());
        }
        else if (parserState.CurrentChar == ':')
        {
          candidateTypes = TomlDataType.LocalTime;
          await parsePartialTime(parserState, firstDateOrTimePart).ConfigureAwait(false);
          await advance(parserState).ConfigureAwait(false);
          return (candidateTypes, parserState.StringBuilder.ToString());
        }
      }
      finally
      {
        parserState.StringBuilder.Clear();

      }

      throw new UnreachableException();
    }

    private static async ValueTask parseTimeOffset<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      if (parserState.CurrentChar == 'Z' || parserState.CurrentChar == 'z')
      {
        parserState.StringBuilder.Append(parserState.CurrentChar);
        return;
      }

      if (parserState.CurrentChar == '+' || parserState.CurrentChar == '-')
      {
        parserState.StringBuilder.Append(parserState.CurrentChar);
        await advance(parserState).ConfigureAwait(false);
        throwIfEof(parserState);
        await parseTimeHour(parserState).ConfigureAwait(false);

        await advance(parserState).ConfigureAwait(false);
        throwIfEof(parserState);
        expectChar(parserState, ':', INVALID_TOML_VALUE_ERROR);
        parserState.StringBuilder.Append(parserState.CurrentChar);

        await advance(parserState).ConfigureAwait(false);
        throwIfEof(parserState);
        await parseTimeMinute(parserState).ConfigureAwait(false);
        return;
      }

      throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
    }

    private static async ValueTask parsePartialTime<TInputReader>(ParserState<TInputReader> parserState,
                                                                  string? firstDateOrTimePart = null)
      where TInputReader : struct, IInputReader
    {

      await parseTimeHour(parserState, firstDateOrTimePart).ConfigureAwait(false);
      if (firstDateOrTimePart == null)
      {
        await advance(parserState).ConfigureAwait(false);
      }

      throwIfEof(parserState);
      expectChar(parserState, ':', INVALID_TOML_VALUE_ERROR);
      parserState.StringBuilder.Append(parserState.CurrentChar);
      await advance(parserState).ConfigureAwait(false);
      throwIfEof(parserState);
      await parseTimeMinute(parserState).ConfigureAwait(false);

      var peekChar = peek(parserState);

      if (peekChar != ':' && parserState.TomlSettings.TomlVersion == TomlVersion.Toml11)
      {
        //This is workaround for external tests
        parserState.StringBuilder.Append(DEFAULT_SECONDS_IN_TIME);
        return;
      }

      await advance(parserState).ConfigureAwait(false);
      throwIfEof(parserState);
      expectChar(parserState, ':', INVALID_TOML_VALUE_ERROR);
      parserState.StringBuilder.Append(parserState.CurrentChar);
      await advance(parserState).ConfigureAwait(false);
      throwIfEof(parserState);
      await parseTimeSeconds(parserState).ConfigureAwait(false);

      peekChar = peek(parserState);
      if (peekChar != '.')
      {
        return;
      }

      await advance(parserState).ConfigureAwait(false);
      await parseTimeSecFraction(parserState).ConfigureAwait(false);
    }

    private static async ValueTask parseTimeSecFraction<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      expectChar(parserState, '.', INVALID_TOML_VALUE_ERROR);
      parserState.StringBuilder.Append(parserState.CurrentChar);
      await advance(parserState).ConfigureAwait(false);
      throwIfEof(parserState);
      if (!char.IsDigit(parserState.CurrentChar))
      {
        throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
      }

      await parseDigits(parserState).ConfigureAwait(false);
    }

    private static async ValueTask parseDigits<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      Debug.Assert(char.IsDigit(parserState.CurrentChar));
      char? nextDigit = parserState.CurrentChar;
      bool isDigit;
      do
      {
        Debug.Assert(nextDigit != null);
        parserState.StringBuilder.Append(nextDigit.Value);
        nextDigit = peek(parserState);
        isDigit = nextDigit != null && char.IsDigit(nextDigit.Value);
        if (isDigit)
        {
          await advance(parserState).ConfigureAwait(false);
        }
      } while (isDigit);
    }

    private static async ValueTask<int> parseTimeSeconds<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {

      if (!char.IsDigit(parserState.CurrentChar) || parserState.CurrentChar - '0' > 5)
      {
        throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
      }

      var secondsC1 = parserState.CurrentChar;
      parserState.StringBuilder.Append(parserState.CurrentChar);
      await advance(parserState).ConfigureAwait(false);
      throwIfEof(parserState);
      var secondsC2 = parserState.CurrentChar;
      if (!char.IsDigit(parserState.CurrentChar) || !rawSecondsToIntSeconds(secondsC1, secondsC2, out var seconds))
      {
        throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
        //Unreachable
        return -1;
      }

      parserState.StringBuilder.Append(parserState.CurrentChar);
      return seconds;
    }

    private static bool rawSecondsToIntSeconds(char secondsC1,
                                               char secondsC2,
                                               out int seconds) => int.TryParse($"{secondsC1}{secondsC2}", out seconds) &&
                                                                   seconds >= 0 &&
                                                                   seconds <= 59;

    private static async ValueTask<int> parseTimeMinute<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      if (!char.IsDigit(parserState.CurrentChar) || parserState.CurrentChar - '0' > 5)
      {
        throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
      }

      var minuteC1 = parserState.CurrentChar;
      parserState.StringBuilder.Append(parserState.CurrentChar);
      await advance(parserState).ConfigureAwait(false);
      throwIfEof(parserState);
      var minuteC2 = parserState.CurrentChar;
      if (!char.IsDigit(parserState.CurrentChar) || !rawMinuteToIntMinute(minuteC1, minuteC2, out var minute))
      {
        throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
        //Unreachable
        return -1;
      }

      parserState.StringBuilder.Append(parserState.CurrentChar);
      return minute;
    }

    private static bool rawMinuteToIntMinute(char minuteC1,
                                             char minuteC2,
                                             out int minute) => int.TryParse($"{minuteC1}{minuteC2}", out minute) && minute >= 0 && minute <= 59;

    private static async ValueTask<int> parseTimeHour<TInputReader>(ParserState<TInputReader> parserState,
                                                                    string? firstDateOrTimePart = null)
      where TInputReader : struct, IInputReader
    {
      if (firstDateOrTimePart != null)
      {
        if (!rawHourToIntHour(firstDateOrTimePart, out var intHour))
        {
          throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
        }

        parserState.StringBuilder.Append(firstDateOrTimePart);
        return intHour;
      }
      if (!char.IsDigit(parserState.CurrentChar))
      {
        throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
      }

      expectChar(parserState, '0', '1', '2', INVALID_TOML_VALUE_ERROR);
      var hourC1 = parserState.CurrentChar;
      parserState.StringBuilder.Append(parserState.CurrentChar);
      await advance(parserState).ConfigureAwait(false);
      throwIfEof(parserState);
      var hourC2 = parserState.CurrentChar;
      if (!char.IsDigit(parserState.CurrentChar) || !rawHourToIntHour(hourC1, hourC2, out var hour))
      {
        throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
        //Unreachable
        return -1;
      }

      parserState.StringBuilder.Append(parserState.CurrentChar);
      return hour;
    }

    private static bool rawHourToIntHour(string rawHour,
                                         out int hour) => int.TryParse(rawHour, out hour) && rawHour.Length == 2 && hour >= 0 && hour <= 23;

    private static bool rawHourToIntHour(char hourC1,
                                         char hourC2,
                                         out int hour) => rawHourToIntHour($"{hourC1}{hourC2}", out hour);

    private static async ValueTask parseFullDate<TInputReader>(ParserState<TInputReader> parserState,
                                                               string firstDatePart)
      where TInputReader : struct, IInputReader

    {
      Debug.Assert(parserState.StringBuilder.Length == 0);
      var year = await parseYear(firstDatePart, parserState).ConfigureAwait(false);
      expectChar(parserState, '-', INVALID_TOML_VALUE_ERROR);
      parserState.StringBuilder.Append(parserState.CurrentChar);
      await advance(parserState).ConfigureAwait(false);
      throwIfEof(parserState);
      var month = await parseMonth(parserState).ConfigureAwait(false);
      await advance(parserState).ConfigureAwait(false);
      throwIfEof(parserState);
      expectChar(parserState, '-', INVALID_TOML_VALUE_ERROR);
      parserState.StringBuilder.Append(parserState.CurrentChar);
      await advance(parserState).ConfigureAwait(false);
      throwIfEof(parserState);
      await parseMonthDay(parserState, year, month).ConfigureAwait(false);
    }

    private static async ValueTask<int> parseMonthDay<TInputReader>(ParserState<TInputReader> parserState,
                                                                    int year,
                                                                    int month)
      where TInputReader : struct, IInputReader
    {

      if (!char.IsDigit(parserState.CurrentChar))
      {
        throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
      }
      var dayC1 = parserState.CurrentChar;
      parserState.StringBuilder.Append(parserState.CurrentChar);
      await advance(parserState).ConfigureAwait(false);
      throwIfEof(parserState);
      var dayC2 = parserState.CurrentChar;
      if (!char.IsDigit(parserState.CurrentChar) || !rawMonthDayToMonthDayInt(parserState, dayC1, dayC2, year, month, out var day))
      {
        throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
        //Unreachable
        return -1;
      }

      parserState.StringBuilder.Append(parserState.CurrentChar);
      return day;
    }

    private static bool rawMonthDayToMonthDayInt<TInputReader>(ParserState<TInputReader> parserState,
                                                               char dayC1,
                                                               char dayC2,
                                                               int year,
                                                               int month,
                                                               out int day)
      where TInputReader : struct, IInputReader
    {
      if (!int.TryParse($"{dayC1}{dayC2}", out day))
      {
        throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
      }

      if (!DateTime.TryParseExact($"{year}/{month}/{day}", new[] { "yyyy/MM/dd", "yyyy/M/dd", "yyyy/MM/d", "yyyy/M/d", "y/M/d" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var _))
      {
        throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
      }

      return true;
    }

    private static async ValueTask<int> parseMonth<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      if (!char.IsDigit(parserState.CurrentChar))
      {
        throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
      }

      expectChar(parserState, '0', '1', INVALID_TOML_VALUE_ERROR);
      var monthC1 = parserState.CurrentChar;
      parserState.StringBuilder.Append(parserState.CurrentChar);
      await advance(parserState).ConfigureAwait(false);
      throwIfEof(parserState);
      var monthC2 = parserState.CurrentChar;
      if (!char.IsDigit(parserState.CurrentChar) || !rawMonthToIntMonth(monthC1, monthC2, out var month))
      {
        throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
        //Unreachable
        return -1;
      }

      parserState.StringBuilder.Append(parserState.CurrentChar);
      return month;
    }

    private static bool rawMonthToIntMonth(char monthC1,
                                           char monthC2,
                                           out int month) => int.TryParse(new string(new[] { monthC1, monthC2 }), out month) &&
                                                             month >= 1 &&
                                                             month <= 12;

    private static ValueTask<int> parseYear<TInputReader>(string yearCandidate, ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      const int YEAR_LENGTH = 4;
      if (yearCandidate.Length != YEAR_LENGTH || !int.TryParse(yearCandidate, out var year))
      {
        throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
        //Unreachable
        return new ValueTask<int>(-1);
      }

      parserState.StringBuilder.Append(yearCandidate);
      return new ValueTask<int>(year);
    }

    private static async ValueTask<TomlInlineTable> parseInlineTable<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      var oldTable = parserState.CurrentTomlTable;
      Debug.Assert(oldTable != null);
      try
      {
        if (parserState.CurrentChar != '{')
        {
          throwTomlSerializerException(parserState, INVALID_TOML_INLINE_TABLE_DEFINITION_ERROR);
        }


        var newInlineTable = new TomlInlineTable(new TomlKey(TomlTable.ANONYMOUS_TABLE_NAME, TomlKeyType.SimpleQuotedBasicString));
        while (parserState.CurrentChar != '}')
        {
          await advance(parserState).ConfigureAwait(false);
          await skipWhiteSpaces(parserState).ConfigureAwait(false);
          throwIfEof(parserState);

          if (parserState.CurrentChar == '}')
          {
            if (newInlineTable.Count > 0 && parserState.TomlSettings.TomlVersion == TomlVersion.Toml10)
            {
              //Trailing comma not allowed
              throwTomlSerializerException(parserState, INVALID_TOML_INLINE_TABLE_DEFINITION_ERROR);
            }

            continue;
          }

          //Empty inline table with comma
          if (parserState.CurrentChar == ',')
          {
            throwTomlSerializerException(parserState, INVALID_TOML_INLINE_TABLE_DEFINITION_ERROR);
          }

          if (isNewLineChar(parserState) || parserState.CurrentChar == '#')
          {
            if (parserState.TomlSettings.TomlVersion == TomlVersion.Toml10)
            {
              throwTomlSerializerException(parserState, INVALID_TOML_INLINE_TABLE_DEFINITION_ERROR);
            }
            else
            {
              await parseWsCommentNewLine(parserState).ConfigureAwait(false);
              if (parserState.CurrentChar == '}')
              {
                continue;
              }
            }
          }

          parserState.CurrentTomlTable = newInlineTable;
          var keyValueExpression = await parseKeyValueExpression(parserState).ConfigureAwait(false);
          if (keyValueExpression.Key.IsDottedKey)
          {
            parserState.CurrentTomlTable.AddInternal(keyValueExpression.Key.LastKeyPart, keyValueExpression.Value);
          }
          else
          {
            newInlineTable.AddInternal(keyValueExpression.Key.LastKeyPart, keyValueExpression.Value);
          }

          if (parserState.TomlSettings.TomlVersion == TomlVersion.Toml10)
          {
            await skipWhiteSpaces(parserState).ConfigureAwait(false);
          }
          else
          {
            await parseWsCommentNewLine(parserState).ConfigureAwait(false);
          }

          expectChar(parserState, ',', '}', INVALID_TOML_ARRAY_DEFINITION_ERROR);
        }

        await advance(parserState).ConfigureAwait(false);
        return newInlineTable;
      }
      finally
      {
        parserState.CurrentTomlTable = oldTable;
        parserState.KeyParent = TomlTokenType.Undefined;
      }
    }

    private static async ValueTask<TomlArray> parseArray<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      if (parserState.CurrentChar != '[')
      {
        throwTomlSerializerException(parserState, INVALID_TOML_ARRAY_DEFINITION_ERROR);
      }

      var tomlArray = new TomlArray(new TomlKey(TomlArray.ANONYMOUS_ARRAY_NAME, TomlKeyType.SimpleQuotedBasicString));

      while (parserState.CurrentChar != ']')
      {
        if (!await advance(parserState).ConfigureAwait(false))
        {
          throwTomlSerializerException(parserState, INVALID_TOML_ARRAY_DEFINITION_ERROR);
        }

        await parseWsCommentNewLine(parserState).ConfigureAwait(false);

        if (parserState.CurrentChar == ']')
        {
          continue;
        }

        var arrayValue = await parseValue(parserState).ConfigureAwait(false);
        tomlArray.Add(arrayValue);
        await parseWsCommentNewLine(parserState).ConfigureAwait(false);
        expectChar(parserState, ',', ']', INVALID_TOML_ARRAY_DEFINITION_ERROR);
      }

      await advance(parserState).ConfigureAwait(false);
      return tomlArray;
    }

    private static async ValueTask parseWsCommentNewLine<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      await skipWhitespacesAndNewLines(parserState).ConfigureAwait(false);
      throwIfEof(parserState);
      while (parserState.CurrentChar == '#')
      {
        _ = await parseComment(parserState).ConfigureAwait(false);
        await advance(parserState).ConfigureAwait(false);
        throwIfEof(parserState);
        await skipWhitespacesAndNewLines(parserState).ConfigureAwait(false);
      }
    }

    private static async ValueTask<(TomlDataType valueCandidateTypes, string parsedValue, bool hasNumber)> parseNumber<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      var builder = parserState.StringBuilder;
      Debug.Assert(builder.Length == 0);
      var valueCandidateTypes = TomlDataType.AllNumberTypes | TomlDataType.AllDateTimeTypes;
      try
      {
        if (parserState.CurrentChar == '+' || parserState.CurrentChar == '-')
        {
          valueCandidateTypes &= ~TomlDataType.IntegerHex | ~TomlDataType.IntegerOct | TomlDataType.IntegerBin | ~TomlDataType.AllDateTimeTypes;
          builder.Append(parserState.CurrentChar);
          await advance(parserState).ConfigureAwait(false);
          throwIfEof(parserState);
        }

        var readIndex = -1;
        while (true)
        {
          ++readIndex;

          if (parserState.Eof)
          {
            if (builder.Length == 0 || (builder.Length == 1 && !char.IsDigit(builder[0])))
            {
              throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
            }

            valueCandidateTypes = TomlDataType.IntegerDec;
            return (valueCandidateTypes, normalizeZeroNumber(builder.ToString()), hasNumber: true);
          }

          if (readIndex == 0)
          {
            if (!char.IsDigit(parserState.CurrentChar) && parserState.CurrentChar != 'i' && parserState.CurrentChar != 'n')
            {
              throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
            }
          }

          if (readIndex == 1 && char.IsDigit(parserState.CurrentChar) && hasZeroChar(builder))
          {
            valueCandidateTypes = TomlDataType.AllDateTimeTypes;
          }

          if (readIndex == 0 && parserState.CurrentChar == 'i')
          {
            if (builder.Length > 0 && builder[0] != '+' && builder[0] != '-')
            {
              throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
            }

            valueCandidateTypes = TomlDataType.Float;
            await parseSpecialFloatInf(parserState).ConfigureAwait(false);
            return (valueCandidateTypes, builder.ToString(), hasNumber: true);
          }

          if (readIndex == 0 && parserState.CurrentChar == 'n')
          {
            if (builder.Length > 0 && builder[0] != '+' && builder[0] != '-')
            {
              throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
            }

            valueCandidateTypes = TomlDataType.Float;
            await parseSpecialFloatNan(parserState).ConfigureAwait(false);
            return (valueCandidateTypes, builder.ToString(), hasNumber: true);
          }

          if (readIndex == 1)
          {
            if (parserState.CurrentChar == 'x')
            {
              if (builder[0] != '0')
              {
                throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
              }

              valueCandidateTypes = TomlDataType.IntegerHex;
              var peekChar = peek(parserState);
              if (peekChar == null)
              {
                throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
              }

              if (!isHexDigit(peekChar.Value))
              {
                throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
              }

              builder.Append(parserState.CurrentChar);
              await advance(parserState).ConfigureAwait(false);
              await parseHexNumber(parserState).ConfigureAwait(false);
              return (valueCandidateTypes, builder.ToString(), hasNumber: true);
            }
            else if (parserState.CurrentChar == 'o')
            {
              if (builder[0] != '0')
              {
                throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
              }

              valueCandidateTypes = TomlDataType.IntegerOct;
              var peekChar = peek(parserState);
              if (peekChar == null)
              {
                throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
              }

              if (!isOctalDigit(peekChar.Value))
              {
                throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
              }

              builder.Append(parserState.CurrentChar);
              await advance(parserState).ConfigureAwait(false);
              await parseOctInt(parserState).ConfigureAwait(false);
              return (valueCandidateTypes, builder.ToString(), hasNumber: true);
            }
            else if (parserState.CurrentChar == 'b')
            {
              if (builder[0] != '0')
              {
                throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
              }

              valueCandidateTypes = TomlDataType.IntegerBin;
              var peekChar = peek(parserState);
              if (peekChar == null)
              {
                throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
              }

              if (!isBinaryDigit(peekChar.Value))
              {
                throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
              }

              builder.Append(parserState.CurrentChar);
              await advance(parserState).ConfigureAwait(false);
              await parseBinInt(parserState).ConfigureAwait(false);
              return (valueCandidateTypes, builder.ToString(), hasNumber: true);
            }
            else
            {
              var canBeIntDecOrFloat = (valueCandidateTypes & (TomlDataType.IntegerDec | valueCandidateTypes | TomlDataType.Float)) != 0;
              var canBeDate = (valueCandidateTypes & TomlDataType.AllDateTimeTypes) != 0;
              await parseUnsignedDecInt(parserState).ConfigureAwait(false);
              if (canBeIntDecOrFloat && parserState.CurrentChar == 'e' || parserState.CurrentChar == 'E')
              {
                valueCandidateTypes = TomlDataType.Float;

                if (isInvalidZeroPrefixedNumber(builder))
                {
                  throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
                }

                await parseFloatExponent(parserState).ConfigureAwait(false);
                return (valueCandidateTypes, builder.ToString(), hasNumber: true);
              }
              else if (canBeIntDecOrFloat && parserState.CurrentChar == '.')
              {
                valueCandidateTypes = TomlDataType.Float;

                if (isInvalidZeroPrefixedNumber(builder))
                {
                  throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
                }

                await parseFloatFraction(parserState).ConfigureAwait(false);
                return (valueCandidateTypes, builder.ToString(), hasNumber: true);
              }
              else if (canBeDate && (parserState.CurrentChar == '-' || parserState.CurrentChar == ':'))
              {
                valueCandidateTypes = TomlDataType.AllDateTimeTypes;
                return (valueCandidateTypes, builder.ToString(), hasNumber: false);
              }
              else if (canBeIntDecOrFloat)
              {
                if (isInvalidZeroPrefixedNumber(builder))
                {
                  throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
                }

                valueCandidateTypes = TomlDataType.IntegerDec;
                return (valueCandidateTypes, normalizeZeroNumber(builder.ToString()), hasNumber: true);
              }
              else
              {
                throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
              }
            }
          }

          if (char.IsDigit(parserState.CurrentChar))
          {
            builder.Append(parserState.CurrentChar);
          }

          await advance(parserState).ConfigureAwait(false);
        }
      }
      finally
      {
        builder.Clear();
      }
    }

    private static bool isInvalidZeroPrefixedNumber(StringBuilder builder)
    {
      return numberStartsWithZero(builder) && ((builder.Length == 2 && builder[0] == '0' && char.IsDigit(builder[1])) || builder.Length > 2);
    }

    private static bool numberStartsWithZero(StringBuilder builder) => builder[0] == '0' || (!char.IsDigit(builder[0]) && builder[1] == '0');

    private static string normalizeZeroNumber(string number) => number.Length == 2 && number[1] == '0' && (number[0] == '+' || number[0] == '-') ? "0" : number;

    private static async ValueTask parseSpecialFloatNan<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      if (!await matchLiteralValue(parserState, TomlPrimitiveValue.SPECIAL_FLOAT_NAN_LITERAL).ConfigureAwait(false))
      {
        throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
      }

      parserState.StringBuilder.Append(TomlPrimitiveValue.SPECIAL_FLOAT_NAN_LITERAL);
    }

    private static async ValueTask parseSpecialFloatInf<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      if (!await matchLiteralValue(parserState, TomlPrimitiveValue.SPECIAL_FLOAT_INF_LITERAL).ConfigureAwait(false))
      {
        throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
      }

      parserState.StringBuilder.Append(TomlPrimitiveValue.SPECIAL_FLOAT_INF_LITERAL);
    }

    private static async ValueTask parseFloatFraction<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      Debug.Assert(parserState.CurrentChar == '.');
      var builder = parserState.StringBuilder;
      builder.Append(parserState.CurrentChar);
      await advance(parserState).ConfigureAwait(false);
      throwIfEof(parserState);

      if (!char.IsDigit(parserState.CurrentChar))
      {
        throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
      }

      await parseUnsignedDecInt(parserState).ConfigureAwait(false);
      if (parserState.CurrentChar == 'e' || parserState.CurrentChar == 'E')
      {
        await parseFloatExponent(parserState).ConfigureAwait(false);
      }
    }

    private static async ValueTask parseFloatExponent<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      Debug.Assert(parserState.CurrentChar == 'e' || parserState.CurrentChar == 'E');
      var builder = parserState.StringBuilder;
      builder.Append(parserState.CurrentChar);
      await advance(parserState).ConfigureAwait(false);
      throwIfEof(parserState);
      if (parserState.CurrentChar == '+' || parserState.CurrentChar == '-')
      {
        builder.Append(parserState.CurrentChar);
        await advance(parserState).ConfigureAwait(false);
      }

      if (!char.IsDigit(parserState.CurrentChar))
      {
        throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
      }

      await parseUnsignedDecInt(parserState).ConfigureAwait(false);
    }

    private static async ValueTask parseBinInt<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader 
    {
      var builder = parserState.StringBuilder;
      while (!parserState.Eof && (isBinaryDigit(parserState.CurrentChar) || parserState.CurrentChar == '_'))
      {
        if (isBinaryDigit(parserState.CurrentChar))
        {
          builder.Append(parserState.CurrentChar);
        }

        var peekChar = peek(parserState);
        if (parserState.CurrentChar == '_' && (peekChar == null || peekChar == '_' || !isBinaryDigit(peekChar.Value)))
        {
          throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
        }

        await advance(parserState).ConfigureAwait(false);
      }
    }

    private static bool isBinaryDigit(char ch) => ch == '0' || ch == '1';

    private static async ValueTask parseOctInt<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      var builder = parserState.StringBuilder;

      while (!parserState.Eof && (isOctalDigit(parserState.CurrentChar) || parserState.CurrentChar == '_'))
      {
        if (isOctalDigit(parserState.CurrentChar))
        {
          builder.Append(parserState.CurrentChar);
        }
        var peekChar = peek(parserState);
        if (parserState.CurrentChar == '_' && (peekChar == null || peekChar == '_' || !isOctalDigit(peekChar.Value)))
        {
          throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
        }
        await advance(parserState).ConfigureAwait(false);
      }
    }

    private static bool isOctalDigit(char ch) => (ch >= '0' && ch <= '7');

    private static bool isHexDigit(char ch) => char.IsDigit(ch) || (ch >= 'A' && ch <= 'F') || ((ch >= 'a' && ch <= 'f'));

    private static async ValueTask parseHexNumber<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      var builder = parserState.StringBuilder;

      while (!parserState.Eof && (isHexDigit(parserState.CurrentChar) || parserState.CurrentChar == '_'))
      {
        if (isHexDigit(parserState.CurrentChar))
        {
          builder.Append(parserState.CurrentChar);
        }

        var peekChar = peek(parserState);
        if (parserState.CurrentChar == '_' && (peekChar == null || peekChar == '_' || !isHexDigit(peekChar.Value)))
        {
          throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
        }

        await advance(parserState).ConfigureAwait(false);
      }
    }

    private static async ValueTask parseUnsignedDecInt<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      var builder = parserState.StringBuilder;

      while (!parserState.Eof && (char.IsDigit(parserState.CurrentChar) || parserState.CurrentChar == '_'))
      {
        if (char.IsDigit(parserState.CurrentChar))
        {
          builder.Append(parserState.CurrentChar);
        }

        var peekChar = peek(parserState);
        if (parserState.CurrentChar == '_' && (peekChar == null || peekChar == '_' || !char.IsDigit(peekChar.Value)))
        {
          throwTomlSerializerException(parserState, INVALID_TOML_VALUE_ERROR);
        }
        await advance(parserState).ConfigureAwait(false);
      }
    }

    private static bool hasZeroChar(StringBuilder builder)
    {
      for (int i = 0; i < builder.Length; i++)
      {
        if (builder[i] == '0')
        {
          return true;
        }
      }

      return false;
    }

    private static async ValueTask<bool> matchLiteralValue<TInputReader>(ParserState<TInputReader> parserState,
                                                                         string literal)
      where TInputReader : struct, IInputReader
    {
      foreach (var l in literal)
      {
        if (parserState.CurrentChar == l)
        {
          await advance(parserState).ConfigureAwait(false);
          continue;
        }

        return false;
      }

      return true;
    }

    private static async ValueTask<(string parsedString, TomlDataType tomlDataType)> parseLiteralStringOrMlLiteralString<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      Debug.Assert(parserState.CurrentChar == '\'');

      if (peek(parserState) != '\'')
      {
        return (await parseLiteralString(parserState).ConfigureAwait(false), TomlDataType.LiteralString);
      }

      await advance(parserState).ConfigureAwait(false);

      if (peek(parserState) != '\'')
      {
        await advance(parserState).ConfigureAwait(false);
        return (String.Empty, TomlDataType.LiteralString);
      }

      await advance(parserState).ConfigureAwait(false);
      return (await parseLiteralMlString(parserState).ConfigureAwait(false), TomlDataType.LiteralMlString);
    }

    private static async ValueTask<string> parseLiteralMlString<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      var literalMlBuilder = parserState.StringBuilder;
      Debug.Assert(parserState.CurrentChar == '\'');
      Debug.Assert(literalMlBuilder.Length == 0);
      try
      {
        //parserState.ParsingString = true;
        await advance(parserState).ConfigureAwait(false);
        await skipNewLine(parserState).ConfigureAwait(false);
        while (true)
        {
          throwIfEof(parserState);
          if (isMllChar(parserState.CurrentChar))
          {
            literalMlBuilder.Append(parserState.CurrentChar);
            await advance(parserState).ConfigureAwait(false);
          }
          else if (isNewLineChar(parserState))
          {
            literalMlBuilder.Append(WINDOWS_NEW_LINE);
            await skipNewLine(parserState).ConfigureAwait(false);
          }
          else if (parserState.CurrentChar == '\'')
          {
            var isStringEnd = false;
            var toWriteQuotes = 1;
            var totalQuotes = 1;
            await advance(parserState).ConfigureAwait(false);
            throwIfEof(parserState);
            if (parserState.CurrentChar == '\'')
            {
              await advance(parserState).ConfigureAwait(false);
              throwIfEof(parserState);
              ++toWriteQuotes;
              ++totalQuotes;

              if (parserState.CurrentChar == '\'')
              {
                isStringEnd = true;
                toWriteQuotes = 0;
                ++totalQuotes;
                while (await advance(parserState).ConfigureAwait(false) && parserState.CurrentChar == '\'')
                {
                  ++totalQuotes;
                  ++toWriteQuotes;
                }
              }
            }

            var maxAllowedSingleQuotes = totalQuotes;
            if (isStringEnd)
            {
              maxAllowedSingleQuotes -= NUMBER_OF_OPENING_CLOSE_SINGLE_QUOTES_IN_LITERAL_ML_STRING;
            }

            if (maxAllowedSingleQuotes > MAX_SINGLE_QUOTES_IN_LITERAL_ML_STRING)
            {
              throwTomlSerializerException(parserState, $"Exceeded max number of allowed consecutive single quotes in multiline literal string. Found '{totalQuotes}' single quotes.");
            }

            literalMlBuilder.Append('\'', toWriteQuotes);
            if (isStringEnd)
            {
              return literalMlBuilder.ToString();
            }
          }
          else
          {
            throwTomlSerializerException(parserState, $"Invalid string definition. Found unsupported char {(int)parserState.CurrentChar:x}");
          }
        }
      }
      finally
      {
        literalMlBuilder.Clear();
      }
    }

    private static bool isMllChar(char ch) => isLiteralChar(ch);

    private static async ValueTask<(string parserString, TomlDataType tomlDataType)> parseBasicStringOrBasicMlString<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      Debug.Assert(parserState.CurrentChar == '"');
      if (peek(parserState) != '"')
      {
        return (await parseBasicString(parserState).ConfigureAwait(false), TomlDataType.BasicString);
      }

      await advance(parserState).ConfigureAwait(false);
      if (peek(parserState) != '"')
      {
        await advance(parserState).ConfigureAwait(false);
        return (String.Empty, TomlDataType.BasicString);
      }

      await advance(parserState).ConfigureAwait(false);

      return (await parseBasicMlString(parserState).ConfigureAwait(false), TomlDataType.BasicMlString);
    }

    private static async ValueTask<string> parseBasicMlString<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      var basicMlBuilder = parserState.StringBuilder;
      Debug.Assert(parserState.CurrentChar == '"');
      Debug.Assert(basicMlBuilder.Length == 0);
      try
      {
        parserState.ParsingString = true;
        await advance(parserState).ConfigureAwait(false);
        await skipNewLine(parserState).ConfigureAwait(false);
        while (true)
        {
          throwIfEof(parserState);
          if (isMlbUnescapedChar(parserState.CurrentChar))
          {
            basicMlBuilder.Append(parserState.CurrentChar);
            await advance(parserState).ConfigureAwait(false);
          }
          else if (isEscapedChar(parserState.CurrentChar))
          {
            var peekChar = peek(parserState);
            if (peekChar == null)
            {
              throwEof(parserState);
            }

            if (isWhiteSpace(peekChar.Value) || isNewLineChar(peekChar.Value))
            {
              await parseMlbEscapedNl(parserState).ConfigureAwait(false);
            }
            else
            {
              basicMlBuilder.Append(await parseBasicEscapedChar(parserState).ConfigureAwait(false));
              await advance(parserState).ConfigureAwait(false);
            }
          }
          else if (isNewLineChar(parserState))
          {
            basicMlBuilder.Append(WINDOWS_NEW_LINE);
            await skipNewLine(parserState).ConfigureAwait(false);
          }
          else if (parserState.CurrentChar == '"')
          {
            var isStringEnd = false;
            var toWriteQuotes = 0;
            var foundQuotes = 0;
            do
            {
              switch (foundQuotes)
              {
                case 0:
                case 1:
                  ++toWriteQuotes;
                  ++foundQuotes;
                  await advance(parserState).ConfigureAwait(false);
                  throwIfEof(parserState);
                  break;
                case 2:
                  toWriteQuotes = 0;
                  ++foundQuotes;
                  isStringEnd = true;
                  await advance(parserState).ConfigureAwait(false);
                  break;
                case 3:
                case 4:

                  ++foundQuotes;
                  ++toWriteQuotes;
                  await advance(parserState).ConfigureAwait(false);
                  break;
                default:
                  {
                    throwTomlSerializerException(parserState, "Invalid string definition. Found more than 2 '\"' chars inside the multiline string.");
                    break;
                  }
              }

            } while (!parserState.Eof && parserState.CurrentChar == '"');

            basicMlBuilder.Append('"', toWriteQuotes);
            if (isStringEnd)
            {
              return basicMlBuilder.ToString();
            }
          }
          else
          {
            throwTomlSerializerException(parserState, $"Invalid string definition. Found unsupported char {(int)parserState.CurrentChar:x}");
          }
        }
      }
      finally
      {
        parserState.StringBuilder.Clear();
      }
    }

    private static bool isNewLineChar(char ch) => ch == '\n' || ch == '\r';

    private static async ValueTask parseMlbEscapedNl<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      Debug.Assert(isEscapedChar(parserState.CurrentChar));
      Debug.Assert(isWhiteSpace(peek(parserState)!.Value) || isNewLineChar(peek(parserState)!.Value));

      var foundEol = false;
      await advance(parserState).ConfigureAwait(false);
      while (true)
      {
        throwIfEof(parserState);
        if (isNewLineChar(parserState))
        {
          foundEol = true;
          await skipNewLine(parserState).ConfigureAwait(false);
        }
        else if (!isWhiteSpace(parserState.CurrentChar))
        {
          if (!foundEol)
          {
            throwTomlSerializerException(parserState, "Invalid multiline string. After '/' should be only whitespaces.");
          }
          return;
        }
        else
        {
          await advance(parserState).ConfigureAwait(false);
        }

      }
    }

    private static bool isMlbUnescapedChar(char ch) => isBasicUnescapedChar(ch);

    private static async ValueTask<(TomlKey key, TomlTable CurrentTomlTable)> parseKey<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      Debug.Assert(parserState.KeyParent != TomlTokenType.Undefined);
      TomlKey? tomlKey = null;
      TomlTable? lastParsedTable = null;
      await skipWhiteSpaces(parserState).ConfigureAwait(false);
      if (parserState.CurrentChar == '.')
      {
        throwTomlSerializerException(parserState, "The key must not start with dot ('.') char.");
      }

      throwIfEof(parserState);

      TomlKey? newKeyPart = null;
      while (newKeyPart == null || parserState.CurrentChar == '.')
      {
        if (newKeyPart != null)
        {
          if (parserState.CurrentTomlTable.TryGetValue(newKeyPart, out var tomlTableOrArrayToken))
          {
            if (tomlTableOrArrayToken.TokenType != TomlTokenType.Table && tomlTableOrArrayToken.TokenType != TomlTokenType.ArrayOfTables)
            {
              throwTomlSerializerException(parserState, $"Token '{newKeyPart}' exists and has type '{tomlTableOrArrayToken.TokenType}'. Expected token type: '{nameof(TomlTokenType.Table)} or '{nameof(TomlTokenType.ArrayOfTables)}'.");
              if (tomlTableOrArrayToken.TokenType == TomlTokenType.Array)
              {
                var tokenType = ((TomlArray)tomlTableOrArrayToken)[0]?.TokenType ?? TomlTokenType.Undefined;
                if (tokenType != TomlTokenType.Table)
                {
                  throwTomlSerializerException(parserState, $"Token '{newKeyPart}' must be previously defined array of tables. Found: '{tokenType}'.");
                }
              }
            }
          }
          else
          {
            var newTomlTable = new TomlTable(newKeyPart.Clone());
            parserState.CurrentTomlTable.AddInternal(newKeyPart, newTomlTable);
            newTomlTable.HasFromKeyDefinition = parserState.KeyParent == TomlTokenType.KeyValue;
            if (newTomlTable.HasFromKeyDefinition && (lastParsedTable?.HasTopLevelDefinition ?? false))
            {
              throwTomlSerializerException(parserState);
            }
            tomlTableOrArrayToken = newTomlTable;
          }

          var tomlTable = tomlTableOrArrayToken.TokenType == TomlTokenType.Table
            ? (TomlTable)tomlTableOrArrayToken
            : (TomlTable)tomlTableOrArrayToken.Tokens.Last();

          if (tomlTable.HasFromInlineTableDefinition)
          {
            throwTomlSerializerException(parserState, $"Previously defined inline table '{newKeyPart}' cannot be modified.");
          }

          //tomlTable.HasTopLevelDeclaration = parserState.KeyParent == TomlTokenType.Table;

          if (parserState.KeyParent == TomlTokenType.Table)
          {
            tomlTable.HasTopLevelDeclaration = true;
          }

          parserState.CurrentTomlTable = lastParsedTable = tomlTable;
        }

        if (parserState.CurrentChar == '.')
        {
          await advance(parserState).ConfigureAwait(false);
          parserState.ParsingDottedKey = tomlKey != null && tomlKey.IsDottedKey;
        }

        await skipWhiteSpaces(parserState).ConfigureAwait(false);
        if (!parserState.ParsingDottedKey)
        {
          await skipNewLine(parserState).ConfigureAwait(false);
        }
        else
        {
          await skipWhiteSpaces(parserState).ConfigureAwait(false);
        }

        throwIfEof(parserState);

        switch (parserState.CurrentChar)
        {
          case '\"':
            newKeyPart = new TomlKey(await parseQuotedKey(parserState).ConfigureAwait(false), TomlKeyType.SimpleQuotedBasicString);
            break;
          case '\'':
            newKeyPart = new TomlKey(await parseQuotedKey(parserState).ConfigureAwait(false), TomlKeyType.SimpleQuotedLiteralString);
            break;
          default:
          {
            newKeyPart = new TomlKey(await parseUnquotedKey(parserState).ConfigureAwait(false), TomlKeyType.SimpleUnquoted);
            break;
          }
        }

        if (tomlKey == null)
        {
          tomlKey = newKeyPart;
        }
        else
        {
          tomlKey.LastKeyPart.NextKeyPart = newKeyPart;
        }
        await skipWhiteSpaces(parserState).ConfigureAwait(false);
      }

      Debug.Assert(tomlKey != null);
      return (tomlKey, parserState.CurrentTomlTable);
    }

    private static async ValueTask<string> parseUnquotedKey<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      await skipWhiteSpaces(parserState).ConfigureAwait(false);
      throwIfEof(parserState);
      var keyBuilder = parserState.StringBuilder;
      Debug.Assert(keyBuilder.Length == 0);

      try
      {
        while (true)
        {
          throwIfEof(parserState);
          if (isUnquotedKeyChar(parserState.CurrentChar))
          {
            keyBuilder.Append(parserState.CurrentChar);
            await advance(parserState).ConfigureAwait(false);
          }
          else if (parserState.CurrentChar == '=')
          {
            if (keyBuilder.Length == 0)
            {
              throwTomlSerializerException(parserState, "Key should not start with the '=' character.");
            }

            break;
          }
          else if (parserState.CurrentChar == ']')
          {
            if (keyBuilder.Length == 0)
            {
              throwTomlSerializerException(parserState, "Key should not start with the ']' character.");
            }

            break;
          }
          else if (parserState.CurrentChar == '.')
          {
            if (keyBuilder.Length == 0)
            {
              throwTomlSerializerException(parserState, "Key should not start with the dot ('.') character.");
            }
            break;
          }
          else if (isWhiteSpace(parserState.CurrentChar))
          {
            await skipWhiteSpaces(parserState).ConfigureAwait(false);
            if (parserState.Eof)
            {
              throwTomlSerializerException(parserState, "Unterminated key.");
            }

            if (keyBuilder.Length > 0)
            {
              expectChar(parserState, '=', '.', ']', "Invalid key definition.");
              break;
            }
          }
          else
          {
            throwTomlSerializerException(parserState, $"'{(int)parserState.CurrentChar:x}' character is not allowed in unquoted key.");
          }
        }

        if (keyBuilder.Length == 0)
        {
          throwTomlSerializerException(parserState, "Simple key cannot be empty string.");
        }

        return keyBuilder.ToString();
      }
      finally
      {
        keyBuilder.Clear();
      }
    }

    private static void expectChar<TInputReader>(ParserState<TInputReader> parserState,
                                                 char expectedChar1,
                                                 char expectedChar2,
                                                 char expectedChar3,
                                                 string error)
      where TInputReader : struct, IInputReader
    {
      if (parserState.CurrentChar != expectedChar1 && parserState.CurrentChar != expectedChar2 && parserState.CurrentChar != expectedChar3)
      {
        throwTomlSerializerException(parserState, $"{error} Expected char '{expectedChar1}' or char '{expectedChar2}' or char '{expectedChar3}', but found '{parserState.CurrentChar}'.");
      }
    }

    private static void expectChar<TInputReader>(ParserState<TInputReader> parserState,
                                                 char expectedChar,
                                                 string error)
      where TInputReader : struct, IInputReader
    {
      if (parserState.CurrentChar != expectedChar)
      {
        throwTomlSerializerException(parserState, $"{error} Expected char '{expectedChar}', but found '{parserState.CurrentChar}'.");
      }
    }


    private static void expectChar<TInputReader>(ParserState<TInputReader> parserState,
                                                 char expectedChar1,
                                                 char expectedChar2,
                                                 string error)
      where TInputReader : struct, IInputReader
    {

      if (parserState.CurrentChar != expectedChar1 && parserState.CurrentChar != expectedChar2)
      {
        throwTomlSerializerException(parserState, $"{error} Expected char '{expectedChar1}' or char '{expectedChar2}', but found '{parserState.CurrentChar}'.");
      }
    }

    private static bool isUnquotedKeyChar(char ch) => ch.IsAsciiLetterOrDigit() || ch == '-' || ch == '_';
    private static ValueTask<string> parseQuotedKey<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      switch (parserState.CurrentChar)
      {
        case '\"':
          return parseBasicString(parserState);
        case '\'':
          return parseLiteralString(parserState);
        default:
          throw new TomlSerializerException($"Unexpected char{(int)parserState.CurrentChar:X}");
      }
    }

    private static async ValueTask<string> parseLiteralString<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      Debug.Assert(parserState.CurrentChar == '\'');
      try
      {
        var literalStringBuilder = parserState.StringBuilder;
        parserState.ParsingString = true;

        do
        {
          await advance(parserState).ConfigureAwait(false);
          if (parserState.Eof)
          {
            throwTomlSerializerException(parserState, "Unterminated literal string.");
          }

          if (parserState.CurrentChar == '\'')
          {
            await advance(parserState).ConfigureAwait(false);
            return literalStringBuilder.ToString();
          }

          if (isLiteralChar(parserState.CurrentChar))
          {
            literalStringBuilder.Append(parserState.CurrentChar);
          }

          else
          {
            throwTomlSerializerException(parserState, $"Found unexpected char in literal string. {(int)parserState.CurrentChar:X}");
          }

        } while (true);
      }
      finally
      {
        parserState.StringBuilder.Clear();
        parserState.ParsingString = false;
      }
    }

    private static bool isLiteralChar(char ch) => ch == 0x09 || (ch >= 0x20 && ch <= 0x26) || (ch >= 0x28 && ch <= 0x7E) || ch.IsNonAsciiChar();

    private static async ValueTask<string> parseBasicString<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      Debug.Assert(parserState.CurrentChar == '"');

      try
      {
        var basicStringBuilder = parserState.StringBuilder;
        parserState.ParsingString = true;

        do
        {
          await advance(parserState).ConfigureAwait(false);
          if (parserState.Eof)
          {
            throwTomlSerializerException(parserState, "Unterminated basic string.");
          }

          if (parserState.CurrentChar == '"')
          {
            await advance(parserState).ConfigureAwait(false);
            return basicStringBuilder.ToString();
          }

          if (isBasicUnescapedChar(parserState.CurrentChar))
          {
            basicStringBuilder.Append(parserState.CurrentChar);
          }
          else if (isEscapedChar(parserState.CurrentChar))
          {
            var escapedChar = await parseBasicEscapedChar(parserState).ConfigureAwait(false);
            basicStringBuilder.Append(escapedChar);
          }
          else
          {
            throwTomlSerializerException(parserState, $"Found unexpected char in basic string. '{(int)parserState.CurrentChar:X}'");
          }

        } while (true);
      }
      finally
      {
        parserState.StringBuilder.Clear();
        parserState.ParsingString = false;
      }
    }

    private static async ValueTask<char> parseBasicEscapedChar<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      Debug.Assert(isEscapedChar(parserState.CurrentChar));
      await advance(parserState).ConfigureAwait(false);
      if (parserState.Eof)
      {
        throwTomlSerializerException(parserState, UNEXPECTED_END_OF_ESCAPED_CHAR_ERROR);
      }
      switch (parserState.CurrentChar)
      {
        case '"':
          return '"';
        case '\\':
          return '\\';
        case 'b':
          return '\b';
        case 'f':
          return '\f';
        case 'n':
          return '\n';
        case 'r':
          return '\r';
        case 't':
          return '\t';
        case 'e':
          if (parserState.TomlSettings.TomlVersion == TomlVersion.Toml10)
          {
            goto default;
          }
          return '\u001b';
        case (char)0x75: //uHHHH
        case (char)0x55: //UHHHHHHHH
        case (char)0x78: //xHH
          {
            const int BASE_NUMBER = 16;
            const int FOUR_CHARS = 4;
            const int EIGHT_CHARS = 8;
            const int TWO_CHARS = 2;
            if (parserState.CurrentChar == 0x78 && parserState.TomlSettings.TomlVersion == TomlVersion.Toml10)
            {
                goto default;
            }
            int requiredChars = parserState.CurrentChar == 0x75
              ? FOUR_CHARS
              : parserState.CurrentChar == 0x55
                ? EIGHT_CHARS
                : TWO_CHARS;
            var numberChars = new char[requiredChars];
            var readResult = await parserState.ReadAsync(new ArraySegment<char>(numberChars)).ConfigureAwait(false);
            if (readResult < requiredChars)
            {
              throwTomlSerializerException(parserState, UNEXPECTED_END_OF_ESCAPED_CHAR_ERROR);
            }

            var int32Char = Convert.ToInt32(new string(numberChars), BASE_NUMBER);
            if (int32Char >= 0x0 && int32Char <= 0x10FFFF)
            {
              return (char)int32Char;
            }

            goto default;
          }
        default:
          {
            throwTomlSerializerException(parserState, "Unsupported basic escaped char.");
            break;
          }

      }

      //Unreachable
      throw new InvalidOperationException();
    }

    private static bool isEscapedChar(char ch)
    {
      return ch == 0x5c;
    }

    private static bool isBasicUnescapedChar(char ch) => isWhiteSpace(ch) || ch == 0x21 ||
                                                         (ch >= 0x23 && ch <= 0x5B) || (ch >= 0x5D && ch <= 0x7E) ||
                                                         ch.IsNonAsciiChar();

    private static bool isBasicUnescapedChar(int ch) => ch == 0x20 || ch == 0x21 || ch == 0x9 ||
                                                        (ch >= 0x23 && ch <= 0x5B) || (ch >= 0x5D && ch <= 0x7E) ||
                                                        (isNonAsciiChar(ch));

    private static async ValueTask<TomlTable> parseStandardTable<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      try
      {
        parserState.KeyParent = TomlTokenType.Table;
        if (parserState.CurrentChar != '[')
        {
          throwTomlSerializerException(parserState, INVALID_TABLE_DEFINITION_ERROR);
        }

        await advance(parserState).ConfigureAwait(false);
        throwIfEof(parserState);
        var (key, parentTable) = await parseKey(parserState).ConfigureAwait(false);
        var lastKeyPart = key.LastKeyPart;
        await skipWhiteSpaces(parserState).ConfigureAwait(false);
        if (parserState.CurrentChar != ']')
        {
          throwTomlSerializerException(parserState, INVALID_TABLE_DEFINITION_ERROR);
        }

        if (parentTable.TryGetValue(lastKeyPart, out var token))
        {
          if (token.TokenType != TomlTokenType.Table)
          {
            throwTomlSerializerException(parserState, $"Token '{key}' exists and has type '{token.TokenType}'. Expected token type: 'Table'");
          }
        }
        else
        {
          token = new TomlTable(lastKeyPart);
          parserState.CurrentTomlTable.AddInternal(lastKeyPart, token);
        }

        var tableToken = (TomlTable) token;
        if (tableToken.HasTopLevelDefinition || tableToken.HasFromKeyDefinition)
        {
          throwTomlSerializerException(parserState, $"Redefinition of table '{tableToken.Name}' is not allowed.");
        }

        await advance(parserState).ConfigureAwait(false);
        if (!parserState.Eof && isWhiteSpace(parserState.CurrentChar))
        {
          await skipWhiteSpaces(parserState).ConfigureAwait(false);
        }

        if (!parserState.Eof)
        {
          if (parserState.CurrentChar != '#' && !isNewLineChar(parserState.CurrentChar))
          {
            throwTomlSerializerException(parserState, INVALID_TOML_ARRAY_DEFINITION_ERROR);
          }

          if (parserState.CurrentChar == '#')
          {
            _ = await parseComment(parserState).ConfigureAwait(false);
          }
        }

        tableToken.HasTopLevelDefinition = tableToken.HasTopLevelDeclaration = true;
        parserState.CurrentTomlTable = tableToken;
        return tableToken;
      }
      finally
      {
        parserState.KeyParent = TomlTokenType.Undefined;
      }
    }

    private static async ValueTask<TomlComment> parseComment<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      try
      {
        var startCurrentLine = parserState.CurrentLine;
        var commentBuilder = parserState.StringBuilder;
        Debug.Assert(commentBuilder.Length == 0);

        while (parserState.CurrentLine == startCurrentLine)
        {
          await advance(parserState).ConfigureAwait(false);
          if (parserState.Eof || isNewLineChar(parserState))
          {
            break;
          }

          if (!isAllowedCommentChar(parserState.CurrentChar))
          {
            throwTomlSerializerException(parserState, $"Found disallowed char '{(int)parserState.CurrentChar:x}' in comment.");
          }

          commentBuilder.Append(parserState.CurrentChar);
        }

        return new TomlComment(commentBuilder.ToString());
      }
      finally
      {
        parserState.StringBuilder.Clear();
      }
    }

#if NET9_0_OR_GREATER
    [DoesNotReturn]
#endif //if NET9_0_OR_GREATER
    private static void throwTomlSerializerException<TInputReader>(ParserState<TInputReader> parserState, string innerMessage = "")
      where TInputReader : struct, IInputReader
    {
       var exceptionMessage = $"TOML document is invalid. {innerMessage} Line: {parserState.CurrentLine} Column: {parserState.CurrentColumn}";
      throw new TomlSerializerException(exceptionMessage);
    }

    private static bool isAllowedCommentChar(char parserStateCurrentChar)
    {
      if (parserStateCurrentChar == '\t')
      {
        return true;
      }

      return !parserStateCurrentChar.IsTomlControlChar();
    }

    
    private static bool isNonAsciiChar(int c32)
    {
      if (c32 >= 0x80 && c32 <= 0xD7FF)
      {
        return true;
      }

      //TODO: Change this
      //Hack for external tests

      if (c32 >= 0xE000 && c32 <= 0x10FFFF)
      {
        return true;
      }

      return false;
    }

    private static bool isNewLineChar<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader => parserState.CurrentChar == '\r' && peek(parserState) == '\n'
                                                   || parserState.CurrentChar == '\n';
    

    private static void throwIfEof<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      if (parserState.Eof)
      {
        throwEof(parserState);
      }
    }

#if NET9_0_OR_GREATER
    [DoesNotReturn]
#endif
    private static void throwEof<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      throwTomlSerializerException(parserState, UNEXPECTED_END_OF_TOML_DOCUMENT_ERROR);
    }

    private static async ValueTask skipWhitespacesAndNewLines<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      while (!parserState.Eof && (isWhiteSpace(parserState.CurrentChar) || isNewLineChar(parserState)))
      {
        await skipWhiteSpaces(parserState).ConfigureAwait(false);
        await skipNewLine(parserState).ConfigureAwait(false);
      }
    }

    private static async ValueTask skipNewLine<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      if (parserState.CurrentChar == '\r' )
      {
        if (peek(parserState) == '\n')
        {
          await advance(parserState).ConfigureAwait(false);
        }
        else
        {
          throwTomlSerializerException(parserState, @"Invalid bare-cr ('\r') char. Expected '\r\n' or '\n'.S");
        }
      }

      if (parserState.CurrentChar == '\n')
      {
        await advance(parserState).ConfigureAwait(false);
      }
    }

    private static async ValueTask skipWhiteSpaces<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      while (!parserState.Eof && isWhiteSpace(parserState.CurrentChar))
      {
        await advance(parserState).ConfigureAwait(false);
      }
    }

    private static async ValueTask<bool> advance<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      char? readC;
      if ((readC = await parserState.ReadCharAsync().ConfigureAwait(false)) != null)
      {
        parserState.CurrentChar = readC.Value;
        if (parserState.CurrentChar == '\uFFFD')
        {
          throwTomlSerializerException(parserState, "Found invalid utf-8 char (\uFFFD).");
        }
        ++parserState.CurrentColumn;

        if (!parserState.ParsingString && parserState.CurrentChar == '\n')
        {
          ++parserState.CurrentLine;
          parserState.CurrentColumn = 0;
        }

        return true;
      }

      parserState.Eof = true;
      return false;
    }

    private static char? peek<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      return parserState.PeekChar();
    }

    private static bool isWhiteSpace(char c)
    {
      switch (c)
      {
        case ' ':
        case '\t':
          //case '\r':
          //case '\n':
          {
            return true;
          }

      }

      return false;
    }
  }

  internal class ParserState<TInputReader>
    where TInputReader : struct, IInputReader
  {
    private TInputReader _inputReader;
    private TomlTable _currentTomlTable;
    private TomlTable? _lastNonRootTomlTableWithScope;
    private CancellationToken _cancellationToken;

    public ParserState(TInputReader inputReader, TomlSettings tomlSettings)
    {
      _inputReader = inputReader;
      _currentTomlTable = RootTomlTable;
      TomlSettings = tomlSettings;
    }

    public TomlTokenType KeyParent
    {
      get;
      set;
    }

    public bool Eof
    {
      get;
      set;
    }

    public int CurrentLine
    {
      get;
      set;
    }

    public int CurrentColumn
    {
      get;
      set;
    }

    public char CurrentChar
    {
      get;
      set;
    }

    public StringBuilder StringBuilder
    {
      get;
      set;
    } = new StringBuilder();

    public bool ParsingString
    {
      get;
      set;
    }

    public TomlTable RootTomlTable
    {
      get;
    } = new TomlTable(new TomlKey(TomlTable.ROOT_TABLE_NAME, TomlKeyType.SimpleQuotedBasicString));


    public TomlTable CurrentTomlTable
    {
      get => _currentTomlTable;
      set
      {
        _currentTomlTable = value;
        if (!ReferenceEquals(_currentTomlTable, RootTomlTable) && _currentTomlTable.HasTopLevelDefinition || CurrentTomlTable.IsArrayOfTablesMember)
        {
          _lastNonRootTomlTableWithScope = _currentTomlTable;
        }
      }
    }

    public bool ParsingDottedKey
    {
      get;
      set;
    }

    public TomlSettings TomlSettings
    {
      get;
      set;
    }

    public CancellationToken CancellationToken
    {
      get => _cancellationToken;
      set => _cancellationToken = value;
    }

    public TomlTable TryRestoreLastScopedTomlTable()
    {
      _currentTomlTable = _lastNonRootTomlTableWithScope ?? RootTomlTable;
      return _currentTomlTable;
    }

    public char? ReadChar() => _inputReader.ReadChar();
    public char? PeekChar() => _inputReader.Peek();
    public ValueTask<char?> ReadCharAsync() => _inputReader.ReadCharAsync();
    public ValueTask<int> ReadAsync(ArraySegment<char> buffer) => _inputReader.ReadAsync(buffer);
    public int Read(ArraySegment<char> buffer) => _inputReader.Read(buffer);
  }

  internal interface IInputReader
  {
    char? ReadChar();
    ValueTask<char?> ReadCharAsync();
    char? Peek();
    ValueTask<int> ReadAsync(ArraySegment<char> buffer);
    int Read(ArraySegment<char> buffer);
  }

#pragma warning disable IDE0044
  // ReSharper disable FieldCanBeMadeReadOnly.Local
  internal struct TextInputReader : IInputReader
  {
    public const int ONE_CHAR_BUFFER_SIZE = 1;
    public const int NO_CHAR = -1;
    private TextReader _reader;
    private char[] _buffer;

    public TextInputReader(TextReader reader)
    {
      Debug.Assert(reader != null);
      _reader = reader;
      _buffer = new char[1];
    }

    public char? ReadChar()
    {
      var readC = _reader.Read();
      return readC == NO_CHAR
        ? null
        : (char?) readC;
    }

#if NET9_0_OR_GREATER
    public async ValueTask<char?> ReadCharAsync() => await _reader.ReadAsync(_buffer).ConfigureAwait(false) == ONE_CHAR_BUFFER_SIZE
      ? _buffer[0]
      : (char?)null;

#else
    public async ValueTask<char?> ReadCharAsync() => await _reader.ReadAsync(_buffer, 0, ONE_CHAR_BUFFER_SIZE).ConfigureAwait(false) == ONE_CHAR_BUFFER_SIZE
      ? _buffer[0]
      : (char?) null;
#endif //NET9_0_OR_GREATER
    public char? Peek()
    {
      var readC = _reader.Peek();
      return readC == NO_CHAR
        ? null
        : (char?) readC;
    }

    public ValueTask<int> ReadAsync(ArraySegment<char> buffer)
    {
      return new ValueTask<int>(_reader.ReadAsync(buffer.Array!, buffer.Offset, buffer.Count));
    }

    public int Read(ArraySegment<char> buffer)
    {
      return _reader.Read(buffer.Array!, buffer.Offset, buffer.Count);
    }
  }

  internal struct StringInputReader : IInputReader
  {
    
    private string _inputString;
    private int _position;

    public StringInputReader(string inputString)
    {
      _inputString = inputString;
      _position = 0;
    }

    public char? ReadChar()
    {
      if (_position >= _inputString.Length)
      {
        return null;
      }

      return _inputString[_position++];
    }

    public ValueTask<char?> ReadCharAsync()
    {
      return new ValueTask<char?>(ReadChar());
    }

    public char? Peek()
    {
      var peekPosition = _position;
      if (peekPosition >= _inputString.Length)
      {
        return null;
      }

      return _inputString[peekPosition];
    }

    public ValueTask<int> ReadAsync(ArraySegment<char> buffer)
    {
      return new ValueTask<int>(Read(buffer));
    }

    public int Read(ArraySegment<char> buffer)
    {
      for (var i = 0; i < buffer.Count; ++i)
      {
        Debug.Assert(buffer.Array != null);
        buffer.Array[buffer.Offset + i] = _inputString[_position++];
      }

      return buffer.Count;
    }
  }
#pragma warning restore IDE0044
  // ReSharper restore FieldCanBeMadeReadOnly.Local
}