// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.ComponentModel;

namespace QuickStart;

[TinyhandObject(LockObject = "syncObject")]
public partial class SecondData
{
    [KeyAsName] // String key "Id"
    public int Id { get; set; }

    [KeyAsName] // String key "Name"
    [DefaultValue("Hoge")] // The default value for the name property.
    public string Name { get; set; } = string.Empty;

    private object syncObject = new(); // Object for exclusive locking.

    public override string ToString()
        => $"Id: {this.Id}, Name: {this.Name}";
}

public class SecondExample
{
    public SecondExample(ICrystal<SecondData> crystal)
    {
        this.crystal = crystal; // Get an ICrystal interface for data storage operations.
    }

    public async Task Process()
    {
        var data = this.crystal.Data; // Get a data instance via ICrystal interface.

        Console.WriteLine($"Load {data.ToString()}"); // Id: 0 Name: Hoge
        data.Id++;
        data.Name = "Fuga";
        Console.WriteLine($"Save {data.ToString()}"); // Id: 1 Name: Fuga

        await this.crystal.Save(); // Save data.
    }

    private ICrystal<SecondData> crystal;
}

public partial class Program
{
    public static async Task<BuiltUnit?> SecondExample()
    {
        var builder = new CrystalControl.Builder()
            .Configure(context =>
            {
                context.TryAddSingleton<SecondExample>(); // Register SecondExample class.
            })
            .ConfigureCrystal(context =>
            {
                context.AddCrystal<SecondData>(
                    new CrystalConfiguration()
                    {
                        SavePolicy = SavePolicy.Manual, // Timing of saving data is controlled by the application.
                        SaveFormat = SaveFormat.Utf8, // Format is utf8 text.
                        NumberOfHistoryFiles = 2, // 2 history files.
                        FileConfiguration = new LocalFileConfiguration("Local/SecondExample/SecondData.tinyhand"), // Specify the file name to save.
                        BackupFileConfiguration = new LocalFileConfiguration("Backup/SecondExample/SecondData.tinyhand"), // The backup file name.
                        Required = true,
                    });
            });

        var unit = builder.Build(); // Build.
        var crystalizer = unit.Context.ServiceProvider.GetRequiredService<Crystalizer>();
        var result = await crystalizer.PrepareAndLoadAll(true); // Use the default query.
        if (result.IsFailure())
        {// Abort
            return default;
        }
                                            
        var example = unit.Context.ServiceProvider.GetRequiredService<SecondExample>();
        await example.Process();

        return unit;
    }
}
