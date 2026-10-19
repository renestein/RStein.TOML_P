using System;
using System.Text;
using System.Text.RegularExpressions;

namespace RStein.TOML
{
  internal static class StringExtensions
  {
    public const string ESCAPE_TOML_BASIC_STRING_MATCH_REGEX = @"(""|\\|[\u0000-\u0019]|[\u000A-\u001f]|\u007f|u\+\d{8}|u\+\d{4})+";
    public static readonly Regex ESCAPE_STRING_MATCH_REGEX = new Regex(ESCAPE_TOML_BASIC_STRING_MATCH_REGEX, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant );

    public const string ESCAPE_TOML_ML_BASIC_STRING_MATCH_REGEX = @"(""{3,}|\\|[\u0000-\u0019]|[\u000A-\u001f]|\u007f|u\+\d{8}|u\+\d{4})+";
    public static readonly Regex ESCAPE_ML_STRING_MATCH_REGEX = new Regex(ESCAPE_TOML_ML_BASIC_STRING_MATCH_REGEX, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string ToEscapedTomlMlString(this String originalString)
    {
      if (String.IsNullOrEmpty(originalString))
      {
        return originalString;
      }

      return ESCAPE_ML_STRING_MATCH_REGEX.Replace(originalString, escapeMlChars);
    }

    public static string ToEscapedTomlString(this String originalString)
    {
      if (String.IsNullOrEmpty(originalString))
      {
        return originalString;
      }

      var escapedStringRegex = ESCAPE_STRING_MATCH_REGEX;
      var escapedString = escapedStringRegex.Replace(originalString, escapeChars);
      return escapedString;
    }

    public static bool HasEscapedChar(this String originalString)
    {
      return ESCAPE_STRING_MATCH_REGEX.IsMatch(originalString);
    }

    public static TomlDataType SelectBestTomlStringType(this string value)
    {
      if (String.IsNullOrEmpty(value))
      {
        return TomlDataType.BasicString;
      }

      return value.Contains("\n")
        ? TomlDataType.BasicMlString
        : TomlDataType.BasicString;
    }
    private static string escapeChars(Match match)
    {
      var retString = String.Empty;
      foreach (var matchChar in match.Value)
      {
        retString += getEscapedChar(matchChar);
      }

      return retString;
    }

    private static string escapeMlChars(Match match)
    {
      var builder = new StringBuilder(match.Value.Length);
      var quotesInRow = 0;
      foreach (var matchChar in match.Value)
      {
        if (matchChar != '"')
        {
          quotesInRow = 0;
          builder.Append(getEscapedChar(matchChar));
          continue;
        }

        // At most two consecutive quotes may stay unescaped, the third one must be escaped.
        if (++quotesInRow == 3)
        {
          builder.Append(getEscapedChar(matchChar));
          quotesInRow = 0;
        }
        else
        {
          builder.Append(matchChar);
        }
      }

      return builder.ToString();
    }

    private static string getEscapedChar(char matchChar)
    {
      switch (matchChar)
      {
        case '\t':
          {
            return @"\t";
          }
        case '\r':
          {
            return @"\r";
          }
        case '\n':
          {
            return @"\n";
          }
        case '\f':
          {
            return @"\f";
          }
        case '\b':
        {
          return @"\b";
        }
        case '\\':
        {
          return @"\\";
        }
        case '"':
        {
          return @"\""";
        }
        default:
          {
            var intChar = (int)matchChar;
            return @"\u" + intChar.ToString("X4");
          }
      }
    }
  }
}