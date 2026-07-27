namespace RStein.TOML
{
  internal interface IRecursionDepthContext
  {
    int RecursionDepth
    {
      get;
      set;
    }

    int MaxRecursionDepth
    {
      get;
    }
  }
}
