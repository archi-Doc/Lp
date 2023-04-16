// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public class CrystalCheck
{
    public CrystalCheck(ILogger<CrystalCheck> logger)
    {
        this.logger = logger;
    }

    internal void Load(string filePath)
    {
        try
        {
            this.filePath = filePath;
            var bytes = File.ReadAllBytes(filePath);
        }
        catch
        {
            this.logger.TryGet(LogLevel.Warning)?.Log($"Could not read check file: {this.filePath}");
        }
    }

    private ILogger logger;
    private string filePath = string.Empty;
}
