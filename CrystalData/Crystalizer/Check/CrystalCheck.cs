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
        newlyRegistered = this.data.DataAndConfigurations.TryAdd(identifier, 0);
    }

    public bool TryRemovePlane(uint plane)
    {
        return this.data.PlaneToJournalPosition.TryRemove(plane, out _);
    }

    public void SetPlanePosition(uint plane, ulong position)
        => this.data.PlaneToJournalPosition[plane] = position;

    public void TryGetPlanePosition(uint plane, out ulong position)
        => this.data.PlaneToJournalPosition.TryGetValue(plane, out position);

    public void Load(string filePath)
    {
        try
        {
            this.filePath = filePath;
            var bytes = File.ReadAllBytes(filePath);
            var data = TinyhandSerializer.Deserialize<CrystalCheckData>(bytes);
            if (data != null)
            {
                this.data = data;
                this.SuccessfullyLoaded = true;
            }
        }
        catch
        {
            this.logger.TryGet(LogLevel.Error)?.Log($"Could not load the check file: {this.filePath}");
        }
    }

    public void Save()
    {
        /*if (!this.SuccessfullyLoaded)
        {
            return;
        }*/

        try
        {
            File.WriteAllBytes(this.filePath, TinyhandSerializer.Serialize(this.data));
        }
        catch
        {
            this.logger.TryGet(LogLevel.Error)?.Log($"Could not write the check file: {this.filePath}");
        }
    }

    public bool SuccessfullyLoaded { get; internal set; } = false;

    public string FilePath => this.filePath;

    private ILogger logger;
    private string filePath = string.Empty;
    private CrystalCheckData data = TinyhandSerializer.Reconstruct<CrystalCheckData>();
}
