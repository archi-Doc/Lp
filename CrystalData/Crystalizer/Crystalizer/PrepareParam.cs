// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public class PrepareParam
{
    public PrepareParam(Crystalizer crystalizer, Type dataType, CrystalPrepare? prepare)
    {
        prepare ??= CrystalPrepare.Default;

        this.Crystalizer = crystalizer;
        this.DataType = dataType;
        this.DataTypeName = this.DataType.FullName ?? string.Empty;
        this.QueryDelegate = prepare.QueryDelegate;
    }

    public void RegisterConfiguration(PathConfiguration configuration, out bool newlyRegistered)
    {
        this.Crystalizer.CrystalCheck.RegisterDataAndConfiguration(
            new(this.DataTypeName, configuration),
            out newlyRegistered);
    }

    /*public DataAndConfigurationIdentifier GetDataAndConfiguration(PathConfiguration configuration)
        => new DataAndConfigurationIdentifier(this.DataTypeName, configuration);*/

    public Task<AbortOrComplete> Query(CrystalStartResult query, string[]? list = null)
        => this.QueryDelegate == null ? Task.FromResult(AbortOrComplete.Complete) : this.QueryDelegate(query, list);

    public Crystalizer Crystalizer { get; }

    public Type DataType { get; }

    public string DataTypeName { get; }

    public CrystalPrepareQueryDelegate? QueryDelegate { get; }
}
