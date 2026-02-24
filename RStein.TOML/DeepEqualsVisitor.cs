using System.Threading.Tasks;

namespace RStein.TOML
{
  internal class DeepEqualsVisitor : ITomlVisitor<DeepEqualsVisitorContext>
  {
    public ValueTask Visit(TomlComment tomlComment,
                           DeepEqualsVisitorContext context)
    {
      if (!context.DeepEqualsResult)
      {
        return new ValueTask();
      }

      if (!tomlComment.Equals(context.OtherToken))
      {
        context.DeepEqualsResult = false;
      }

      return new ValueTask();
    }

    public ValueTask Visit(TomlKeyValue tomlKeyValue,
                           DeepEqualsVisitorContext context)
    {
      if (!context.DeepEqualsResult)
      {
        return new ValueTask();
      }

      if (!tomlKeyValue.Equals(context.OtherToken))
      {
        context.DeepEqualsResult = false;
      }

      return new ValueTask();
    }

    public ValueTask Visit(TomlPrimitiveValue tomlPrimitiveValue,
                           DeepEqualsVisitorContext context)
    {

      if (!context.DeepEqualsResult)
      {
        return new ValueTask();
      }

      if (!tomlPrimitiveValue.Equals(context.OtherToken))
      {
        context.DeepEqualsResult = false;
      }

      return new ValueTask();
    }

    public async ValueTask Visit(TomlTable tomlTable,
                                 DeepEqualsVisitorContext context)
    {
      if (!context.DeepEqualsResult)
      {
        return;
      }

      if (!(context.OtherToken is TomlTable secondTable))
      {
        context.DeepEqualsResult = false;
        return;
      }

      if (tomlTable.Count != secondTable.Count)
      {
        context.DeepEqualsResult = false;
        return;
      }

      foreach (var tomlTableValue in tomlTable)
      {
        if (!secondTable.TryGetValue(tomlTableValue.Key, out var secondTableValue))
        {
          context.DeepEqualsResult = false;
          return;
        }

        context.OtherToken = secondTableValue;
        await tomlTableValue.Value.AcceptVisitorAsync(this, context).ConfigureAwait(false);
        if (!context.DeepEqualsResult)
        {
          return;
        }
      }

      context.OtherToken = secondTable;
    }

    public async ValueTask Visit(TomlInlineTable tomlInlineTable,
                                 DeepEqualsVisitorContext context)
    {

      if (!context.DeepEqualsResult)
      {
        return;
      }

      if (!(context.OtherToken is TomlInlineTable secondTable))
      {
        context.DeepEqualsResult = false;
        return;
      }


      if (tomlInlineTable.Count != secondTable.Count)
      {
        context.DeepEqualsResult = false;
        return;
      }


      foreach (var tomlTableValue in tomlInlineTable)
      {
        if (!secondTable.TryGetValue(tomlTableValue.Key, out var secondTableValue))
        {
          context.DeepEqualsResult = false;
          return;
        }

        context.OtherToken = secondTableValue;
        await tomlTableValue.Value.AcceptVisitorAsync(this, context).ConfigureAwait(false);
        if (!context.DeepEqualsResult)
        {
          return;
        }
      }

      context.OtherToken = secondTable;
    }

    public async ValueTask Visit(TomlArray tomlArray,
                                 DeepEqualsVisitorContext context)
    {

      if (!context.DeepEqualsResult)
      {
        return;
      }

      if (!(context.OtherToken is TomlArray secondArray))
      {
        context.DeepEqualsResult = false;
        return;
      }

      if (tomlArray.Count != secondArray.Count)
      {
        context.DeepEqualsResult = false;
        return;
      }

      for (var index = 0; index < tomlArray.Count; index++)
      {
        var tomlArrayValue = tomlArray[index];
        var secondArrayValue = secondArray[index];
        context.OtherToken = secondArrayValue;
        await tomlArrayValue.AcceptVisitorAsync(this, context).ConfigureAwait(false);
        if (!context.DeepEqualsResult)
        {
          return;
        }
      }

      context.OtherToken = secondArray;
    }

    public async ValueTask Visit(TomlArrayOfTables tomlArrayOfTables,
                                 DeepEqualsVisitorContext context)
    {
      if (!context.DeepEqualsResult)
      {
        return;
      }

      if (!(context.OtherToken is TomlArrayOfTables secondArray))
      {
        context.DeepEqualsResult = false;
        return;
      }

      if (tomlArrayOfTables.Count != secondArray.Count)
      {
        context.DeepEqualsResult = false;
        return;
      }

      for (var index = 0; index < tomlArrayOfTables.Count; index++)
      {
        var tomlArrayValue = tomlArrayOfTables[index];
        var secondArrayValue = secondArray[index];
        context.OtherToken = secondArrayValue;
        await tomlArrayValue.AcceptVisitorAsync(this, context).ConfigureAwait(false);
        if (!context.DeepEqualsResult)
        {
          return;
        }
      }

      context.OtherToken = secondArray;
    }

    public ValueTask Visit(TomlKey tomlKey,
                           DeepEqualsVisitorContext context)
    {
      if (!context.DeepEqualsResult)
      {
        return new ValueTask();
      }

      if (!tomlKey.Equals(context.OtherToken))
      {
        context.DeepEqualsResult = false;
      }

      return new ValueTask();
    }
  }
}