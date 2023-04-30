// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record BigCrystalConfiguration : CrystalConfiguration
{
    public const int DefaultMaxDataSize = 1024 * 1024 * 4; // 4MB
    public const int DefaultMaxFragmentSize = 1024 * 4; // 4KB
    public const int DefaultMaxFragmentCount = 1000;

    public static new readonly BigCrystalConfiguration Default = new BigCrystalConfiguration();

    public BigCrystalConfiguration()
        : base()
    {
        this.RegisterDatum = registry => { };
    }

    public BigCrystalConfiguration(Action<DatumRegistry> registerDatum, SavePolicy saveMethod, FileConfiguration fileConfiguration, StorageConfiguration storageConfiguration)
        : base(saveMethod, fileConfiguration, storageConfiguration)
    {
        this.RegisterDatum = registerDatum;
    }

    [IgnoreMember]
    public Action<DatumRegistry> RegisterDatum { get; init; }

    public int MaxDataSize { get; init; } = DefaultMaxDataSize;

    public int MaxFragmentSize { get; init; } = DefaultMaxFragmentSize;

    public int MaxFragmentCount { get; init; } = DefaultMaxFragmentCount;

    public string StorageGroupExtension { get; init; } = ".Storages";
}
