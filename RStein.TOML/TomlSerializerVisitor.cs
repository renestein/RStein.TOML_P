using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace RStein.TOML
{
  internal class TomlSerializerVisitor : ITomlVisitor<TomlSerializerVisitorContext>
  {
    public async ValueTask VisitAsync(TomlComment tomlComment,
                                 TomlSerializerVisitorContext context)
    {
      Debug.Assert(context != null);
      var writer = context.TomlWriter;
      if (context.UseAsync)
      {
        await writer.BeginWriteCommentAsync(tomlComment).ConfigureAwait(false);
        await writer.EndWriteCommentAsync(tomlComment).ConfigureAwait(false);
      }
      else
      {
        writer.BeginWriteComment(tomlComment);
        writer.EndWriteComment(tomlComment);
      }
    }

    public async ValueTask VisitAsync(TomlKeyValue tomlKeyValue,
                                 TomlSerializerVisitorContext context)
    {
      Debug.Assert(context != null);
      await tomlKeyValue.Key.AcceptVisitorAsync(this, context).ConfigureAwait(false);
      await tomlKeyValue.Value.AcceptVisitorAsync(this, context).ConfigureAwait(false);
    }

    public async ValueTask VisitAsync(TomlPrimitiveValue tomlPrimitiveValue,
                                 TomlSerializerVisitorContext context)
    {
      Debug.Assert(context != null);
      var writer = context.TomlWriter;
      if (context.UseAsync)
      {
        await writer.BeginWritePrimitiveValueAsync(tomlPrimitiveValue).ConfigureAwait(false);
        await writer.EndWritePrimitiveValueAsync(tomlPrimitiveValue).ConfigureAwait(false);
      }
      else
      {
        writer.BeginWritePrimitiveValue(tomlPrimitiveValue);
        writer.EndWritePrimitiveValue(tomlPrimitiveValue);
      }
    }

    public async ValueTask VisitAsync(TomlTable tomlTable,
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
        if (context.UseAsync)
          await writer.BeginTomlDocumentAsync().ConfigureAwait(false);
        else
          writer.BeginTomlDocument();
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
              if (context.UseAsync)
              {
                await writer.BeginWriteKeyAsync(key).ConfigureAwait(false);
                await writer.EndWriteKeyAsync(key).ConfigureAwait(false);
              }
              else
              {
                writer.BeginWriteKey(key);
                writer.EndWriteKey(key);
              }
            }
            else
            {
              if ((tomlTable.HasTopLevelDefinition) && !isRootTableName && !emittedBeginStandardTable)
              {
                Debug.Assert(context.CurrentKeyForTopTables != null);
                if (context.UseAsync)
                  await writer.BeginWriteTableAsync(tomlTable, context.CurrentKeyForTopTables).ConfigureAwait(false);
                else
                  writer.BeginWriteTable(tomlTable, context.CurrentKeyForTopTables);
                emittedBeginStandardTable = true;
              }

              if (context.UseAsync)
              {
                await writer.BeginWriteKeyAsync(keyValue.Key).ConfigureAwait(false);
                await writer.EndWriteKeyAsync(keyValue.Key).ConfigureAwait(false);
              }
              else
              {
                writer.BeginWriteKey(keyValue.Key);
                writer.EndWriteKey(keyValue.Key);
              }
            }
          }
          else
          {
            if (tomlTable.HasTopLevelDefinition && !isRootTableName && !emittedBeginStandardTable)
            {
              Debug.Assert(context.CurrentKeyForTopTables != null);
              if (context.UseAsync)
                await writer.BeginWriteTableAsync(tomlTable, context.CurrentKeyForTopTables).ConfigureAwait(false);
              else
                writer.BeginWriteTable(tomlTable, context.CurrentKeyForTopTables);
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

        if (context.UseAsync)
          await writer.EndTomlDocumentAsync().ConfigureAwait(false);
        else
          writer.EndTomlDocument();
      }
      else if (tomlTable.HasFromKeyDefinition)
      {
        if (tomlTable.Count == 0)
        {
          if (context.UseAsync)
          {
            await writer.BeginWriteKeyAsync(tomlTable.FullName).ConfigureAwait(false);
            await writer.EndWriteKeyAsync(tomlTable.FullName).ConfigureAwait(false);
            await writer.BeginWriteInlineTableAsync(tomlTable).ConfigureAwait(false);
            await writer.EndWriteInlineTableAsync(tomlTable).ConfigureAwait(false);
          }
          else
          {
            writer.BeginWriteKey(tomlTable.FullName);
            writer.EndWriteKey(tomlTable.FullName);
            writer.BeginWriteInlineTable(tomlTable);
            writer.EndWriteInlineTable(tomlTable);
          }
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
          if (context.UseAsync)
            await writer.BeginWriteTableAsync(tomlTable, context.CurrentKeyForTopTables).ConfigureAwait(false);
          else
            writer.BeginWriteTable(tomlTable, context.CurrentKeyForTopTables);
          emittedBeginStandardTable = true;
        }

        if (emittedBeginStandardTable)
        {
          Debug.Assert(tomlTable.HasTopLevelDefinition || tomlTable.IsArrayOfTablesMember);
          if (context.UseAsync)
            await writer.EndWriteTableAsync(tomlTable).ConfigureAwait(false);
          else
            writer.EndWriteTable(tomlTable);
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

    public async ValueTask VisitAsync(TomlInlineTable tomlInlineTable,
                                 TomlSerializerVisitorContext context)
    {
      Debug.Assert(context != null);

      var writer = context.TomlWriter;
      if (context.UseAsync)
        await writer.BeginWriteInlineTableAsync(tomlInlineTable).ConfigureAwait(false);
      else
        writer.BeginWriteInlineTable(tomlInlineTable);
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
          if (context.UseAsync)
          {
            await writer.BeginWriteInlineTableItemAsync(keyValuePair, isFirstItem).ConfigureAwait(false);
            await writer.BeginWriteKeyAsync(keyValuePair.Key).ConfigureAwait(false);
            await writer.EndWriteKeyAsync(keyValuePair.Key).ConfigureAwait(false);
          }
          else
          {
            writer.BeginWriteInlineTableItem(keyValuePair, isFirstItem);
            writer.BeginWriteKey(keyValuePair.Key);
            writer.EndWriteKey(keyValuePair.Key);
          }
          await keyValuePair.Value.AcceptVisitorAsync(this, context).ConfigureAwait(false);
          if (context.UseAsync)
            await writer.EndWriteInlineTableItemAsync(keyValuePair, isFirstItem).ConfigureAwait(false);
          else
            writer.EndWriteInlineTableItem(keyValuePair, isFirstItem);
          isFirstItem = false;
        }
      }

      if (context.UseAsync)
        await writer.EndWriteInlineTableAsync(tomlInlineTable).ConfigureAwait(false);
      else
        writer.EndWriteInlineTable(tomlInlineTable);
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
          if (context.UseAsync)
          {
            await writer.BeginWriteInlineTableItemAsync(default, isFirstItem).ConfigureAwait(false);
            await writer.BeginWriteKeyAsync(context.CurrentKeyForFromKeyTables).ConfigureAwait(false);
            await writer.EndWriteKeyAsync(context.CurrentKeyForFromKeyTables).ConfigureAwait(false);
            await tomlTable.AcceptVisitorAsync(this, context).ConfigureAwait(false);
            await writer.EndWriteInlineTableItemAsync(default, isFirstItem).ConfigureAwait(false);
          }
          else
          {
            writer.BeginWriteInlineTableItem(default, isFirstItem);
            writer.BeginWriteKey(context.CurrentKeyForFromKeyTables);
            writer.EndWriteKey(context.CurrentKeyForFromKeyTables);
            await tomlTable.AcceptVisitorAsync(this, context).ConfigureAwait(false);
            writer.EndWriteInlineTableItem(default, isFirstItem);
          }
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
            if (context.UseAsync)
            {
              await writer.BeginWriteInlineTableItemAsync(keyPair, isFirstItem).ConfigureAwait(false);
              await writer.BeginWriteKeyAsync(key).ConfigureAwait(false);
              await writer.EndWriteKeyAsync(key).ConfigureAwait(false);
              await keyPair.Value.AcceptVisitorAsync(this, context).ConfigureAwait(false);
              await writer.EndWriteInlineTableItemAsync(keyPair, isFirstItem).ConfigureAwait(false);
            }
            else
            {
              writer.BeginWriteInlineTableItem(keyPair, isFirstItem);
              writer.BeginWriteKey(key);
              writer.EndWriteKey(key);
              await keyPair.Value.AcceptVisitorAsync(this, context).ConfigureAwait(false);
              writer.EndWriteInlineTableItem(keyPair, isFirstItem);
            }
            isFirstItem = false;
          }
        }

        context.CurrentKeyForFromKeyTables = oldContextKey;
      }
    }

    public async ValueTask VisitAsync(TomlArray tomlArray,
                                 TomlSerializerVisitorContext context)
    {
      Debug.Assert(context != null);
      var writer = context.TomlWriter;
      if (context.UseAsync)
        await writer.BeginWriteArrayAsync(tomlArray).ConfigureAwait(false);
      else
        writer.BeginWriteArray(tomlArray);
      foreach (TomlValue item in tomlArray)
      {
        if (context.UseAsync)
          await writer.BeginWriteArrayItemAsync(item).ConfigureAwait(false);
        else
          writer.BeginWriteArrayItem(item);
        await item.AcceptVisitorAsync(this, context).ConfigureAwait(false);
        if (context.UseAsync)
          await writer.EndWriteArrayItemAsync(item).ConfigureAwait(false);
        else
          writer.EndWriteArrayItem(item);
      }

      if (context.UseAsync)
        await writer.EndWriteArrayAsync(tomlArray).ConfigureAwait(false);
      else
        writer.EndWriteArray(tomlArray);
    }

    public async ValueTask VisitAsync(TomlArrayOfTables tomlArrayOfTables,
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
            if (context.UseAsync)
              await writer.WriteLineAsync().ConfigureAwait(false);
            else
              writer.WriteLine();
            firstItem = false;
          }
          if (context.UseAsync)
          {
            await writer.BeginWriteArrayOfTablesAsync(tomlArrayOfTables, context.CurrentKeyForTopTables).ConfigureAwait(false);
            await item.AcceptVisitorAsync(this, context).ConfigureAwait(false);
            await writer.EndWriteArrayOfTablesAsync(tomlArrayOfTables).ConfigureAwait(false);
          }
          else
          {
            writer.BeginWriteArrayOfTables(tomlArrayOfTables, context.CurrentKeyForTopTables);
            await item.AcceptVisitorAsync(this, context).ConfigureAwait(false);
            writer.EndWriteArrayOfTables(tomlArrayOfTables);
          }
        }
      }
      finally
      {
        context.CurrentKeyForTopTables = oldCurrentKeyForTopTables;
      }
    }

    public async ValueTask VisitAsync(TomlKey tomlKey,
                                 TomlSerializerVisitorContext context)
    {
      Debug.Assert(context != null);
      var writer = context.TomlWriter;
      if (context.UseAsync)
      {
        await writer.BeginWriteKeyAsync(tomlKey).ConfigureAwait(false);
        await writer.EndWriteKeyAsync(tomlKey).ConfigureAwait(false);
      }
      else
      {
        writer.BeginWriteKey(tomlKey);
        writer.EndWriteKey(tomlKey);
      }
    }
  }
}
