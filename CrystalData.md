## CrystalData

CrystalData is a simple and versatile storage engine for C# and it covers a wide range of storage needs.



## Quick start



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