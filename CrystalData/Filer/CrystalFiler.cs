// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Filer;

public class CrystalFiler
{
    public CrystalFiler(Crystalizer crystalizer)
    {
        this.crystalizer = crystalizer;
        this.configuration = EmptyFileConfiguration.Default;
    }

    #region PropertyAndField

    private Crystalizer crystalizer;
    private FileConfiguration configuration;
    private IRawFiler? rawFiler;

    private object syncObject = new();
    private SortedSet<Waypoint> waypoints = new();

    #endregion

    public async Task<CrystalResult> PrepareAndCheck(PrepareParam param, FileConfiguration configuration)
    {
        this.configuration = configuration;

        // Filer
        if (this.rawFiler == null)
        {
            this.rawFiler = this.crystalizer.ResolveRawFiler(this.configuration);
            var result = await this.rawFiler.PrepareAndCheck(param, this.configuration).ConfigureAwait(false);
            if (result.IsFailure())
            {
                return result;
            }
        }

        // List data
        await this.ListData();

        return CrystalResult.Success;
    }

    public Task<CrystalResult> Save(byte[] data, Waypoint waypoint)
    {
        if (this.rawFiler == null)
        {
            return Task.FromResult(CrystalResult.NotPrepared);
        }

        var path = this.GetFilePath(waypoint);
        return this.rawFiler.WriteAsync(path, 0, new(data));
    }

    public Task<CrystalResult> LimitNumberOfFiles(int numberOfFiles)
    {
        if (this.rawFiler == null)
        {
            return Task.FromResult(CrystalResult.NotPrepared);
        }

        if (numberOfFiles < 1)
        {
            numberOfFiles = 1;
        }

        Waypoint[] array;
        string[] pathArray;
        lock (this.syncObject)
        {
            array = this.waypoints.Take(this.waypoints.Count - numberOfFiles).ToArray();
            if (array.Length == 0)
            {
                return Task.FromResult(CrystalResult.Success);
            }

            pathArray = array.Select(x => this.GetFilePath(x)).ToArray();

            foreach (var x in array)
            {
                this.waypoints.Remove(x);
            }
        }

        var tasks = pathArray.Select(x => this.rawFiler.DeleteAsync(x)).ToArray();
        return Task.WhenAll(tasks).ContinueWith(x => CrystalResult.Success);
    }

    public async Task<(CrystalMemoryOwnerResult Result, Waypoint Waypoint)> LoadLatest()
    {
        if (this.rawFiler == null)
        {
            return (new(CrystalResult.NotPrepared), Waypoint.Invalid);
        }

        if (!this.TryGetLatest(out var waypoint))
        {
            return (new(CrystalResult.NotFound), Waypoint.Invalid);
        }

        var path = this.GetFilePath(waypoint);
        var result = await this.rawFiler.ReadAsync(path, 0, -1).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return (new(result.Result), Waypoint.Invalid);
        }

        if (FarmHash.Hash64(result.Data.Memory.Span) != waypoint.Hash)
        {// Hash does not match
            return (new(CrystalResult.CorruptedData), waypoint);
        }

        return (result, waypoint);
    }

    public Task<CrystalResult> DeleteAllAsync()
    {
        if (this.rawFiler == null)
        {
            return Task.FromResult(CrystalResult.NotPrepared);
        }

        string[] pathArray;
        lock (this.syncObject)
        {
            pathArray = this.waypoints.Select(x => this.GetFilePath(x)).ToArray();
            this.waypoints.Clear();
        }

        var tasks = pathArray.Select(x => this.rawFiler.DeleteAsync(x)).ToArray();
        return Task.WhenAll(tasks).ContinueWith(x => CrystalResult.Success);
    }

    private string GetFilePath(Waypoint waypoint)
    {
        return $"{this.configuration.Path}.{waypoint.ToBase64Url()}";
    }

    private bool TryGetLatest(out Waypoint waypoint)
    {
        lock (this.syncObject)
        {
            if (this.waypoints.Count == 0)
            {
                waypoint = default;
                return false;
            }

            waypoint = this.waypoints.Last();
            return true;
        }
    }

    private async Task ListData()
    {
        if (this.rawFiler == null)
        {
            return;
        }

        var listResult = await this.rawFiler.ListAsync(this.configuration.Path + ".").ConfigureAwait(false); // "Folder/Data.Waypoint"

        lock (this.syncObject)
        {
            this.waypoints.Clear();

            foreach (var x in listResult.Where(a => a.IsFile))
            {
                var path = x.Path;
                var index = path.LastIndexOf('.');
                if (index >= 0)
                {
                    path = path.Substring(index + 1);
                }

                if (Waypoint.TryParse(path, out var waypoint))
                {
                    this.waypoints.Add(waypoint);
                }
            }
        }
    }
}
