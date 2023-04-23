// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public class PrepareParam : CrystalPrepare
{
    public static new PrepareParam ContinueAll<TData>(Crystalizer crystalizer)
        => CrystalPrepare.ContinueAll.ToParam<TData>(crystalizer);

    internal PrepareParam(Crystalizer crystalizer, Type dataType)
    {
        this.Crystalizer = crystalizer;
        this.DataType = dataType;
        this.DataTypeName = this.DataType.FullName ?? string.Empty;
    }

    public void RegisterConfiguration(PathConfiguration configuration, out bool newlyRegistered)
    {
        this.Crystalizer.CrystalCheck.RegisterDataAndConfiguration(
            new(this.DataTypeName, configuration),
            out newlyRegistered);
    }

    /*public DataAndConfigurationIdentifier GetDataAndConfiguration(PathConfiguration configuration)
        => new DataAndConfigurationIdentifier(this.DataTypeName, configuration);*/

    public Crystalizer Crystalizer { get; }

    public Type DataType { get; }

    public string DataTypeName { get; }
}
