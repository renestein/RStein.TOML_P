using System.Runtime.CompilerServices;

namespace RStein.TOML
{
  internal static class CharExtensions
  {

    public static bool NeedsEscape(this char c) => StringExtensions.ESCAPE_STRING_MATCH_REGEX.IsMatch(c.ToString());

    public static bool IsTomlControlChar(this char c)
    {
      if (c == '\u007f')
      {
        return true;
      }

      if (c <= '\u0008')
      {
        return true;
      }

      if (c >= '\u000A' && c <= '\u001F')
      {
        return true;
      }

      return false;
    }

    public static bool IsNonAsciiChar(this char c)
    {
      if (c >= 0x80 && c <= 0xd7ff)
      {
        return true;
      }


      if (c >= 0xE000)
      {
        return true;
      }

      return false;
    }

    public static bool IsAsciiChar(this char c) => !IsNonAsciiChar(c);

    public static bool IsAsciiLetterOrDigit(this char c) => (c >= 'a' && c <= 'z') ||
                                                            (c >= 'A' && c <= 'Z') ||
                                                            char.IsDigit(c);
  }
}