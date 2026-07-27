namespace RStein.TOML
{
  internal class DeepEqualsVisitorContext : IRecursionDepthContext
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

    public int RecursionDepth
    {
      get;
      set;
    }

    public int MaxRecursionDepth => TomlSettings.Default.MaxNestingDepth;
  }
}