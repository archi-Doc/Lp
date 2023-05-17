## CrystalData is a storage engine for C#

- Very versatile and easy to use.
- Covers a wide range of storage needs.

- Full serialization features integrated with [Tinyhand](https://github.com/archi-Doc/Tinyhand).



## Table of Contents

- [Requirements](#requirements)
- [Quick Start](#quick-start)



## Requirements

**Visual Studio 2022** or later for Source Generator V2.

**C# 11** or later for generated codes.

**.NET 7** or later target framework.



## Quick start

Install CrystalData using Package Manager Console.

```
Install-Package CrystalData
```

This is a small example code to use CrystalData.

```csharp
// First, create a class to represent the data content.
[TinyhandObject] // Annotate TinyhandObject attribute to make this class serializable.
public partial class FirstData
{
    [Key(0)] // The key attribute specifies the index at serialization
    public int Id { get; set; }

    [Key(1)]
    [DefaultValue("Hoge")] // The default value for the name property.
    public string Name { get; set; } = string.Empty;

    public override string ToString()
        => $"Id: {this.Id}, Name: {this.Name}";
}
```

```csharp
// Create a builder to organize dependencies and register data configurations.
var builder = new CrystalControl.Builder()
    .ConfigureCrystal(context =>
    {
        // Register SimpleData configuration.
        context.AddCrystal<FirstData>(
            new CrystalConfiguration()
            {
                SavePolicy = SavePolicy.Manual, // Timing of saving data is controlled by the application.
                SaveFormat = SaveFormat.Utf8, // Format is utf8 text.
                NumberOfHistoryFiles = 0, // No history file.
                FileConfiguration = new LocalFileConfiguration("Local/SimpleExample/SimpleData.tinyhand"), // Specify the file name to save.
            });
    });

var unit = builder.Build(); // Build.
var crystalizer = unit.Context.ServiceProvider.GetRequiredService<Crystalizer>(); // Obtains a Crystalizer instance for data storage operations.
await crystalizer.PrepareAndLoadAll(false); // Prepare resources for storage operations and read data from files.

var data = unit.Context.ServiceProvider.GetRequiredService<FirstData>(); // Retrieve a data instance from the service provider.

Console.WriteLine($"Load {data.ToString()}"); // Id: 0 Name: Hoge
data.Id = 1;
data.Name = "Fuga";
Console.WriteLine($"Save {data.ToString()}"); // Id: 1 Name: Fuga

await crystalizer.SaveAll(); // Save all data.
```



## CrystalConfiguration




## Timing of data persistence
Data persistence is a core feature of CrystalData and its timing is critical.
There are several options for when to save data.

### Instant save
Save the data after it has been changed, and wait until the process is complete.

```csharp
data.id = 2;
await crystal.Save();
```



### On changed

When data is changed, it is registered in the save queue and will be saved in a second.

```csharp
data.Id = 2; // Generated property
```



```csharp
data.id = 2;
data.TrySave
```




### Manual
Timing of saving data is controlled by the application.



### Periodic




### When exiting the application
Add the following code to save all data and release resources when the application exits.

```csharp
await unit.Context.ServiceProvider.GetRequiredService<Crystalizer>().SaveAllAndTerminate();
```




## Timing of configuration and instantiation

### Builder pattern
Create a **CrystalControl.Builder** and register Data using the **ConfigureCrystal()** and **AddCrystal()** methods. As Data is registered in the DI container, it can be easily used.

```csharp
var builder = new CrystalControl.Builder()
    .ConfigureCrystal(context =>
    {
        context.AddCrystal<ManualClass>(
            new(SavePolicy.Manual, new RelativeFileConfiguration("Local/manual.tinyhand"))
            {
                SaveFormat = SaveFormat.Utf8,
                NumberOfHistoryFiles = 2,
                BackupFileConfiguration = new LocalFileConfiguration("Backup/manual.tinyhand")
            });
    });
var unit = builder.Build();
```

```csharp
internal class TestClass
{
    public TestClass(ManualClass manualClass)
    {
    }
}
```



### Crystalizer
Create an **ICrystal** object using the **Crystalizer**.

If it's a new instance, make sure to register the configuration. If it has already been registered with the Builder, utilize the registered configuration.

```csharp
public NtpCorrection(UnitContext context, ILogger<NtpCorrection> logger, Crystalizer crystalizer)
        : base(context)
    {
        this.logger = null; // logger;

        this.crystal = crystalizer.GetOrCreateCrystal<Data>(new CrystalConfiguration() with
        {
            SaveFormat = SaveFormat.Utf8,
            FileConfiguration = new RelativeFileConfiguration(Filename),
            NumberOfHistoryFiles = 0,
        });

        this.data = this.crystal.Data;

        this.ResetHostnames();
    }
```



## Data class
public class Data
{
}

Add lock object:
If you need exclusive access for multi-threading, please add Lock object

## Template data class