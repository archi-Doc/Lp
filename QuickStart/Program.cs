// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

global using Arc.Threading;
global using Arc.Unit;
global using CrystalData;
global using Microsoft.Extensions.DependencyInjection;
global using Tinyhand;
using System.ComponentModel;

namespace QuickStart;

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

public partial class Program
{
    public static async Task Main(string[] args)
    {
        AppDomain.CurrentDomain.ProcessExit += (s, e) =>
        {// Console window closing or process terminated.
            ThreadCore.Root.Terminate(); // Send a termination signal to the root.
            ThreadCore.Root.TerminationEvent.WaitOne(2000); // Wait until the termination process is complete (#1).
        };

        Console.CancelKeyPress += (s, e) =>
        {// Ctrl+C pressed
            e.Cancel = true;
            ThreadCore.Root.Terminate(); // Send a termination signal to the root.
        };

        // var unit = await FirstExample();
        var unit = await SecondExample();

        ThreadCore.Root.Terminate();
        if (unit is not null)
        {
            await unit.Context.ServiceProvider.GetRequiredService<Crystalizer>().SaveAllAndTerminate();
        }
        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.
        if (unit?.Context.ServiceProvider.GetService<UnitLogger>() is { } unitLogger)
        {
            await unitLogger.FlushAndTerminate();
        }

        ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
    }

    public static async Task<BuiltUnit> FirstExample()
    {
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
                        FileConfiguration = new LocalFileConfiguration("Local/FirstExample/FirstData.tinyhand"), // Specify the file name to save.
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

        return unit;
    }
}
