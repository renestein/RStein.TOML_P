using System.Diagnostics;
using System.Threading;

namespace RStein.TOML
{
  internal class TomlSerializerVisitorContext
  {
    private CancellationToken _cancellationToken;

    public TomlSerializerVisitorContext(TomlWriter tomlWriter)
    {
      Debug.Assert(tomlWriter != null);
      TomlWriter = tomlWriter;
    }

    public TomlWriter TomlWriter
    {
      get;
    }

    public TomlKey? CurrentKeyForFromKeyTables
    {
      get;
      set;
    }

    public TomlKey? CurrentKeyForTopTables
    {
      get;
      set;
    }

    public TomlSettings TomlSettings
    {
      get;
      set;
    } = TomlSettings.Default;

    public CancellationToken CancellationToken
    {
      get => _cancellationToken;
      set => _cancellationToken = value;
    }
  }
}