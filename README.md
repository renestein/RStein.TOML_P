# RStein.TOML
[![RStein.TOML build](https://github.com/renestein/RStein.TOML_P/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/renestein/RStein.TOML_P/actions/workflows/dotnet.yml)

A simple asynchronous .NET library for parsing and generating [TOML](https://toml.io/) (Tom's Obvious Minimal Language) files.
- ✅ Supports .NET 11 (Preview), .NET 10, .NET 9, .NET Framework 4.8, .NET Standard 2.0.
- ✅ Tested against [TOML platform agnostic test suite](https://github.com/toml-lang/toml-test) (TOML 1.0.0 and 1.1.0 compliant).

## Features

- ✅ Support for TOML 1.1 and TOML 1.0.
- ✅ Support for all TOML data types.
- ✅ Memory-efficient low-allocation parser.
- ✅ Asynchronous API with cancellation token, plus a synchronous API.
- ✅ Parse errors report the line and column of the problem.
- ✅ Nullable-aware library with proper annotations.
- ✅ `DateOnly` and `TimeOnly` (TOML local date and local time) supported on .NET 9 and later: `TomlPrimitiveValue` constructors, implicit conversion to `TomlToken` and explicit conversion back.


## Installation
- NuGet Package

```bash
dotnet add package RStein.TOML
```

## Quick Start

The examples use `using RStein.TOML;` and C# 11 raw string literals (`"""`).
On an older language version use a regular or verbatim string instead.

### Deserializing TOML

#### From a String

```csharp
using RStein.TOML;

string tomlContent = """
[package]
name = "MyApp"
version = "1.0.0"
""";

using var cts = new CancellationTokenSource();
TomlTable table = await TomlSerializer.DeserializeAsync(tomlContent, cts.Token);
```

#### From a Stream

```csharp
using var stream = File.Open("config.toml", FileMode.Open, FileAccess.Read);
TomlTable table = await TomlSerializer.DeserializeAsync(stream);
```


### Serializing TOML

#### To a String

```csharp
var table = new TomlTable();
table.Add("name", "MyApp");
table.Add("version", "1.0.0");

string tomlOutput = await TomlSerializer.SerializeToStringAsync(table);
Console.WriteLine(tomlOutput);

// Output:
// name = "MyApp"
// version = "1.0.0"
```

#### To a Stream

```csharp
var rootTable = new TomlTable();
// ... populate table ...

using var stream = File.Open("output.toml", FileMode.Create, FileAccess.Write);
await TomlSerializer.SerializeAsync(rootTable, stream);
```

> **Note:** `SerializeAsync` and `Serialize` dispose the stream you pass in once the document is written.


### Synchronous API

Every operation also has a synchronous counterpart (`Deserialize`, `SerializeToString`, `Serialize`).

```csharp
TomlTable table = TomlSerializer.Deserialize("""
name = "MyApp"
""");

string toml = TomlSerializer.SerializeToString(table);
```



## Working with TOML Data

### Accessing Values

```csharp
string tomlContent = """
[package]
name = "MyApp"
version = "1.0.0"
""";

TomlTable table = await TomlSerializer.DeserializeAsync(tomlContent);

// Access nested values
var packageNode = table["package"];
var nameNode = packageNode["name"];

// Cast to specific types using explicit cast operators
string name = (string?)nameNode ?? "unknown";
string version = (string?)packageNode["version"] ?? "0.0.0";
```

The explicit casts (`string?`, `int?`, `long?`, `double?`, `bool?`, `Guid?`, `DateTime?`, `DateOnly?`, `TimeOnly?`, and others) return `null` when the node is `null`.
Indexers throw `KeyNotFoundException` when a key is missing, so check for optional keys first:

```csharp
TomlTable table = TomlSerializer.Deserialize("""
count = 3
""");

// Optional keys: ContainsKey / TryGetValue
int count = table.ContainsKey("count") ? ((int?)table["count"]) ?? 0 : 0;
bool enabled = table.ContainsKey("enabled") ? ((bool?)table["enabled"]) ?? false : false;

if (table.TryGetValue("count", out var countNode))
{
    Console.WriteLine((int?)countNode);
}
```

### Dates and Times

```csharp
TomlTable table = TomlSerializer.Deserialize("""
released = 2026-10-20T08:30:00Z
day = 2026-10-20
at = 08:30:00
""");

DateTime? released = (DateTime?)table["released"]; // offset date-time
DateOnly? day = (DateOnly?)table["day"];           // local date (.NET 9+)
TimeOnly? at = (TimeOnly?)table["at"];             // local time (.NET 9+)
```

### Arrays and Arrays of Tables

```csharp
TomlTable table = TomlSerializer.Deserialize("""
ports = [8000, 8001]

[[products]]
name = "Hammer"

[[products]]
name = "Nail"
""");

var ports = (TomlArray)table["ports"];
Console.WriteLine($"{ports.Count} ports, first: {(int?)ports[0]}");

foreach (var product in (TomlArray)table["products"])
{
    Console.WriteLine((string?)product["name"]);
}
```

### Creating TOML Documents

```csharp
var config = new TomlTable();

// Add simple key-value pairs
config.Add("title", "TOML Example");
config.Add("version", "1.0.0");
config.Add("count", 42);
config.Add("enabled", true);

// Add arrays
var hosts = new TomlArray();
hosts.Add("localhost");
hosts.Add("example.com");
config.Add("hosts", hosts);

// Add nested tables
var owner = new TomlTable("owner");
owner.Add("name", "Tom Preston-Werner");
owner.Add("email", "tom@example.com");
config.Add(owner.FullName, owner);

// Replace a value through the indexer
config["count"] = 43;

string output = await TomlSerializer.SerializeToStringAsync(config);
Console.WriteLine(output);

// Output (the serializer may add blank lines and a trailing comma after the last array item):
// title = "TOML Example"
// version = "1.0.0"
// count = 43
// enabled = true
// hosts = ["localhost", "example.com", ]
// [owner]
// name = "Tom Preston-Werner"
// email = "tom@example.com"
```


## TOML Version and Settings

TOML 1.1 is the default. Pass a `TomlSettings` to the (de)serialization methods to select TOML 1.0 or to change limits.

```csharp
// "point" is an inline table with a newline: valid in TOML 1.1, not in TOML 1.0.
const string toml = "point = { x = 1,\n y = 2 }";

TomlTable table = TomlSerializer.Deserialize(toml);                                    // OK (TOML 1.1)
// TomlSerializer.Deserialize(toml, new TomlSettings(TomlVersion.Toml10));              // throws TomlSerializerException

var settings = new TomlSettings
{
    TomlVersion = TomlVersion.Toml10,
    MaxNestingDepth = 50 // default: 200, protects against deeply nested untrusted input
};

TomlTable table10 = await TomlSerializer.DeserializeAsync("a = [1, 2]", settings);
string text = await TomlSerializer.SerializeToStringAsync(table10, settings, CancellationToken.None);
```


## Error Handling

Invalid TOML (and documents that cannot be serialized) throw `TomlSerializerException`.
For parse errors the message contains the position and `Line` and `Column` (both 1-based) are set; otherwise they are `null`.

```csharp
try
{
    TomlTable table = TomlSerializer.Deserialize("key = \nother = 1");
}
catch (TomlSerializerException ex)
{
    Console.WriteLine($"Invalid TOML at line {ex.Line}, column {ex.Column}: {ex.Message}");
}
```

Other exceptions: `ArgumentNullException` for `null` arguments, `KeyNotFoundException` for a missing key in a `TomlTable` indexer, and `OperationCanceledException` when the cancellation token is canceled.

```csharp
using var cts = new CancellationTokenSource();
cts.Cancel();

try
{
    await TomlSerializer.DeserializeAsync("a = 1", cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Canceled.");
}
```


## Example: Configuration File Handling

```csharp
// Create a sample appsettings.toml
string configContent = """
[app]
name = "MyApp"

[logging]
level = "info"

[server]
port = 8000
hosts = ["localhost", "127.0.0.1", "example.com"]
""";
await File.WriteAllTextAsync("appsettings.toml", configContent);

// Read configuration
string configText = await File.ReadAllTextAsync("appsettings.toml");
TomlTable config = await TomlSerializer.DeserializeAsync(configText);

// Extract and cast values to specific types
var appNameNode = config["app"]["name"];
var logLevelNode = config["logging"]["level"];
var portNode = config["server"]["port"];
var hostsNode = config["server"]["hosts"];

// Use explicit cast operators to convert to int, string, etc.
string appName = (string?)appNameNode ?? "DefaultApp";
int port = ((int?)portNode) ?? 8000;

Console.WriteLine($"App: {appName}");
Console.WriteLine($"Port: {port}");

// Access array elements
if (hostsNode is TomlArray hostsArray)
{
    Console.WriteLine("Hosts:");
    foreach (var host in hostsArray)
    {
        Console.WriteLine($"  - {(string?)host}");
    }
}

// Modify and save
config["server"]["port"] = 8080;
string updatedConfig = await TomlSerializer.SerializeToStringAsync(config);
await File.WriteAllTextAsync("appsettings.toml", updatedConfig);

// Updated appsettings.toml (the original layout is preserved):
// [app]
// name = "MyApp"
//
// [logging]
// level = "info"
//
// [server]
// port = 8080
// hosts = ["localhost", "127.0.0.1", "example.com", ]
```
## Public API Doc
[API DOC](https://renestein.github.io/RStein.TOML_P/api/RStein.TOML.html)
## License

The software is released under the MIT license. See [LICENSE.md](LICENSE.md) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a pull request.
