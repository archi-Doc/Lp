// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Check;

internal class CrystalCheck
{
    public CrystalCheck(ILogger<CrystalCheck> logger)
    {
        this.logger = logger;
    }

    public void RegisterDataAndConfiguration(DataAndConfigurationIdentifier identifier, out bool newlyRegistered)
    {
        newlyRegistered = this.checkData.DataAndConfigurations.TryAdd(identifier, 0);
    }

    public void Load(string filePath)
    {
        try
        {
            this.filePath = filePath;
            var bytes = File.ReadAllBytes(filePath);
            var data = TinyhandSerializer.Deserialize<CrystalCheckData>(bytes);
            if (data != null)
            {
                this.checkData = data;
            }
        }
        catch
        {
            this.logger.TryGet(LogLevel.Error)?.Log($"Could not load check file: {this.filePath}");
        }
    }

    public void Save()
    {
        try
        {
            File.WriteAllBytes(this.filePath, TinyhandSerializer.Serialize(this.checkData));
        }
        catch
        {
            this.logger.TryGet(LogLevel.Error)?.Log($"Could not write check file: {this.filePath}");
        }
    }

    private ILogger logger;
    private string filePath = string.Empty;
    private CrystalCheckData checkData = new();
}
