namespace RStein.TOML
{
  internal class DeepEqualsVisitorContext
  {
    public TomlToken? OtherToken
    {
      get;
      set;
    }

    public bool DeepEqualsResult
    {
      get;
      set;
    } = true;
  }
}