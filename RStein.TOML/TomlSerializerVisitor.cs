using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace RStein.TOML
{
  internal class TomlSerializerVisitor : ITomlVisitor<TomlSerializerVisitorContext>
  {
    public async ValueTask Visit(TomlComment tomlComment,
                                 TomlSerializerVisitorContext context)
    {
      Debug.Assert(context != null);
      var writer = context.TomlWriter;
      await writer.BeginWriteCommentAsync(tomlComment).ConfigureAwait(false);
      await writer.EndWriteCommentAsync(tomlComment).ConfigureAwait(false);
    }

    public async ValueTask Visit(TomlKeyValue tomlKeyValue,
                                 TomlSerializerVisitorContext context)
    {
      Debug.Assert(context != null);
      await tomlKeyValue.Key.AcceptVisitorAsync(this, context).ConfigureAwait(false);
      await tomlKeyValue.Value.AcceptVisitorAsync(this, context).ConfigureAwait(false);
    }

    public async ValueTask Visit(TomlPrimitiveValue tomlPrimitiveValue,
                                 TomlSerializerVisitorContext context)
    {
      Debug.Assert(context != null);
      var writer = context.TomlWriter;
      await writer.BeginWritePrimitiveValueAsync(tomlPrimitiveValue).ConfigureAwait(false);
      await writer.EndWritePrimitiveValueAsync(tomlPrimitiveValue).ConfigureAwait(false);
    }

    public async ValueTask Visit(TomlTable tomlTable,
                                 TomlSerializerVisitorContext context)
    {
      Debug.Assert(context != null);
      context.CancellationToken.ThrowIfCancellationRequested();
      var writer = context.TomlWriter;
      var isRootTableName = tomlTable.Name.Equals(TomlTable.ROOT_TABLE_NAME);
      TomlKey? oldCurrentKeyForFromKeyTables = null;
      TomlKey? oldCurrentKeyForTopTables = null;

      if (isRootTableName)
      {
        await writer.BeginTomlDocumentAsync().ConfigureAwait(false);
      }
      else if (tomlTable.HasFromKeyDefinition)
      {
        oldCurrentKeyForFromKeyTables = context.CurrentKeyForFromKeyTables;
        if (context.CurrentKeyForFromKeyTables == null)
        {
          context.CurrentKeyForFromKeyTables = new TomlKey(tomlTable.FullName.RawKey, tomlTable.FullName.Type);
        }
        else
        {
          context.CurrentKeyForFromKeyTables = context.CurrentKeyForFromKeyTables.Clone();
          context.CurrentKeyForFromKeyTables.LastKeyPart.NextKeyPart = (new TomlKey(tomlTable.FullName.RawKey, tomlTable.FullName.Type));
        }
      }

      if (!isRootTableName && (tomlTable.HasTopLevelDeclaration || tomlTable.IsArrayOfTablesMember))
      {
        oldCurrentKeyForTopTables = context.CurrentKeyForTopTables;
        if (context.CurrentKeyForTopTables == null)
        {
          context.CurrentKeyForTopTables = new TomlKey(tomlTable.FullName.RawKey, tomlTable.FullName.Type);
        }
        else
        {
          if (!tomlTable.IsArrayOfTablesMember)
          {
            context.CurrentKeyForTopTables = context.CurrentKeyForTopTables.Clone();
            context.CurrentKeyForTopTables.LastKeyPart.NextKeyPart = new TomlKey(tomlTable.FullName.RawKey, tomlTable.FullName.Type);
          }
        }
      }
     
      var innerTables = tomlTable.Values.OfType<TomlTable>().Where(table => table.HasTopLevelDeclaration).ToArray();

      var emittedBeginStandardTable = false;
      foreach (var keyValue in tomlTable)
      {
        var nestedTable = keyValue.Value as TomlTable;

        if (nestedTable?.HasTopLevelDeclaration ?? false)
        {
          continue;
        }

        if (!writer.HasState(TomlWriterState.WritingInlineTable) && !(keyValue.Value is TomlArrayOfTables))
        {
          if (nestedTable == null || !nestedTable.HasFromKeyDefinition || (nestedTable.HasFromKeyDefinition && nestedTable.HasFromInlineTableDefinition))
          {
            if (context.CurrentKeyForFromKeyTables != null)
            {
              var key = context.CurrentKeyForFromKeyTables.Clone();
              key.LastKeyPart.NextKeyPart = keyValue.Key;
              await writer.BeginWriteKeyAsync(key).ConfigureAwait(false);
              await writer.EndWriteKeyAsync(key).ConfigureAwait(false);
            }
            else
            {
              if ((tomlTable.HasTopLevelDefinition) && !isRootTableName && !emittedBeginStandardTable)
              {
                Debug.Assert(context.CurrentKeyForTopTables != null);
                await writer.BeginWriteTableAsync(tomlTable, context.CurrentKeyForTopTables).ConfigureAwait(false);
                emittedBeginStandardTable = true;
              }

              await writer.BeginWriteKeyAsync(keyValue.Key).ConfigureAwait(false);
              await writer.EndWriteKeyAsync(keyValue.Key).ConfigureAwait(false);
            }
          }
          else
          {
            if (tomlTable.HasTopLevelDefinition && !isRootTableName && !emittedBeginStandardTable)
            {
              Debug.Assert(context.CurrentKeyForTopTables != null);
              await writer.BeginWriteTableAsync(tomlTable, context.CurrentKeyForTopTables).ConfigureAwait(false);
              emittedBeginStandardTable = true;
            }
          }
        }
        await keyValue.Value.AcceptVisitorAsync(this, context).ConfigureAwait(false);
      }

      if (isRootTableName)
      {
        foreach (var innerTable in innerTables)
        {
          await innerTable.AcceptVisitorAsync(this, context).ConfigureAwait(false);
        }

        await writer.EndTomlDocumentAsync().ConfigureAwait(false);
      }
      else if (tomlTable.HasFromKeyDefinition)
      {
        if (tomlTable.Count == 0)
        {
          await writer.BeginWriteKeyAsync(tomlTable.FullName).ConfigureAwait(false);
          await writer.EndWriteKeyAsync(tomlTable.FullName).ConfigureAwait(false);
          await writer.BeginWriteInlineTableAsync(tomlTable).ConfigureAwait(false);
          await writer.EndWriteInlineTableAsync(tomlTable).ConfigureAwait(false);
        }
        context.CurrentKeyForFromKeyTables = oldCurrentKeyForFromKeyTables;
        foreach (var innerTable in innerTables)
        {
          await innerTable.AcceptVisitorAsync(this, context).ConfigureAwait(false);
        }
      }
      else
      {
        if (tomlTable.HasTopLevelDefinition && !emittedBeginStandardTable)
        {
          Debug.Assert(context.CurrentKeyForTopTables != null);
          await writer.BeginWriteTableAsync(tomlTable, context.CurrentKeyForTopTables).ConfigureAwait(false);
          emittedBeginStandardTable = true;
        }

        if (emittedBeginStandardTable)
        {
          Debug.Assert(tomlTable.HasTopLevelDefinition || tomlTable.IsArrayOfTablesMember);
          await writer.EndWriteTableAsync(tomlTable).ConfigureAwait(false);
        }

        foreach (var innerTable in innerTables)
        {
          await innerTable.AcceptVisitorAsync(this, context).ConfigureAwait(false);
        }

        if (tomlTable.HasTopLevelDeclaration)
        {
          context.CurrentKeyForTopTables = oldCurrentKeyForTopTables;
        }
      }
    }

    public async ValueTask Visit(TomlInlineTable tomlInlineTable,
                                 TomlSerializerVisitorContext context)
    {
      Debug.Assert(context != null);

      var writer = context.TomlWriter;
      await writer.BeginWriteInlineTableAsync(tomlInlineTable).ConfigureAwait(false);
      var oldCurrentKeyForFromKeyTables = context.CurrentKeyForFromKeyTables;
      context.CurrentKeyForFromKeyTables = null;
      var isFirstItem = true;

      foreach (var keyValuePair in tomlInlineTable)
      {
        if (keyValuePair.Value is TomlTable table && table.HasFromKeyDefinition)
        {
          await writeFromKeyTable(table).ConfigureAwait(false);
          isFirstItem = false;
        }
        else
        {
          await writer.BeginWriteInlineTableItemAsync(keyValuePair, isFirstItem).ConfigureAwait(false);
          await writer.BeginWriteKeyAsync(keyValuePair.Key).ConfigureAwait(false);
          await writer.EndWriteKeyAsync(keyValuePair.Key).ConfigureAwait(false);
          await keyValuePair.Value.AcceptVisitorAsync(this, context).ConfigureAwait(false);
          await writer.EndWriteInlineTableItemAsync(keyValuePair, isFirstItem).ConfigureAwait(false);
          isFirstItem = false;
        }
      }

      await writer.EndWriteInlineTableAsync(tomlInlineTable).ConfigureAwait(false);
      context.CurrentKeyForFromKeyTables = oldCurrentKeyForFromKeyTables;

      async Task writeFromKeyTable(TomlTable tomlTable)
      {
        var oldContextKey = context.CurrentKeyForFromKeyTables;
        if (context.CurrentKeyForFromKeyTables == null)
        {
          context.CurrentKeyForFromKeyTables = tomlTable.FullName;
        }
        else
        {
          context.CurrentKeyForFromKeyTables = context.CurrentKeyForFromKeyTables.Clone();
          context.CurrentKeyForFromKeyTables.LastKeyPart.NextKeyPart = tomlTable.FullName;
        }

        if (tomlTable.Count == 0)
        {
          await writer.BeginWriteInlineTableItemAsync(default, isFirstItem).ConfigureAwait(false);
          await writer.BeginWriteKeyAsync(context.CurrentKeyForFromKeyTables).ConfigureAwait(false);
          await writer.EndWriteKeyAsync(context.CurrentKeyForFromKeyTables).ConfigureAwait(false);
          await tomlTable.AcceptVisitorAsync(this, context).ConfigureAwait(false);
          await writer.EndWriteInlineTableItemAsync(default, isFirstItem).ConfigureAwait(false);
        }
        else
        {
          foreach (var keyPair in tomlTable)
          {
            if (keyPair.Value is TomlTable nextTable && nextTable.Count > 0)
            {
              await writeFromKeyTable(nextTable).ConfigureAwait(false);
              continue;
            }

            var key = context.CurrentKeyForFromKeyTables.Clone();
            key.LastKeyPart.NextKeyPart = keyPair.Key;
            await writer.BeginWriteInlineTableItemAsync(keyPair, isFirstItem).ConfigureAwait(false);
            await writer.BeginWriteKeyAsync(key).ConfigureAwait(false);
            await writer.EndWriteKeyAsync(key).ConfigureAwait(false);
            await keyPair.Value.AcceptVisitorAsync(this, context).ConfigureAwait(false);
            await writer.EndWriteInlineTableItemAsync(keyPair, isFirstItem).ConfigureAwait(false);
            isFirstItem = false;
          }
        }

        context.CurrentKeyForFromKeyTables = oldContextKey;
      }
    }

    public async ValueTask Visit(TomlArray tomlArray,
                                 TomlSerializerVisitorContext context)
    {
      Debug.Assert(context != null);
      var writer = context.TomlWriter;
      await writer.BeginWriteArrayAsync(tomlArray).ConfigureAwait(false);
      foreach (TomlValue item in tomlArray)
      {
        await writer.BeginWriteArrayItemAsync(item).ConfigureAwait(false);
        await item.AcceptVisitorAsync(this, context).ConfigureAwait(false);
        await writer.EndWriteArrayItemAsync(item).ConfigureAwait(false);
      }

      await writer.EndWriteArrayAsync(tomlArray).ConfigureAwait(false);
    }

    public async ValueTask Visit(TomlArrayOfTables tomlArrayOfTables,
                                 TomlSerializerVisitorContext context)
    {
      Debug.Assert(context != null);
      var writer = context.TomlWriter;
      var oldCurrentKeyForTopTables = context.CurrentKeyForTopTables;
      try
      {
        if (context.CurrentKeyForTopTables == null)
        {
          context.CurrentKeyForTopTables = new TomlKey(tomlArrayOfTables.Name, tomlArrayOfTables.FullName.Type);
        }
        else
        {
          context.CurrentKeyForTopTables = context.CurrentKeyForTopTables.Clone();
          context.CurrentKeyForTopTables.LastKeyPart.NextKeyPart = new TomlKey(tomlArrayOfTables.Name, tomlArrayOfTables.FullName.Type);
        }

        var firstItem = true;
        foreach (var item in tomlArrayOfTables)
        {
          if (firstItem && writer.LastState != TomlWriterState.WritingTomlDocument)
          {
            await writer.WriteLineAsync().ConfigureAwait(false);
            firstItem = false;
          }
          await writer.BeginWriteArrayOfTablesAsync(tomlArrayOfTables, context.CurrentKeyForTopTables).ConfigureAwait(false);
          await item.AcceptVisitorAsync(this, context).ConfigureAwait(false);
          await writer.EndWriteArrayOfTablesAsync(tomlArrayOfTables).ConfigureAwait(false);
        }
      }
      finally
      {
        context.CurrentKeyForTopTables = oldCurrentKeyForTopTables;
      }
    }

    public async ValueTask Visit(TomlKey tomlKey,
                                 TomlSerializerVisitorContext context)
    {
      Debug.Assert(context != null);
      var writer = context.TomlWriter;
      await writer.BeginWriteKeyAsync(tomlKey).ConfigureAwait(false);
      await writer.EndWriteKeyAsync(tomlKey).ConfigureAwait(false);
    }
  }
}