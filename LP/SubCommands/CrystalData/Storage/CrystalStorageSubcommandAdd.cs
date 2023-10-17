// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Filer;
using CrystalData.Results;
using SimpleCommandLine;

namespace LP.Subcommands.CrystalData;

/*[SimpleCommand("add", Description = "Add storage.")]
public class CrystalStorageSubcommandAdd : ISimpleCommandAsync<CrystalStorageOptionsAdd>
{
    public CrystalStorageSubcommandAdd(ILogger<CrystalStorageSubcommandAdd> logger, IUserInterfaceService userInterfaceService, IBigCrystal crystal, CrystalStorageSubcommandLs crystalDirSubcommandLs)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.crystal = crystal;
        this.CrystalDirSubcommandLs = crystalDirSubcommandLs;
    }

    public async Task RunAsync(CrystalStorageOptionsAdd options, string[] args)
    {
        if (options.Capacity != 0)
        {
            options.capacityInBytes = (long)options.Capacity * 1024 * 1024 * 1024;
        }

        if (string.IsNullOrEmpty(options.Filer) || options.Filer == "local")
        {
            await this.AddLocal(options, args);
        }
        else if (options.Filer == "s3")
        {
            await this.AddS3(options, args);
        }
        else
        {
            await this.userInterfaceService.Notify(LogLevel.Warning, Hashed.Storage.InvalidFiler, options.Filer);
        }
    }

    private async Task AddLocal(CrystalStorageOptionsAdd options, string[] args)
    {
        this.userInterfaceService.WriteLine(HashedString.Get(Hashed.Storage.CreateLocal));

EnterPath:
        var path = await this.userInterfaceService.RequestString(true, Hashed.Storage.EnterPath);
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var resultCheck = LocalFiler.Check(default!, path);
        if (resultCheck != AddStorageResult.Success)
        {
            this.userInterfaceService.WriteLine(HashedString.FromEnum(resultCheck));
            goto EnterPath;
        }

        var resultAdd = this.crystal.GroupStorage.AddStorage_SimpleLocal(path, options.capacityInBytes);
        if (resultAdd.Result == AddStorageResult.Success)
        {
            this.logger.TryGet()?.Log($"Storage added: {resultAdd.Id:x4}");
            this.userInterfaceService.WriteLine();
            await this.CrystalDirSubcommandLs.RunAsync(Array.Empty<string>());
        }
    }

    private async Task AddS3(CrystalStorageOptionsAdd options, string[] args)
    {
        this.userInterfaceService.WriteLine(HashedString.Get(Hashed.Storage.CreateS3));

EnterBucket:
        var bucket = await this.userInterfaceService.RequestString(true, Hashed.Storage.EnterBucket);
        if (string.IsNullOrEmpty(bucket))
        {
            return;
        }

        var resultCheck = S3Filer.Check(this.crystal.GroupStorage, bucket, string.Empty);
        if (resultCheck != AddStorageResult.Success)
        {
            this.userInterfaceService.WriteLine(HashedString.FromEnum(resultCheck));
            goto EnterBucket;
        }

// EnterPath:
        var path = await this.userInterfaceService.RequestString(true, Hashed.Storage.EnterPath);
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var resultAdd = this.crystal.GroupStorage.AddStorage_SimpleS3(bucket, path, options.capacityInBytes);
        if (resultAdd.Result == AddStorageResult.Success)
        {
            this.logger.TryGet()?.Log($"Storage added: {resultAdd.Id:x4}");
            this.userInterfaceService.WriteLine();
            await this.CrystalDirSubcommandLs.RunAsync(Array.Empty<string>());
        }
    }

    public CrystalStorageSubcommandLs CrystalDirSubcommandLs { get; private set; }

    private ILogger<CrystalStorageSubcommandAdd> logger;
    private IUserInterfaceService userInterfaceService;
    private IBigCrystal crystal;
}

public record CrystalStorageOptionsAdd
{
    // [SimpleOption("storage", Description = "Storage type (simple)")]
    // public string Storage { get; init; } = string.Empty;

    [SimpleOption("filer", Description = "Filer type (local, s3)", Required = true)]
    public string Filer { get; init; } = string.Empty;

    [SimpleOption("capacity", Description = "Directory capacity in GB")]
    public int Capacity { get; set; } = 0;

    internal long capacityInBytes { get; set; } = GroupStorage.DefaultStorageCapacity;
}*/
