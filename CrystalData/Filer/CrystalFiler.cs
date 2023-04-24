// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Filer;

public class CrystalFiler
{
    public CrystalFiler(Crystalizer crystalizer)
    {
        this.crystalizer = crystalizer;
        this.configuration = CrystalConfiguration.Default;
        this.prefix = string.Empty;
        this.extension = string.Empty;
    }

    #region PropertyAndField

    public bool IsProtected => this.configuration.NumberOfBackups > 0;

    private Crystalizer crystalizer;
    private CrystalConfiguration configuration;
    private IRawFiler? rawFiler;
    private string prefix; // "Directory/File."
    private string extension; // string.Empty or ".extension"

    private object syncObject = new();
    private SortedSet<Waypoint> waypoints = new();

    #endregion

    public async Task<CrystalResult> PrepareAndCheck(PrepareParam param, CrystalConfiguration configuration)
    {
        this.configuration = configuration;
        var fileConfiguration = this.configuration.FileConfiguration;

        // Filer
        if (this.rawFiler == null)
        {
            this.rawFiler = this.crystalizer.ResolveRawFiler(fileConfiguration);
            var result = await this.rawFiler.PrepareAndCheck(param, fileConfiguration).ConfigureAwait(false);
            if (result.IsFailure())
            {
                return result;
            }
        }

        // identifier/extension
        this.extension = Path.GetExtension(fileConfiguration.Path) ?? string.Empty;
        this.prefix = fileConfiguration.Path.Substring(0, fileConfiguration.Path.Length - this.extension.Length) + ".";

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

        if (!this.IsProtected)
        {// Prefix.Extension
            return this.rawFiler.WriteAsync(this.GetFilePath(), 0, new(data));
        }

        lock (this.syncObject)
        {
            this.waypoints.Add(waypoint);
        }

        var path = this.GetFilePath(waypoint);
        return this.rawFiler.WriteAsync(path, 0, new(data));
    }

    public Task<CrystalResult> LimitNumberOfFiles()
    {
        if (this.rawFiler == null)
        {
            return Task.FromResult(CrystalResult.NotPrepared);
        }

        var numberOfFiles = 1 + this.configuration.NumberOfBackups;

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

        var array = this.GetReverseWaypointArray();
        string path;
        CrystalMemoryOwnerResult result;

        if (!this.IsProtected)
        {
            path = this.GetFilePath();
            result = await this.rawFiler.ReadAsync(path, 0, -1).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                return (result, default);
            }
        }

        foreach (var x in array)
        {
            path = this.GetFilePath(x);
            result = await this.rawFiler.ReadAsync(path, 0, -1).ConfigureAwait(false);
            if (result.IsSuccess &&
                FarmHash.Hash64(result.Data.Memory.Span) == x.Hash)
            {// Success
                return (result, x);
            }
        }

        return (new(CrystalResult.NotFound), Waypoint.Invalid);
    }

    public Task<CrystalResult> DeleteAllAsync()
    {
        if (this.rawFiler == null)
        {
            return Task.FromResult(CrystalResult.NotPrepared);
        }

        List<string> pathList;
        lock (this.syncObject)
        {
            pathList = this.waypoints.Select(x => this.GetFilePath(x)).ToList();
            pathList.Add(this.GetFilePath());
            this.waypoints.Clear();
        }

        var tasks = pathList.Select(x => this.rawFiler.DeleteAsync(x)).ToArray();
        return Task.WhenAll(tasks).ContinueWith(x => CrystalResult.Success);
    }

    private string GetFilePath()
    {
        return $"{this.prefix.Substring(0, this.prefix.Length - 1)}{this.extension}";
    }

    private string GetFilePath(Waypoint waypoint)
    {
        return $"{this.prefix}{waypoint.ToBase64Url()}{this.extension}";
    }

    private Waypoint[] GetReverseWaypointArray()
    {
        lock (this.syncObject)
        {
            return this.waypoints.Reverse().ToArray();
        }
    }

    private bool TryGetLatestWaypoint(out Waypoint waypoint)
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
    {// Prefix/Data.Waypoint.Extension or Prefix/Data.Extension
        if (this.rawFiler == null)
        {
            return;
        }

        var listResult = await this.rawFiler.ListAsync(this.prefix).ConfigureAwait(false); // "Folder/Data."

        lock (this.syncObject)
        {
            this.waypoints.Clear();

            foreach (var x in listResult.Where(a => a.IsFile))
            {
                var path = x.Path;
                if (!string.IsNullOrEmpty(this.extension))
                {
                    if (path.EndsWith(this.extension))
                    {// Prefix/Data.Waypoint.Extension or Prefix/Data.Extension
                        path = path.Substring(0, path.Length - this.extension.Length);
                    }
                    else
                    {// No .Extension
                        continue;
                    }
                }

                var index = path.LastIndexOf('.');
                if (index < 0)
                {
                    continue;
                }

                index++;
                path = path.Substring(index, path.Length - index);
                if (Waypoint.TryParse(path, out var waypoint))
                {// Data.Waypoint.Extension
                    this.waypoints.Add(waypoint);
                }
            }
        }
    }
}
