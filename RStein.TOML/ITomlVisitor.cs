using System.Threading.Tasks;

namespace RStein.TOML
{
  internal interface ITomlVisitor<in TContext>
  {
    ValueTask Visit(TomlComment tomlComment, TContext context);
    ValueTask Visit(TomlKeyValue tomlKeyValue, TContext context);
    ValueTask Visit(TomlPrimitiveValue tomlPrimitiveValue, TContext context);
    ValueTask Visit(TomlTable tomlTable, TContext context);
    ValueTask Visit(TomlInlineTable tomlInlineTable, TContext context);
    ValueTask Visit(TomlArray tomlArray, TContext context);
    ValueTask Visit(TomlArrayOfTables tomlArrayOfTables, TContext context);
    ValueTask Visit(TomlKey tomlKey, TContext context);
  }
}