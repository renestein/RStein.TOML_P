using System.Threading.Tasks;

namespace RStein.TOML
{
  internal interface ITomlVisitor<in TContext>
  {
    ValueTask VisitAsync(TomlComment tomlComment, TContext context);
    ValueTask VisitAsync(TomlKeyValue tomlKeyValue, TContext context);
    ValueTask VisitAsync(TomlPrimitiveValue tomlPrimitiveValue, TContext context);
    ValueTask VisitAsync(TomlTable tomlTable, TContext context);
    ValueTask VisitAsync(TomlInlineTable tomlInlineTable, TContext context);
    ValueTask VisitAsync(TomlArray tomlArray, TContext context);
    ValueTask VisitAsync(TomlArrayOfTables tomlArrayOfTables, TContext context);
    ValueTask VisitAsync(TomlKey tomlKey, TContext context);
  }
}