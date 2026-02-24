# RStein.TOML
[![RStein.TOML build](https://github.com/renestein/RStein.TOML_P/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/renestein/RStein.TOML_P/actions/workflows/dotnet.yml)

A simple asynchronous .NET library for parsing and generating [TOML](https://toml.io/) (Tom's Obvious Minimal Language) files.
- ✅ Supports .NET 10, .NET 9, .NET Framework 4.8, .NET Standard 2.0.
- ✅ Tested against [TOML platform agnostic tests](https://github.com/toml-lang/toml-test) (TOML 1.0.0 and 1.1.0 compliant).

## Features

- ✅ Support for TOML 1.1 and TOML 1.0.
- ✅ Support for all TOML data types.
- ✅ Memory-efficient low-allocation parser.
- ✅ Asynchronous API with cancellation token.
- ✅ Nullable-aware library with proper annotations.


## Installation
- NuGet Package

```bash
dotnet add package RStein.TOML
```

## Quick Start

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



## Working with TOML Data

### Accessing Values

```csharp
TomlTable table = await TomlSerializer.DeserializeAsync(tomlContent);

// Access nested values
var packageNode = table["package"];
var nameNode = packageNode["name"];

// Cast to specific types using explicit cast operators
string name = (string?)nameNode ?? "unknown";
int count = ((int?)table["count"]) ?? 0;
bool enabled = ((bool?)table["enabled"]) ?? false;
```

### Creating TOML Documents

```csharp
var config = new TomlTable();

// Add simple key-value pairs
config.Add("title", "TOML Example");
config.Add("version", "1.0.0");
config.Add("count", 42);
config.Add("enabled", true);

// Add nested tables
var owner = new TomlTable("owner");
owner.Add("name", "Tom Preston-Werner");
owner.Add("email", "tom@example.com");
config.Add(owner.FullName, owner);

string output = await TomlSerializer.SerializeToStringAsync(config);
Console.WriteLine(output);

// Output:
// title = "TOML Example"
// version = "1.0.0"
// count = 42
// enabled = true
//
// [owner]
// name = "Tom Preston-Werner"
// email = "tom@example.com"
```

## Example: Configuration File Handling

```csharp
// appsettings.toml
string configContent = """
[app]
name = "MyApp"

[logging]
level = "info"

[server]
port = 8000
hosts = ["localhost", "127.0.0.1", "example.com"]
""";

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

// Updated appsettings.toml:
// [app]
// name = "MyApp"
//
// [logging]
// level = "info"
//
// [server]
// port = 8080
// hosts = ["localhost", "127.0.0.1", "example.com"]
```

## License

See LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit a pull request.
