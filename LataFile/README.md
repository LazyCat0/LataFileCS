# LataFile (C#)

A lightweight configuration format handler for `.lata` files.

## Features
- Section-based structure (INI-like).
- Support for Strings, Booleans, Integers, and Floats (with `f` suffix).
- Metadata support with version validation.
- Read-only section protection.

## Usage

### Loading a file
```csharp
var lata = new LataFile();
lata.Load("file.lata");
var value = lata.Get("section", "key");
```
### Saving a file
```csharp
lata.SetValue("meta", "version", LataFile.LATA_VERSION);
lata.SetValue("example section", "example key", 100);
lata.SaveToFile("config.lata");
```
### Example for file
```
[meta]
version = 1.2-snapshot

[data]
name = "Maxim"
money = 100
isSomething = true
```