using System;
using System.Collections.Generic;
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
    private const string UNEXPECTED_END_OF_TOML_DOCUMENT_ERROR = "Unexpected end of TOML document";
    private const int UNLIMITED = -1;
    private const string INVALID_DATE_ERROR = "Invalid date (expected format YYYY-MM-DD).";
    private const string INVALID_TIME_ERROR = "Invalid time (expected format HH:MM:SS).";
    private const string INVALID_TIME_OFFSET_ERROR = "Invalid time zone offset (expected 'Z', '+HH:MM' or '-HH:MM').";
    private const string INVALID_FRACTIONAL_SECONDS_ERROR = "Invalid fractional seconds.";
    private const string INVALID_ARRAY_ERROR = "Invalid array.";
    private const string INVALID_INLINE_TABLE_ERROR = "Invalid inline table.";
    private const string PARSING_FAILED_GENERAL_ERROR = "Parsing failed. See inner exception for details.";
    private const int MAX_SINGLE_QUOTES_IN_LITERAL_ML_STRING = 2;
    private const int NUMBER_OF_OPENING_CLOSE_SINGLE_QUOTES_IN_LITERAL_ML_STRING = 3;
    private const string WINDOWS_NEW_LINE = "\r\n";
    private const string DEFAULT_SECONDS_IN_TIME = ":00";

    public static TomlTable Parse(string toml,
                                  TomlSettings? tomlSettings)
    {
      if (toml == null)
      {
        throw new ArgumentNullException(nameof(toml));
      }

      tomlSettings ??= TomlSettings.Default;
      var parserState = new ParserState<StringInputReader>(new StringInputReader(toml), tomlSettings)
      {
        CancellationToken = CancellationToken.None
      };

      return parseToml(parserState, useAsync: false).GetAwaiter().GetResult();
    }

    public static TomlTable Parse(Stream tomlStream,
                                  TomlSettings tomlSettings)
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
          CancellationToken = CancellationToken.None
        };

        return parseToml(parserState, useAsync: false).GetAwaiter().GetResult();
      }
    }

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

      return await parseToml(parserState, useAsync: true).ConfigureAwait(false);
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

        return await parseToml(parserState, useAsync: true).ConfigureAwait(false);
      }
    }

    private static async ValueTask<TomlTable> parseToml<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      try
      {
        await advance(parserState, useAsync).ConfigureAwait(false);
        while (!parserState.Eof)
        {
          parserState.CancellationToken.ThrowIfCancellationRequested();
          await skipWhitespacesAndNewLines(parserState, useAsync).ConfigureAwait(false);
          if (parserState.Eof)
          {
            continue;

          }

          var expression = await parseExpression(parserState, useAsync).ConfigureAwait(false);

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

    private static async ValueTask<TomlExpression> parseExpression<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      await skipWhitespacesAndNewLines(parserState, useAsync).ConfigureAwait(false);
      throwIfEof(parserState);
      TomlExpression expression;
      switch (parserState.CurrentChar)
      {
        case '#':
          {
            expression = await parseComment(parserState, useAsync).ConfigureAwait(false);
            break;
          }
        case '[':
          {
            var peekChar = peek(parserState);
            var parsingArrayOfTables = peekChar == '[';
            parserState.CurrentTomlTable = parserState.RootTomlTable;

            expression = parsingArrayOfTables ? (TomlExpression)await parseArrayOfTables(parserState, useAsync).ConfigureAwait(false)
                                               : await parseStandardTable(parserState, useAsync).ConfigureAwait(false);

            break;
          }
        default:
          {
            expression = await parseKeyValueExpression(parserState, useAsync).ConfigureAwait(false);
            break;
          }
      }

      return expression;
    }

    private static async ValueTask<TomlArray> parseArrayOfTables<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      try
      {
        parserState.KeyParent = TomlTokenType.Array;
        if (parserState.CurrentChar != '[')
        {
          throwTomlSerializerException(parserState, $"Invalid array of tables header: expected '[[', but found {describeChar(parserState.CurrentChar)}.");
        }

        await advance(parserState, useAsync).ConfigureAwait(false);
        throwIfEof(parserState, "'[[' to start the array of tables header");
        if (parserState.CurrentChar != '[')
        {
          throwTomlSerializerException(parserState, $"Invalid array of tables header: expected '[[', but found '[' followed by {describeChar(parserState.CurrentChar)}.");
        }

        await advance(parserState, useAsync).ConfigureAwait(false);
        throwIfEof(parserState, "a table name");

        var (key, parentTable) = await parseKey(parserState, useAsync).ConfigureAwait(false);
        var lastKey = key.LastKeyPart;
        await skipWhiteSpaces(parserState, useAsync).ConfigureAwait(false);
        if (parserState.CurrentChar != ']')
        {
          throwTomlSerializerException(parserState, $"Invalid array of tables header: expected ']]' to close the header, but found {describeCurrentChar(parserState)}.");
        }

        await advance(parserState, useAsync).ConfigureAwait(false);
        throwIfEof(parserState, "']]' to close the array of tables header");
        if (parserState.CurrentChar != ']')
        {
          throwTomlSerializerException(parserState, $"Invalid array of tables header: expected ']]' to close the header, but found ']' followed by {describeChar(parserState.CurrentChar)}.");
        }


        var peekChar = peek(parserState);
        if (peekChar != null && isWhiteSpace(peekChar.Value))
        {
          await advance(parserState, useAsync).ConfigureAwait(false);
          await skipWhiteSpaces(parserState, useAsync).ConfigureAwait(false);
          peekChar = parserState.Eof ? (char?) null : parserState.CurrentChar;
        }

        if (!parserState.Eof && peekChar != null)
        {
          if (peekChar != '#' && !isNewLineChar(peekChar.Value))
          {
            throwTomlSerializerException(parserState, $"Expected end of line or comment after the array of tables header, but found {describeChar(peekChar.Value)}.");
          }

          if (peekChar == '#')
          {
            _ = await parseComment(parserState, useAsync).ConfigureAwait(false);
          }
        }

        if (parentTable.TryGetValue(lastKey, out var token))
        {
          if (token.TokenType != TomlTokenType.ArrayOfTables)
          {
            throwTomlSerializerException(parserState, $"Cannot define array of tables '{key}': the key already exists as '{token.TokenType}'.");
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
          await advance(parserState, useAsync).ConfigureAwait(false);
        }

        return arrayToken;
      }
      finally
      {
        parserState.KeyParent = TomlTokenType.Undefined;
      }
    }

    private static async ValueTask<TomlKeyValue> parseKeyValueExpression<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      try
      {
        parserState.KeyParent = parserState.KeyParent == TomlTokenType.Undefined ? TomlTokenType.KeyValue : parserState.KeyParent;
        var (key, table) = await parseKey(parserState, useAsync).ConfigureAwait(false);

        if (key.IsDottedKey && table.HasTopLevelDefinition)
        {
          throwTomlSerializerException(parserState, $"Cannot add key '{key}' to table '{table.FullName}': the table was explicitly defined with a [header] and cannot be extended with dotted keys.");
        }

        var lastPartKey = key.LastKeyPart;
        if (table.ContainsKey(lastPartKey.RawKey))
        {
          throwTomlSerializerException(parserState, $"Key '{key}' is already defined. Redefinition is not allowed.");
        }

        await skipWhiteSpaces(parserState, useAsync).ConfigureAwait(false);
        throwIfEof(parserState, "'=' after the key");

        if (parserState.CurrentChar != '=')
        {
          throwTomlSerializerException(parserState, $"Expected '=' after key '{key}', but found {describeChar(parserState.CurrentChar)}.");
        }

        await advance(parserState, useAsync).ConfigureAwait(false);
        var value = await parseValue(parserState, useAsync).ConfigureAwait(false);
        if (table.IsArrayOfTablesMember && key.IsDottedKey && value.TokenType == TomlTokenType.PrimitiveValue)
        {
          throwTomlSerializerException(parserState, $"Key '{key}' is an array of tables. Could not append to array of tables via dotted key.");
        }
        await skipWhiteSpaces(parserState, useAsync).ConfigureAwait(false);

        if (parserState.CurrentChar == '#')
        {
          _ = await parseComment(parserState, useAsync).ConfigureAwait(false);
        }

        if (!isNewLineChar(parserState) && parserState.CurrentChar != ',' && parserState.CurrentChar != '}' && !parserState.Eof)
        {
          throwTomlSerializerException(parserState, $"Expected end of line or comment after the value of key '{key}', but found {describeChar(parserState.CurrentChar)}.");
        }

        await skipWhiteSpaces(parserState, useAsync).ConfigureAwait(false);
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

    private static async ValueTask<TomlValue> parseValue<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
        where TInputReader : struct, IInputReader
    {
      try
      {
        parserState.CurrentNestingDepth++;
        if (parserState.CurrentNestingDepth > parserState.TomlSettings.MaxNestingDepth)
        {
          throwTomlSerializerException(parserState, $"Maximum nesting depth ({parserState.TomlSettings.MaxNestingDepth}) exceeded.");
        }

        await skipWhiteSpaces(parserState, useAsync).ConfigureAwait(false);
        throwIfEof(parserState, "a value");

        switch (parserState.CurrentChar)
        {
          case '"':
            {
              var (parsedString, tomlDataType) = await parseBasicStringOrBasicMlString(parserState, useAsync).ConfigureAwait(false);
              return new TomlPrimitiveValue(parsedString, TomlValueType.String, tomlDataType);
            }
          case '\'':
            {
              var (parsedString, tomlDataType) = await parseLiteralStringOrMlLiteralString(parserState, useAsync).ConfigureAwait(false);
              return new TomlPrimitiveValue(parsedString, TomlValueType.String, tomlDataType);
            }
          case '[':
            {
              var tomlArray = await parseArray(parserState, useAsync).ConfigureAwait(false);
              return tomlArray;
            }
          case '{':
            {
              var tomlInlineTable = await parseInlineTable(parserState, useAsync).ConfigureAwait(false);
              tomlInlineTable.HasFromInlineTableDefinition = true;
              return tomlInlineTable;
            }
          case 't':
            {
              if (await matchLiteralValue(parserState, TomlPrimitiveValue.TRUE_VALUE_LITERAL, useAsync).ConfigureAwait(false))
              {
                return new TomlPrimitiveValue(TomlPrimitiveValue.TRUE_VALUE_LITERAL, TomlValueType.Boolean);
              }

              throwTomlSerializerException(parserState, $"Invalid value: expected '{TomlPrimitiveValue.TRUE_VALUE_LITERAL}', but found {describeCurrentChar(parserState)} while reading it. Strings must be quoted.");
              break;
            }
          case 'f':
            if (await matchLiteralValue(parserState, TomlPrimitiveValue.FALSE_VALUE_LITERAL, useAsync).ConfigureAwait(false))
            {
              return new TomlPrimitiveValue(TomlPrimitiveValue.FALSE_VALUE_LITERAL, TomlValueType.Boolean);
            }

            throwTomlSerializerException(parserState, $"Invalid value: expected '{TomlPrimitiveValue.FALSE_VALUE_LITERAL}', but found {describeCurrentChar(parserState)} while reading it. Strings must be quoted.");
            break;
        }

        if (char.IsDigit(parserState.CurrentChar) || parserState.CurrentChar == '+' || parserState.CurrentChar == '-' ||
            parserState.CurrentChar == 'i' || parserState.CurrentChar == 'n')
        {
          var (valueCandidateType, parsedValue, hasNumber) = await parseNumber(parserState, useAsync).ConfigureAwait(false);
          if (hasNumber)
          {
            return new TomlPrimitiveValue(parsedValue, valueCandidateType.ToTomlValueType(), valueCandidateType);
          }

          Debug.Assert((valueCandidateType & TomlDataType.AllDateTimeTypes) != 0);
          var (subType, dateTimeValue) = await parseDateTime(parserState, parsedValue, useAsync).ConfigureAwait(false);
          Debug.Assert(subType != TomlDataType.Unspecified);
          return new TomlPrimitiveValue(dateTimeValue, TomlValueType.DateTime, subType);
        }

        throwTomlSerializerException(parserState, $"Unexpected {describeChar(parserState.CurrentChar)} where a value was expected. A value must be a string, number, boolean, date-time, array or inline table (strings must be quoted).");

        return null;
      }
      finally
      {
        parserState.CurrentNestingDepth--;
        Debug.Assert(parserState.CurrentNestingDepth >= 0);
      }
    }

    private static async ValueTask<(TomlDataType subtype, string dateValue)> parseDateTime<TInputReader>(ParserState<TInputReader> parserState,
                                                                                                         string firstDateOrTimePart,
                                                                                                         bool useAsync)
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
          await parseFullDate(parserState, firstDateOrTimePart, useAsync).ConfigureAwait(false);
          await advance(parserState, useAsync).ConfigureAwait(false);
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

            await advance(parserState, useAsync).ConfigureAwait(false);
            if (parserState.CurrentChar.Equals('#'))
            {
              if (mustHaveDateTime)
              {
                throwTomlSerializerException(parserState, "Invalid date-time: expected a time after 'T', but found a comment.");
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
            await parsePartialTime(parserState, useAsync).ConfigureAwait(false);
          }
          else
          {
            candidateTypes = TomlDataType.LocalDate;
            return (candidateTypes, parserState.StringBuilder.ToString());
          }

          await advance(parserState, useAsync).ConfigureAwait(false);
          if (parserState.Eof || (parserState.CurrentChar != 'Z' && parserState.CurrentChar != 'z' && parserState.CurrentChar != '+' && parserState.CurrentChar != '-'))
          {
            candidateTypes = TomlDataType.LocalDateTime;
            return (candidateTypes, parserState.StringBuilder.ToString());
          }

          candidateTypes = TomlDataType.OffsetDateTime;
          await parseTimeOffset(parserState, useAsync).ConfigureAwait(false);
          await advance(parserState, useAsync).ConfigureAwait(false);
          return (candidateTypes, parserState.StringBuilder.ToString());
        }
        else if (parserState.CurrentChar == ':')
        {
          candidateTypes = TomlDataType.LocalTime;
          await parsePartialTime(parserState, useAsync, firstDateOrTimePart).ConfigureAwait(false);
          await advance(parserState, useAsync).ConfigureAwait(false);
          return (candidateTypes, parserState.StringBuilder.ToString());
        }
      }
      finally
      {
        parserState.StringBuilder.Clear();

      }

      throw new UnreachableException();
    }

    private static async ValueTask parseTimeOffset<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
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
        await advance(parserState, useAsync).ConfigureAwait(false);
        throwIfEof(parserState, "hours of the time zone offset");
        await parseTimeHour(parserState, useAsync).ConfigureAwait(false);

        await advance(parserState, useAsync).ConfigureAwait(false);
        throwIfEof(parserState, "':' in the time zone offset");
        expectChar(parserState, ':', INVALID_TIME_OFFSET_ERROR);
        parserState.StringBuilder.Append(parserState.CurrentChar);

        await advance(parserState, useAsync).ConfigureAwait(false);
        throwIfEof(parserState, "minutes of the time zone offset");
        await parseTimeMinute(parserState, useAsync).ConfigureAwait(false);
        return;
      }

      throwTomlSerializerException(parserState, $"{INVALID_TIME_OFFSET_ERROR} Found {describeChar(parserState.CurrentChar)}.");
    }

    private static async ValueTask parsePartialTime<TInputReader>(ParserState<TInputReader> parserState,
                                                                  bool useAsync,
                                                                  string? firstDateOrTimePart = null)
      where TInputReader : struct, IInputReader
    {

      await parseTimeHour(parserState, useAsync, firstDateOrTimePart).ConfigureAwait(false);
      if (firstDateOrTimePart == null)
      {
        await advance(parserState, useAsync).ConfigureAwait(false);
      }

      throwIfEof(parserState, "':' after the hour");
      expectChar(parserState, ':', INVALID_TIME_ERROR);
      parserState.StringBuilder.Append(parserState.CurrentChar);
      await advance(parserState, useAsync).ConfigureAwait(false);
      throwIfEof(parserState, "minutes");
      await parseTimeMinute(parserState, useAsync).ConfigureAwait(false);

      var peekChar = peek(parserState);

      if (peekChar != ':' && parserState.TomlSettings.TomlVersion == TomlVersion.Toml11)
      {
        //This is workaround for external tests
        parserState.StringBuilder.Append(DEFAULT_SECONDS_IN_TIME);
        return;
      }

      await advance(parserState, useAsync).ConfigureAwait(false);
      throwIfEof(parserState, "':' after the minutes");
      expectChar(parserState, ':', INVALID_TIME_ERROR);
      parserState.StringBuilder.Append(parserState.CurrentChar);
      await advance(parserState, useAsync).ConfigureAwait(false);
      throwIfEof(parserState, "seconds");
      await parseTimeSeconds(parserState, useAsync).ConfigureAwait(false);

      peekChar = peek(parserState);
      if (peekChar != '.')
      {
        return;
      }

      await advance(parserState, useAsync).ConfigureAwait(false);
      await parseTimeSecFraction(parserState, useAsync).ConfigureAwait(false);
    }

    private static async ValueTask parseTimeSecFraction<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      expectChar(parserState, '.', INVALID_FRACTIONAL_SECONDS_ERROR);
      parserState.StringBuilder.Append(parserState.CurrentChar);
      await advance(parserState, useAsync).ConfigureAwait(false);
      throwIfEof(parserState, "digits after '.'");
      if (!char.IsDigit(parserState.CurrentChar))
      {
        throwTomlSerializerException(parserState, $"{INVALID_FRACTIONAL_SECONDS_ERROR} Expected a digit after '.', but found {describeChar(parserState.CurrentChar)}.");
      }

      await parseDigits(parserState, useAsync).ConfigureAwait(false);
    }

    private static async ValueTask parseDigits<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
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
          await advance(parserState, useAsync).ConfigureAwait(false);
        }
      } while (isDigit);
    }

    private static async ValueTask<int> parseTimeSeconds<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {

      if (!char.IsDigit(parserState.CurrentChar) || parserState.CurrentChar - '0' > 5)
      {
        throwTomlSerializerException(parserState, $"Invalid seconds: expected two digits 00-59, but found {describeDigitPair(parserState)}.");
      }

      var secondsC1 = parserState.CurrentChar;
      parserState.StringBuilder.Append(parserState.CurrentChar);
      await advance(parserState, useAsync).ConfigureAwait(false);
      throwIfEof(parserState, "the second digit of the seconds");
      var secondsC2 = parserState.CurrentChar;
      if (!char.IsDigit(parserState.CurrentChar) || !rawSecondsToIntSeconds(secondsC1, secondsC2, out var seconds))
      {
        throwTomlSerializerException(parserState, $"Invalid seconds: expected 00-59, but found '{secondsC1}{secondsC2}'.");
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

    private static async ValueTask<int> parseTimeMinute<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      if (!char.IsDigit(parserState.CurrentChar) || parserState.CurrentChar - '0' > 5)
      {
        throwTomlSerializerException(parserState, $"Invalid minutes: expected two digits 00-59, but found {describeDigitPair(parserState)}.");
      }

      var minuteC1 = parserState.CurrentChar;
      parserState.StringBuilder.Append(parserState.CurrentChar);
      await advance(parserState, useAsync).ConfigureAwait(false);
      throwIfEof(parserState, "the second digit of the minutes");
      var minuteC2 = parserState.CurrentChar;
      if (!char.IsDigit(parserState.CurrentChar) || !rawMinuteToIntMinute(minuteC1, minuteC2, out var minute))
      {
        throwTomlSerializerException(parserState, $"Invalid minutes: expected 00-59, but found '{minuteC1}{minuteC2}'.");
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
                                                                    bool useAsync,
                                                                    string? firstDateOrTimePart = null)
      where TInputReader : struct, IInputReader
    {
      if (firstDateOrTimePart != null)
      {
        if (!rawHourToIntHour(firstDateOrTimePart, out var intHour))
        {
          throwTomlSerializerException(parserState, $"Invalid hour: expected two digits 00-23, but found '{firstDateOrTimePart}'.");
        }

        parserState.StringBuilder.Append(firstDateOrTimePart);
        return intHour;
      }
      if (!char.IsDigit(parserState.CurrentChar))
      {
        throwTomlSerializerException(parserState, $"Invalid hour: expected two digits 00-23, but found {describeChar(parserState.CurrentChar)}.");
      }

      expectChar(parserState, '0', '1', '2', "Invalid hour: expected two digits 00-23.");
      var hourC1 = parserState.CurrentChar;
      parserState.StringBuilder.Append(parserState.CurrentChar);
      await advance(parserState, useAsync).ConfigureAwait(false);
      throwIfEof(parserState, "the second digit of the hour");
      var hourC2 = parserState.CurrentChar;
      if (!char.IsDigit(parserState.CurrentChar) || !rawHourToIntHour(hourC1, hourC2, out var hour))
      {
        throwTomlSerializerException(parserState, $"Invalid hour: expected 00-23, but found '{hourC1}{hourC2}'.");
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
                                                               string firstDatePart,
                                                               bool useAsync)
      where TInputReader : struct, IInputReader

    {
      Debug.Assert(parserState.StringBuilder.Length == 0);
      var year = await parseYear(firstDatePart, parserState).ConfigureAwait(false);
      expectChar(parserState, '-', INVALID_DATE_ERROR);
      parserState.StringBuilder.Append(parserState.CurrentChar);
      await advance(parserState, useAsync).ConfigureAwait(false);
      throwIfEof(parserState, "the month");
      var month = await parseMonth(parserState, useAsync).ConfigureAwait(false);
      await advance(parserState, useAsync).ConfigureAwait(false);
      throwIfEof(parserState, "'-' after the month");
      expectChar(parserState, '-', INVALID_DATE_ERROR);
      parserState.StringBuilder.Append(parserState.CurrentChar);
      await advance(parserState, useAsync).ConfigureAwait(false);
      throwIfEof(parserState, "the day");
      await parseMonthDay(parserState, year, month, useAsync).ConfigureAwait(false);
    }

    private static async ValueTask<int> parseMonthDay<TInputReader>(ParserState<TInputReader> parserState,
                                                                    int year,
                                                                    int month,
                                                                    bool useAsync)
      where TInputReader : struct, IInputReader
    {

      if (!char.IsDigit(parserState.CurrentChar))
      {
        throwTomlSerializerException(parserState, $"Invalid day: expected two digits 01-31, but found {describeChar(parserState.CurrentChar)}.");
      }
      var dayC1 = parserState.CurrentChar;
      parserState.StringBuilder.Append(parserState.CurrentChar);
      await advance(parserState, useAsync).ConfigureAwait(false);
      throwIfEof(parserState, "the second digit of the day");
      var dayC2 = parserState.CurrentChar;
      if (!char.IsDigit(parserState.CurrentChar) || !rawMonthDayToMonthDayInt(parserState, dayC1, dayC2, year, month, out var day))
      {
        throwTomlSerializerException(parserState, $"Invalid day: expected two digits 01-31, but found '{dayC1}{dayC2}'.");
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
        throwTomlSerializerException(parserState, $"Invalid day: expected two digits 01-31, but found '{dayC1}{dayC2}'.");
      }

      if (!DateTime.TryParseExact($"{year}/{month}/{day}", new[] { "yyyy/MM/dd", "yyyy/M/dd", "yyyy/MM/d", "yyyy/M/d", "y/M/d" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var _))
      {
        throwTomlSerializerException(parserState, $"Invalid date: '{year:D4}-{month:D2}-{dayC1}{dayC2}' does not exist in the calendar.");
      }

      return true;
    }

    private static async ValueTask<int> parseMonth<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      if (!char.IsDigit(parserState.CurrentChar))
      {
        throwTomlSerializerException(parserState, $"Invalid month: expected two digits 01-12, but found {describeChar(parserState.CurrentChar)}.");
      }

      expectChar(parserState, '0', '1', "Invalid month: expected two digits 01-12.");
      var monthC1 = parserState.CurrentChar;
      parserState.StringBuilder.Append(parserState.CurrentChar);
      await advance(parserState, useAsync).ConfigureAwait(false);
      throwIfEof(parserState, "the second digit of the month");
      var monthC2 = parserState.CurrentChar;
      if (!char.IsDigit(parserState.CurrentChar) || !rawMonthToIntMonth(monthC1, monthC2, out var month))
      {
        throwTomlSerializerException(parserState, $"Invalid month: expected 01-12, but found '{monthC1}{monthC2}'.");
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
        throwTomlSerializerException(parserState, $"Invalid year: expected four digits, but found '{yearCandidate}'.");
        //Unreachable
        return new ValueTask<int>(-1);
      }

      parserState.StringBuilder.Append(yearCandidate);
      return new ValueTask<int>(year);
    }

    private static async ValueTask<TomlInlineTable> parseInlineTable<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      var oldTable = parserState.CurrentTomlTable;
      Debug.Assert(oldTable != null);
      var constructEntered = false;
      try
      {
        if (parserState.CurrentChar != '{')
        {
          throwTomlSerializerException(parserState, $"{INVALID_INLINE_TABLE_ERROR} Expected '{{', but found {describeChar(parserState.CurrentChar)}.");
        }

        enterConstruct(parserState, "inline table");
        constructEntered = true;

        var newInlineTable = new TomlInlineTable(new TomlKey(TomlTable.ANONYMOUS_TABLE_NAME, TomlKeyType.SimpleQuotedBasicString));
        while (parserState.CurrentChar != '}')
        {
          await advance(parserState, useAsync).ConfigureAwait(false);
          await skipWhiteSpaces(parserState, useAsync).ConfigureAwait(false);
          throwIfEof(parserState);

          if (parserState.CurrentChar == '}')
          {
            if (newInlineTable.Count > 0 && parserState.TomlSettings.TomlVersion == TomlVersion.Toml10)
            {
              //Trailing comma not allowed
              throwTomlSerializerException(parserState, $"{INVALID_INLINE_TABLE_ERROR} A trailing comma is not allowed in TOML 1.0.");
            }

            continue;
          }

          //Empty inline table with comma
          if (parserState.CurrentChar == ',')
          {
            throwTomlSerializerException(parserState, $"{INVALID_INLINE_TABLE_ERROR} Unexpected ',': a key was expected.");
          }

          if (isNewLineChar(parserState) || parserState.CurrentChar == '#')
          {
            if (parserState.TomlSettings.TomlVersion == TomlVersion.Toml10)
            {
              throwTomlSerializerException(parserState, $"{INVALID_INLINE_TABLE_ERROR} Newlines and comments are not allowed inside an inline table in TOML 1.0.");
            }
            else
            {
              await parseWsCommentNewLine(parserState, useAsync).ConfigureAwait(false);
              if (parserState.CurrentChar == '}')
              {
                continue;
              }
            }
          }

          parserState.CurrentTomlTable = newInlineTable;
          var keyValueExpression = await parseKeyValueExpression(parserState, useAsync).ConfigureAwait(false);
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
            await skipWhiteSpaces(parserState, useAsync).ConfigureAwait(false);
          }
          else
          {
            await parseWsCommentNewLine(parserState, useAsync).ConfigureAwait(false);
          }

          expectChar(parserState, ',', '}', INVALID_INLINE_TABLE_ERROR);
        }

        await advance(parserState, useAsync).ConfigureAwait(false);
        return newInlineTable;
      }
      finally
      {
        if (constructEntered)
        {
          exitConstruct(parserState);
        }

        parserState.CurrentTomlTable = oldTable;
        parserState.KeyParent = TomlTokenType.Undefined;
      }
    }

    private static async ValueTask<TomlArray> parseArray<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      if (parserState.CurrentChar != '[')
      {
        throwTomlSerializerException(parserState, $"{INVALID_ARRAY_ERROR} Expected '[', but found {describeChar(parserState.CurrentChar)}.");
      }

      var tomlArray = new TomlArray(new TomlKey(TomlArray.ANONYMOUS_ARRAY_NAME, TomlKeyType.SimpleQuotedBasicString));

      enterConstruct(parserState, "array");
      try
      {
        while (parserState.CurrentChar != ']')
        {
          if (!await advance(parserState, useAsync).ConfigureAwait(false))
          {
            throwEof(parserState, "an array value or ']'");
          }

          await parseWsCommentNewLine(parserState, useAsync).ConfigureAwait(false);

          if (parserState.CurrentChar == ']')
          {
            continue;
          }

          var arrayValue = await parseValue(parserState, useAsync).ConfigureAwait(false);
          tomlArray.Add(arrayValue);
          await parseWsCommentNewLine(parserState, useAsync).ConfigureAwait(false);
          expectChar(parserState, ',', ']', INVALID_ARRAY_ERROR);
        }
      }
      finally
      {
        exitConstruct(parserState);
      }

      await advance(parserState, useAsync).ConfigureAwait(false);
      return tomlArray;
    }

    private static async ValueTask parseWsCommentNewLine<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      await skipWhitespacesAndNewLines(parserState, useAsync).ConfigureAwait(false);
      throwIfEof(parserState);
      while (parserState.CurrentChar == '#')
      {
        _ = await parseComment(parserState, useAsync).ConfigureAwait(false);
        await advance(parserState, useAsync).ConfigureAwait(false);
        throwIfEof(parserState);
        await skipWhitespacesAndNewLines(parserState, useAsync).ConfigureAwait(false);
      }
    }

    private static async ValueTask<(TomlDataType valueCandidateTypes, string parsedValue, bool hasNumber)> parseNumber<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
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
          await advance(parserState, useAsync).ConfigureAwait(false);
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
              throwTomlSerializerException(parserState, "Invalid number: expected digits, but reached the end of the document.");
            }

            valueCandidateTypes = TomlDataType.IntegerDec;
            return (valueCandidateTypes, normalizeZeroNumber(builder.ToString()), hasNumber: true);
          }

          if (readIndex == 0)
          {
            if (!char.IsDigit(parserState.CurrentChar) && parserState.CurrentChar != 'i' && parserState.CurrentChar != 'n')
            {
              throwTomlSerializerException(parserState, $"Invalid number: expected a digit, 'inf' or 'nan' after the sign, but found {describeChar(parserState.CurrentChar)}.");
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
              throwTomlSerializerException(parserState, "Invalid number: 'inf' can only be preceded by a sign.");
            }

            valueCandidateTypes = TomlDataType.Float;
            await parseSpecialFloatInf(parserState, useAsync).ConfigureAwait(false);
            return (valueCandidateTypes, builder.ToString(), hasNumber: true);
          }

          if (readIndex == 0 && parserState.CurrentChar == 'n')
          {
            if (builder.Length > 0 && builder[0] != '+' && builder[0] != '-')
            {
              throwTomlSerializerException(parserState, "Invalid number: 'nan' can only be preceded by a sign.");
            }

            valueCandidateTypes = TomlDataType.Float;
            await parseSpecialFloatNan(parserState, useAsync).ConfigureAwait(false);
            return (valueCandidateTypes, builder.ToString(), hasNumber: true);
          }

          if (readIndex == 1)
          {
            if (parserState.CurrentChar == 'x')
            {
              if (builder[0] != '0')
              {
                throwTomlSerializerException(parserState, "Invalid number: unexpected 'x'. Hexadecimal integers must start with '0x' and cannot have a sign.");
              }

              valueCandidateTypes = TomlDataType.IntegerHex;
              var peekChar = peek(parserState);
              if (peekChar == null || !isHexDigit(peekChar.Value))
              {
                throwTomlSerializerException(parserState, $"Invalid number: expected a hexadecimal digit after '0x', but found {describeChar(peekChar)}.");
              }

              builder.Append(parserState.CurrentChar);
              await advance(parserState, useAsync).ConfigureAwait(false);
              await parseHexNumber(parserState, useAsync).ConfigureAwait(false);
              return (valueCandidateTypes, builder.ToString(), hasNumber: true);
            }
            else if (parserState.CurrentChar == 'o')
            {
              if (builder[0] != '0')
              {
                throwTomlSerializerException(parserState, "Invalid number: unexpected 'o'. Octal integers must start with '0o' and cannot have a sign.");
              }

              valueCandidateTypes = TomlDataType.IntegerOct;
              var peekChar = peek(parserState);
              if (peekChar == null || !isOctalDigit(peekChar.Value))
              {
                throwTomlSerializerException(parserState, $"Invalid number: expected an octal digit (0-7) after '0o', but found {describeChar(peekChar)}.");
              }

              builder.Append(parserState.CurrentChar);
              await advance(parserState, useAsync).ConfigureAwait(false);
              await parseOctInt(parserState, useAsync).ConfigureAwait(false);
              return (valueCandidateTypes, builder.ToString(), hasNumber: true);
            }
            else if (parserState.CurrentChar == 'b')
            {
              if (builder[0] != '0')
              {
                throwTomlSerializerException(parserState, "Invalid number: unexpected 'b'. Binary integers must start with '0b' and cannot have a sign.");
              }

              valueCandidateTypes = TomlDataType.IntegerBin;
              var peekChar = peek(parserState);
              if (peekChar == null || !isBinaryDigit(peekChar.Value))
              {
                throwTomlSerializerException(parserState, $"Invalid number: expected a binary digit (0 or 1) after '0b', but found {describeChar(peekChar)}.");
              }

              builder.Append(parserState.CurrentChar);
              await advance(parserState, useAsync).ConfigureAwait(false);
              await parseBinInt(parserState, useAsync).ConfigureAwait(false);
              return (valueCandidateTypes, builder.ToString(), hasNumber: true);
            }
            else
            {
              var canBeIntDecOrFloat = (valueCandidateTypes & (TomlDataType.IntegerDec | valueCandidateTypes | TomlDataType.Float)) != 0;
              var canBeDate = (valueCandidateTypes & TomlDataType.AllDateTimeTypes) != 0;
              await parseUnsignedDecInt(parserState, useAsync).ConfigureAwait(false);
              if (canBeIntDecOrFloat && parserState.CurrentChar == 'e' || parserState.CurrentChar == 'E')
              {
                valueCandidateTypes = TomlDataType.Float;

                if (isInvalidZeroPrefixedNumber(builder))
                {
                  throwTomlSerializerException(parserState, "Invalid number: leading zeros are not allowed.");
                }

                await parseFloatExponent(parserState, useAsync).ConfigureAwait(false);
                return (valueCandidateTypes, builder.ToString(), hasNumber: true);
              }
              else if (canBeIntDecOrFloat && parserState.CurrentChar == '.')
              {
                valueCandidateTypes = TomlDataType.Float;

                if (isInvalidZeroPrefixedNumber(builder))
                {
                  throwTomlSerializerException(parserState, "Invalid number: leading zeros are not allowed.");
                }

                await parseFloatFraction(parserState, useAsync).ConfigureAwait(false);
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
                  throwTomlSerializerException(parserState, "Invalid number: leading zeros are not allowed.");
                }

                valueCandidateTypes = TomlDataType.IntegerDec;
                return (valueCandidateTypes, normalizeZeroNumber(builder.ToString()), hasNumber: true);
              }
              else
              {
                throwTomlSerializerException(parserState, $"Invalid number: unexpected {describeChar(parserState.CurrentChar)}.");
              }
            }
          }

          if (char.IsDigit(parserState.CurrentChar))
          {
            builder.Append(parserState.CurrentChar);
          }

          await advance(parserState, useAsync).ConfigureAwait(false);
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

    private static async ValueTask parseSpecialFloatNan<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      if (!await matchLiteralValue(parserState, TomlPrimitiveValue.SPECIAL_FLOAT_NAN_LITERAL, useAsync).ConfigureAwait(false))
      {
        throwTomlSerializerException(parserState, $"Invalid number: expected '{TomlPrimitiveValue.SPECIAL_FLOAT_NAN_LITERAL}', but found {describeCurrentChar(parserState)} while reading it.");
      }

      parserState.StringBuilder.Append(TomlPrimitiveValue.SPECIAL_FLOAT_NAN_LITERAL);
    }

    private static async ValueTask parseSpecialFloatInf<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      if (!await matchLiteralValue(parserState, TomlPrimitiveValue.SPECIAL_FLOAT_INF_LITERAL, useAsync).ConfigureAwait(false))
      {
        throwTomlSerializerException(parserState, $"Invalid number: expected '{TomlPrimitiveValue.SPECIAL_FLOAT_INF_LITERAL}', but found {describeCurrentChar(parserState)} while reading it.");
      }

      parserState.StringBuilder.Append(TomlPrimitiveValue.SPECIAL_FLOAT_INF_LITERAL);
    }

    private static async ValueTask parseFloatFraction<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      Debug.Assert(parserState.CurrentChar == '.');
      var builder = parserState.StringBuilder;
      builder.Append(parserState.CurrentChar);
      await advance(parserState, useAsync).ConfigureAwait(false);
      throwIfEof(parserState, "digits after '.'");

      if (!char.IsDigit(parserState.CurrentChar))
      {
        throwTomlSerializerException(parserState, $"Invalid number: expected a digit after '.', but found {describeChar(parserState.CurrentChar)}.");
      }

      await parseUnsignedDecInt(parserState, useAsync).ConfigureAwait(false);
      if (parserState.CurrentChar == 'e' || parserState.CurrentChar == 'E')
      {
        await parseFloatExponent(parserState, useAsync).ConfigureAwait(false);
      }
    }

    private static async ValueTask parseFloatExponent<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      Debug.Assert(parserState.CurrentChar == 'e' || parserState.CurrentChar == 'E');
      var builder = parserState.StringBuilder;
      builder.Append(parserState.CurrentChar);
      await advance(parserState, useAsync).ConfigureAwait(false);
      throwIfEof(parserState, "the exponent");
      if (parserState.CurrentChar == '+' || parserState.CurrentChar == '-')
      {
        builder.Append(parserState.CurrentChar);
        await advance(parserState, useAsync).ConfigureAwait(false);
      }

      if (parserState.Eof || !char.IsDigit(parserState.CurrentChar))
      {
        throwTomlSerializerException(parserState, $"Invalid number: expected a digit in the exponent, but found {describeCurrentChar(parserState)}.");
      }

      await parseUnsignedDecInt(parserState, useAsync).ConfigureAwait(false);
    }

    private static async ValueTask parseBinInt<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
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
          throwTomlSerializerException(parserState, "Invalid number: '_' must be placed between two digits.");
        }

        await advance(parserState, useAsync).ConfigureAwait(false);
      }
    }

    private static bool isBinaryDigit(char ch) => ch == '0' || ch == '1';

    private static async ValueTask parseOctInt<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
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
          throwTomlSerializerException(parserState, "Invalid number: '_' must be placed between two digits.");
        }
        await advance(parserState, useAsync).ConfigureAwait(false);
      }
    }

    private static bool isOctalDigit(char ch) => (ch >= '0' && ch <= '7');

    private static bool isAsciiHexDigit(char ch) => (ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'F') || (ch >= 'a' && ch <= 'f');

    private static bool isHexDigit(char ch) => char.IsDigit(ch) || (ch >= 'A' && ch <= 'F') || ((ch >= 'a' && ch <= 'f'));

    private static async ValueTask parseHexNumber<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
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
          throwTomlSerializerException(parserState, "Invalid number: '_' must be placed between two digits.");
        }

        await advance(parserState, useAsync).ConfigureAwait(false);
      }
    }

    private static async ValueTask parseUnsignedDecInt<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
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
          throwTomlSerializerException(parserState, "Invalid number: '_' must be placed between two digits.");
        }
        await advance(parserState, useAsync).ConfigureAwait(false);
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
                                                                         string literal,
                                                                         bool useAsync)
      where TInputReader : struct, IInputReader
    {
      foreach (var l in literal)
      {
        if (parserState.CurrentChar == l)
        {
          await advance(parserState, useAsync).ConfigureAwait(false);
          continue;
        }

        return false;
      }

      return true;
    }

    private static async ValueTask<(string parsedString, TomlDataType tomlDataType)> parseLiteralStringOrMlLiteralString<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      Debug.Assert(parserState.CurrentChar == '\'');

      if (peek(parserState) != '\'')
      {
        return (await parseLiteralString(parserState, useAsync).ConfigureAwait(false), TomlDataType.LiteralString);
      }

      var startLine = parserState.CurrentLine;
      var startColumn = parserState.CurrentColumn;
      await advance(parserState, useAsync).ConfigureAwait(false);

      if (peek(parserState) != '\'')
      {
        await advance(parserState, useAsync).ConfigureAwait(false);
        return (String.Empty, TomlDataType.LiteralString);
      }

      await advance(parserState, useAsync).ConfigureAwait(false);
      return (await parseLiteralMlString(parserState, startLine, startColumn, useAsync).ConfigureAwait(false), TomlDataType.LiteralMlString);
    }

    private static async ValueTask<string> parseLiteralMlString<TInputReader>(ParserState<TInputReader> parserState,
                                                                              int startLine,
                                                                              int startColumn,
                                                                              bool useAsync)
      where TInputReader : struct, IInputReader
    {
      var literalMlBuilder = parserState.StringBuilder;
      Debug.Assert(parserState.CurrentChar == '\'');
      Debug.Assert(literalMlBuilder.Length == 0);
      enterConstruct(parserState, "multiline literal string", startLine, startColumn);
      try
      {
        await advance(parserState, useAsync).ConfigureAwait(false);
        await skipNewLine(parserState, useAsync).ConfigureAwait(false);
        while (true)
        {
          throwIfEof(parserState);
          if (isMllChar(parserState.CurrentChar))
          {
            literalMlBuilder.Append(parserState.CurrentChar);
            await advance(parserState, useAsync).ConfigureAwait(false);
          }
          else if (isNewLineChar(parserState))
          {
            literalMlBuilder.Append(WINDOWS_NEW_LINE);
            await skipNewLine(parserState, useAsync).ConfigureAwait(false);
          }
          else if (parserState.CurrentChar == '\'')
          {
            var isStringEnd = false;
            var toWriteQuotes = 1;
            var totalQuotes = 1;
            await advance(parserState, useAsync).ConfigureAwait(false);
            throwIfEof(parserState);
            if (parserState.CurrentChar == '\'')
            {
              await advance(parserState, useAsync).ConfigureAwait(false);
              throwIfEof(parserState);
              ++toWriteQuotes;
              ++totalQuotes;

              if (parserState.CurrentChar == '\'')
              {
                isStringEnd = true;
                toWriteQuotes = 0;
                ++totalQuotes;
                while (await advance(parserState, useAsync).ConfigureAwait(false) && parserState.CurrentChar == '\'')
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
            throwTomlSerializerException(parserState, $"Character {describeChar(parserState.CurrentChar)} is not allowed in a multiline literal string.");
          }
        }
      }
      finally
      {
        exitConstruct(parserState);
        literalMlBuilder.Clear();
      }
    }

    private static bool isMllChar(char ch) => isLiteralChar(ch);

    private static async ValueTask<(string parserString, TomlDataType tomlDataType)> parseBasicStringOrBasicMlString<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      Debug.Assert(parserState.CurrentChar == '"');
      if (peek(parserState) != '"')
      {
        return (await parseBasicString(parserState, useAsync).ConfigureAwait(false), TomlDataType.BasicString);
      }

      var startLine = parserState.CurrentLine;
      var startColumn = parserState.CurrentColumn;
      await advance(parserState, useAsync).ConfigureAwait(false);
      if (peek(parserState) != '"')
      {
        await advance(parserState, useAsync).ConfigureAwait(false);
        return (String.Empty, TomlDataType.BasicString);
      }

      await advance(parserState, useAsync).ConfigureAwait(false);

      return (await parseBasicMlString(parserState, startLine, startColumn, useAsync).ConfigureAwait(false), TomlDataType.BasicMlString);
    }

    private static async ValueTask<string> parseBasicMlString<TInputReader>(ParserState<TInputReader> parserState,
                                                                            int startLine,
                                                                            int startColumn,
                                                                            bool useAsync)
      where TInputReader : struct, IInputReader
    {
      var basicMlBuilder = parserState.StringBuilder;
      Debug.Assert(parserState.CurrentChar == '"');
      Debug.Assert(basicMlBuilder.Length == 0);
      enterConstruct(parserState, "multiline basic string", startLine, startColumn);
      try
      {
        await advance(parserState, useAsync).ConfigureAwait(false);
        await skipNewLine(parserState, useAsync).ConfigureAwait(false);
        while (true)
        {
          throwIfEof(parserState);
          if (isMlbUnescapedChar(parserState.CurrentChar))
          {
            basicMlBuilder.Append(parserState.CurrentChar);
            await advance(parserState, useAsync).ConfigureAwait(false);
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
              await parseMlbEscapedNl(parserState, useAsync).ConfigureAwait(false);
            }
            else
            {
              basicMlBuilder.Append(await parseBasicEscapedChar(parserState, useAsync).ConfigureAwait(false));
              await advance(parserState, useAsync).ConfigureAwait(false);
            }
          }
          else if (isNewLineChar(parserState))
          {
            basicMlBuilder.Append(WINDOWS_NEW_LINE);
            await skipNewLine(parserState, useAsync).ConfigureAwait(false);
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
                  await advance(parserState, useAsync).ConfigureAwait(false);
                  throwIfEof(parserState);
                  break;
                case 2:
                  toWriteQuotes = 0;
                  ++foundQuotes;
                  isStringEnd = true;
                  await advance(parserState, useAsync).ConfigureAwait(false);
                  break;
                case 3:
                case 4:

                  ++foundQuotes;
                  ++toWriteQuotes;
                  await advance(parserState, useAsync).ConfigureAwait(false);
                  break;
                default:
                  {
                    throwTomlSerializerException(parserState, "Invalid multiline basic string. No more than two consecutive '\"' characters are allowed inside the string (escape them with a backslash).");
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
            throwTomlSerializerException(parserState, $"Character {describeChar(parserState.CurrentChar)} is not allowed in a multiline basic string.");
          }
        }
      }
      finally
      {
        exitConstruct(parserState);
        parserState.StringBuilder.Clear();
      }
    }

    private static bool isNewLineChar(char ch) => ch == '\n' || ch == '\r';

    private static async ValueTask parseMlbEscapedNl<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      Debug.Assert(isEscapedChar(parserState.CurrentChar));
      Debug.Assert(isWhiteSpace(peek(parserState)!.Value) || isNewLineChar(peek(parserState)!.Value));

      var foundEol = false;
      await advance(parserState, useAsync).ConfigureAwait(false);
      while (true)
      {
        throwIfEof(parserState);
        if (isNewLineChar(parserState))
        {
          foundEol = true;
          await skipNewLine(parserState, useAsync).ConfigureAwait(false);
        }
        else if (!isWhiteSpace(parserState.CurrentChar))
        {
          if (!foundEol)
          {
            throwTomlSerializerException(parserState, "Invalid multiline basic string. A line-ending backslash ('\\') may be followed only by whitespace until the end of the line.");
          }
          return;
        }
        else
        {
          await advance(parserState, useAsync).ConfigureAwait(false);
        }

      }
    }

    private static bool isMlbUnescapedChar(char ch) => isBasicUnescapedChar(ch);

    private static async ValueTask<(TomlKey key, TomlTable CurrentTomlTable)> parseKey<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      Debug.Assert(parserState.KeyParent != TomlTokenType.Undefined);
      TomlKey? tomlKey = null;
      TomlKey? lastKeyPart = null;
      TomlTable? lastParsedTable = null;
      await skipWhiteSpaces(parserState, useAsync).ConfigureAwait(false);
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
              throwTomlSerializerException(parserState, $"Key '{newKeyPart}' is already defined as '{tomlTableOrArrayToken.TokenType}' and cannot be used as a table.");
            }
          }
          else
          {
            var newTomlTable = new TomlTable(newKeyPart.Clone());
            parserState.CurrentTomlTable.AddInternal(newKeyPart, newTomlTable);
            newTomlTable.HasFromKeyDefinition = parserState.KeyParent == TomlTokenType.KeyValue;
            if (newTomlTable.HasFromKeyDefinition && (lastParsedTable?.HasTopLevelDefinition ?? false))
            {
              throwTomlSerializerException(parserState, $"Cannot define table '{newKeyPart}' with a dotted key inside table '{lastParsedTable!.FullName}': that table was explicitly defined with a [header] and cannot be extended with dotted keys.");
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
          await advance(parserState, useAsync).ConfigureAwait(false);
          parserState.ParsingDottedKey = tomlKey != null && tomlKey.IsDottedKey;
        }

        await skipWhiteSpaces(parserState, useAsync).ConfigureAwait(false);
        if (!parserState.ParsingDottedKey)
        {
          await skipNewLine(parserState, useAsync).ConfigureAwait(false);
        }
        else
        {
          await skipWhiteSpaces(parserState, useAsync).ConfigureAwait(false);
        }

        throwIfEof(parserState);

        switch (parserState.CurrentChar)
        {
          case '\"':
            newKeyPart = new TomlKey(await parseQuotedKey(parserState, useAsync).ConfigureAwait(false), TomlKeyType.SimpleQuotedBasicString);
            break;
          case '\'':
            newKeyPart = new TomlKey(await parseQuotedKey(parserState, useAsync).ConfigureAwait(false), TomlKeyType.SimpleQuotedLiteralString);
            break;
          default:
          {
            newKeyPart = new TomlKey(await parseUnquotedKey(parserState, useAsync).ConfigureAwait(false), TomlKeyType.SimpleUnquoted);
            break;
          }
        }

        if (tomlKey == null)
        {
          tomlKey = newKeyPart;
        }
        else
        {
          lastKeyPart!.NextKeyPart = newKeyPart;
        }
        lastKeyPart = newKeyPart;
        await skipWhiteSpaces(parserState, useAsync).ConfigureAwait(false);
      }

      Debug.Assert(tomlKey != null);
      return (tomlKey, parserState.CurrentTomlTable);
    }

    private static async ValueTask<string> parseUnquotedKey<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      await skipWhiteSpaces(parserState, useAsync).ConfigureAwait(false);
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
            await advance(parserState, useAsync).ConfigureAwait(false);
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
            await skipWhiteSpaces(parserState, useAsync).ConfigureAwait(false);
            if (parserState.Eof)
            {
              throwEof(parserState, "'=' after the key");
            }

            if (keyBuilder.Length > 0)
            {
              expectChar(parserState, '=', '.', ']', "Invalid key definition.");
              break;
            }
          }
          else
          {
            throwTomlSerializerException(parserState, $"Character {describeChar(parserState.CurrentChar)} is not allowed in an unquoted key. Unquoted keys may contain only A-Z, a-z, 0-9, '-' and '_'; quote the key otherwise.");
          }
        }

        if (keyBuilder.Length == 0)
        {
          throwTomlSerializerException(parserState, "A key must not be empty (use a quoted key such as \"\" for an empty name).");
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
        throwTomlSerializerException(parserState, $"{error} Expected '{expectedChar1}', '{expectedChar2}' or '{expectedChar3}', but found {describeCurrentChar(parserState)}.");
      }
    }

    private static void expectChar<TInputReader>(ParserState<TInputReader> parserState,
                                                 char expectedChar,
                                                 string error)
      where TInputReader : struct, IInputReader
    {
      if (parserState.CurrentChar != expectedChar)
      {
        throwTomlSerializerException(parserState, $"{error} Expected '{expectedChar}', but found {describeCurrentChar(parserState)}.");
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
        throwTomlSerializerException(parserState, $"{error} Expected '{expectedChar1}' or '{expectedChar2}', but found {describeCurrentChar(parserState)}.");
      }
    }

    private static bool isUnquotedKeyChar(char ch) => ch.IsAsciiLetterOrDigit() || ch == '-' || ch == '_';
    private static ValueTask<string> parseQuotedKey<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      switch (parserState.CurrentChar)
      {
        case '\"':
          return parseBasicString(parserState, useAsync);
        case '\'':
          return parseLiteralString(parserState, useAsync);
        default:
          throwTomlSerializerException(parserState, $"Unexpected {describeChar(parserState.CurrentChar)} where a quoted key was expected.");
          return default;
      }
    }

    private static async ValueTask<string> parseLiteralString<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      Debug.Assert(parserState.CurrentChar == '\'');
      try
      {
        var literalStringBuilder = parserState.StringBuilder;

        do
        {
          await advance(parserState, useAsync).ConfigureAwait(false);
          if (parserState.Eof)
          {
            throwEof(parserState, "closing ' of the literal string", reportOpenConstruct: false);
          }

          if (parserState.CurrentChar == '\'')
          {
            await advance(parserState, useAsync).ConfigureAwait(false);
            return literalStringBuilder.ToString();
          }

          if (isLiteralChar(parserState.CurrentChar))
          {
            literalStringBuilder.Append(parserState.CurrentChar);
          }

          else
          {
            throwTomlSerializerException(parserState, $"Character {describeChar(parserState.CurrentChar)} is not allowed in a literal string{(isNewLineChar(parserState.CurrentChar) ? " (use ''' for a multiline string)" : "")}.");
          }

        } while (true);
      }
      finally
      {
        parserState.StringBuilder.Clear();
      }
    }

    private static bool isLiteralChar(char ch) => ch == 0x09 || (ch >= 0x20 && ch <= 0x26) || (ch >= 0x28 && ch <= 0x7E) || ch.IsNonAsciiChar();

    private static async ValueTask<string> parseBasicString<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      Debug.Assert(parserState.CurrentChar == '"');

      try
      {
        var basicStringBuilder = parserState.StringBuilder;

        do
        {
          await advance(parserState, useAsync).ConfigureAwait(false);
          if (parserState.Eof)
          {
            throwEof(parserState, "closing \" of the basic string", reportOpenConstruct: false);
          }

          if (parserState.CurrentChar == '"')
          {
            await advance(parserState, useAsync).ConfigureAwait(false);
            return basicStringBuilder.ToString();
          }

          if (isBasicUnescapedChar(parserState.CurrentChar))
          {
            basicStringBuilder.Append(parserState.CurrentChar);
          }
          else if (isEscapedChar(parserState.CurrentChar))
          {
            var escapedChar = await parseBasicEscapedChar(parserState, useAsync).ConfigureAwait(false);
            basicStringBuilder.Append(escapedChar);
          }
          else
          {
            throwTomlSerializerException(parserState, $"Character {describeChar(parserState.CurrentChar)} is not allowed in a basic string{(isNewLineChar(parserState.CurrentChar) ? " (use \"\"\" for a multiline string)" : "")}.");
          }

        } while (true);
      }
      finally
      {
        parserState.StringBuilder.Clear();
      }
    }

    private static async ValueTask<char> parseBasicEscapedChar<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      Debug.Assert(isEscapedChar(parserState.CurrentChar));
      await advance(parserState, useAsync).ConfigureAwait(false);
      if (parserState.Eof)
      {
        throwEof(parserState, "an escape sequence");
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
            var escapeChar = parserState.CurrentChar;
            var readResult = useAsync
              ? await parserState.ReadAsync(new ArraySegment<char>(numberChars)).ConfigureAwait(false)
              : parserState.Read(new ArraySegment<char>(numberChars));
            //The chars were read directly from the input, so the position has to be moved manually.
            parserState.CurrentColumn += readResult;
            if (readResult < requiredChars)
            {
              throwEof(parserState, $"{requiredChars} hexadecimal digits after '\\{escapeChar}'");
            }

            var escapeDigits = new string(numberChars);
            if (!numberChars.All(isAsciiHexDigit))
            {
              throwTomlSerializerException(parserState, $"Invalid escape sequence '\\{escapeChar}{escapeDigits}': expected {requiredChars} hexadecimal digits after '\\{escapeChar}'.");
            }

            var int32Char = Convert.ToInt32(escapeDigits, BASE_NUMBER);
            if (int32Char >= 0x0 && int32Char <= 0x10FFFF && (int32Char < 0xD800 || int32Char > 0xDFFF))
            {
              return (char)int32Char;
            }

            throwTomlSerializerException(parserState, $"Invalid escape sequence '\\{escapeChar}{escapeDigits}': U+{escapeDigits.ToUpperInvariant()} is not a valid Unicode scalar value (surrogates and values above U+10FFFF are not allowed).");
            break;
          }
        default:
          {
            var versionHint = (parserState.CurrentChar == 'e' || parserState.CurrentChar == 'x') && parserState.TomlSettings.TomlVersion == TomlVersion.Toml10
              ? " (it is supported only in TOML 1.1)"
              : "";
            throwTomlSerializerException(parserState, $"Unsupported escape sequence '\\{parserState.CurrentChar}'{versionHint}. Supported escapes are \\b \\t \\n \\f \\r \\\" \\\\ \\uXXXX and \\UXXXXXXXX.");
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

    private static async ValueTask<TomlTable> parseStandardTable<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      try
      {
        parserState.KeyParent = TomlTokenType.Table;
        if (parserState.CurrentChar != '[')
        {
          throwTomlSerializerException(parserState, $"Invalid table header: expected '[', but found {describeChar(parserState.CurrentChar)}.");
        }

        await advance(parserState, useAsync).ConfigureAwait(false);
        throwIfEof(parserState, "a table name");
        var (key, parentTable) = await parseKey(parserState, useAsync).ConfigureAwait(false);
        var lastKeyPart = key.LastKeyPart;
        await skipWhiteSpaces(parserState, useAsync).ConfigureAwait(false);
        if (parserState.CurrentChar != ']')
        {
          throwTomlSerializerException(parserState, $"Invalid table header: expected ']' to close the header, but found {describeChar(parserState.CurrentChar)}.");
        }

        if (parentTable.TryGetValue(lastKeyPart, out var token))
        {
          if (token.TokenType != TomlTokenType.Table)
          {
            throwTomlSerializerException(parserState, $"Cannot define table '{key}': the key already exists as '{token.TokenType}'.");
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
          throwTomlSerializerException(parserState, $"Redefinition of table '{key}' is not allowed.");
        }

        await advance(parserState, useAsync).ConfigureAwait(false);
        if (!parserState.Eof && isWhiteSpace(parserState.CurrentChar))
        {
          await skipWhiteSpaces(parserState, useAsync).ConfigureAwait(false);
        }

        if (!parserState.Eof)
        {
          if (parserState.CurrentChar != '#' && !isNewLineChar(parserState.CurrentChar))
          {
            throwTomlSerializerException(parserState, $"Expected end of line or comment after the table header, but found {describeChar(parserState.CurrentChar)}.");
          }

          if (parserState.CurrentChar == '#')
          {
            _ = await parseComment(parserState, useAsync).ConfigureAwait(false);
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

    private static async ValueTask<TomlComment> parseComment<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      try
      {
        var startCurrentLine = parserState.CurrentLine;
        var commentBuilder = parserState.StringBuilder;
        Debug.Assert(commentBuilder.Length == 0);

        while (parserState.CurrentLine == startCurrentLine)
        {
          await advance(parserState, useAsync).ConfigureAwait(false);
          if (parserState.Eof || isNewLineChar(parserState))
          {
            break;
          }

          if (!isAllowedCommentChar(parserState.CurrentChar))
          {
            throwTomlSerializerException(parserState, $"Control character {describeChar(parserState.CurrentChar)} is not allowed in a comment.");
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
      var exceptionMessage = $"TOML document is invalid. {innerMessage} (line {parserState.CurrentLine}, column {parserState.CurrentColumn})";
      throw new TomlSerializerException(exceptionMessage, parserState.CurrentLine, parserState.CurrentColumn);
    }

    private static string describeChar(char ch)
    {
      switch (ch)
      {
        case '\n':
          return "newline";
        case '\r':
          return "carriage return";
        case '\t':
          return "tab";
      }

      return char.IsControl(ch) || char.IsSurrogate(ch) || ch == '�'
        ? $"U+{(int) ch:X4}"
        : $"'{ch}'";
    }

    //Describes the current char; when it is a digit, the following digit (if any) is included to show the whole number.
    private static string describeDigitPair<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      var next = peek(parserState);
      return char.IsDigit(parserState.CurrentChar) && next != null && char.IsDigit(next.Value)
        ? $"'{parserState.CurrentChar}{next.Value}'"
        : describeChar(parserState.CurrentChar);
    }

    private static string describeChar(char? ch) => ch == null ? "end of document" : describeChar(ch.Value);

    private static string describeCurrentChar<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader => parserState.Eof ? "end of document" : describeChar(parserState.CurrentChar);

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
    

    private static void throwIfEof<TInputReader>(ParserState<TInputReader> parserState, string? expected = null)
      where TInputReader : struct, IInputReader
    {
      if (parserState.Eof)
      {
        throwEof(parserState, expected);
      }
    }

#if NET9_0_OR_GREATER
    [DoesNotReturn]
#endif
    private static void throwEof<TInputReader>(ParserState<TInputReader> parserState, string? expected = null, bool reportOpenConstruct = true)
      where TInputReader : struct, IInputReader
    {
      var message = new StringBuilder(UNEXPECTED_END_OF_TOML_DOCUMENT_ERROR);
      if (expected != null)
      {
        message.Append(", expected ").Append(expected);
      }

      if (reportOpenConstruct && parserState.OpenConstructs.Count > 0)
      {
        var (name, line, column) = parserState.OpenConstructs.Peek();
        message.Append(": unterminated ").Append(name).Append(" that started at line ").Append(line).Append(", column ").Append(column);
      }

      message.Append('.');
      throwTomlSerializerException(parserState, message.ToString());
    }

    private static void enterConstruct<TInputReader>(ParserState<TInputReader> parserState, string name)
      where TInputReader : struct, IInputReader
    {
      enterConstruct(parserState, name, parserState.CurrentLine, parserState.CurrentColumn);
    }

    private static void enterConstruct<TInputReader>(ParserState<TInputReader> parserState,
                                                     string name,
                                                     int line,
                                                     int column)
      where TInputReader : struct, IInputReader
    {
      parserState.OpenConstructs.Push((name, line, column));
    }

    private static void exitConstruct<TInputReader>(ParserState<TInputReader> parserState)
      where TInputReader : struct, IInputReader
    {
      parserState.OpenConstructs.Pop();
    }

    private static async ValueTask skipWhitespacesAndNewLines<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      while (!parserState.Eof && (isWhiteSpace(parserState.CurrentChar) || isNewLineChar(parserState)))
      {
        await skipWhiteSpaces(parserState, useAsync).ConfigureAwait(false);
        await skipNewLine(parserState, useAsync).ConfigureAwait(false);
      }
    }

    private static async ValueTask skipNewLine<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      if (parserState.CurrentChar == '\r' )
      {
        if (peek(parserState) == '\n')
        {
          await advance(parserState, useAsync).ConfigureAwait(false);
        }
        else
        {
          throwTomlSerializerException(parserState, @"Invalid bare carriage return ('\r'). Expected '\r\n' or '\n'.");
        }
      }

      if (parserState.CurrentChar == '\n')
      {
        await advance(parserState, useAsync).ConfigureAwait(false);
      }
    }

    private static async ValueTask skipWhiteSpaces<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      while (!parserState.Eof && isWhiteSpace(parserState.CurrentChar))
      {
        await advance(parserState, useAsync).ConfigureAwait(false);
      }
    }

    private static async ValueTask<bool> advance<TInputReader>(ParserState<TInputReader> parserState, bool useAsync)
      where TInputReader : struct, IInputReader
    {
      char? readC;
      if ((readC = useAsync
            ? await parserState.ReadCharAsync().ConfigureAwait(false)
            : parserState.ReadChar()) != null)
      {
        parserState.CurrentChar = readC.Value;
        if (parserState.PendingNewLine)
        {
          //The newline char itself still belongs to the line it terminates.
          parserState.PendingNewLine = false;
          ++parserState.CurrentLine;
          parserState.CurrentColumn = 0;
        }

        ++parserState.CurrentColumn;
        if (parserState.CurrentChar == '\uFFFD')
        {
          throwTomlSerializerException(parserState, "The input contains the Unicode replacement character U+FFFD, which usually means invalid UTF-8 encoding.");
        }

        parserState.PendingNewLine = parserState.CurrentChar == '\n';
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
    } = 1;

    public bool PendingNewLine
    {
      get;
      set;
    }

    public Stack<(string Name, int Line, int Column)> OpenConstructs
    {
      get;
    } = new Stack<(string Name, int Line, int Column)>();

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

    public int CurrentNestingDepth
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
      var count = Math.Min(buffer.Count, _inputString.Length - _position);
      for (var i = 0; i < count; ++i)
      {
        Debug.Assert(buffer.Array != null);
        buffer.Array[buffer.Offset + i] = _inputString[_position++];
      }

      return count;
    }
  }
#pragma warning restore IDE0044
  // ReSharper restore FieldCanBeMadeReadOnly.Local
}