// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CrystalData.Filer;

public class CrystalFiler
{
    private class Destination
    {
        public Destination(CrystalFiler crystalFiler, FileConfiguration fileConfiguration)
        {
            this.crystalFiler = crystalFiler;
            this.fileConfiguration = fileConfiguration;
        }

        #region PropertyAndField

        private CrystalFiler crystalFiler;
        private FileConfiguration fileConfiguration;

        private IRawFiler? rawFiler;
        private object syncObject = new();
        private string prefix = string.Empty; // "Directory/File."
        private string extension = string.Empty; // string.Empty or ".extension"
        private SortedSet<Waypoint>? waypoints;

        #endregion

        public async Task<CrystalResult> PrepareAndCheck(PrepareParam param, CrystalConfiguration configuration)
        {
            if (this.rawFiler == null)
            {
                this.rawFiler = this.crystalFiler.crystalizer.ResolveRawFiler(this.fileConfiguration);
                var result = await this.rawFiler.PrepareAndCheck(param, this.fileConfiguration).ConfigureAwait(false);
                if (result.IsFailure())
                {
                    return result;
                }
            }

            lock (this.syncObject)
            {
                // identifier/extension
                this.extension = Path.GetExtension(this.fileConfiguration.Path) ?? string.Empty;
                this.prefix = this.fileConfiguration.Path.Substring(0, this.fileConfiguration.Path.Length - this.extension.Length) + ".";
            }

            return CrystalResult.Success;
        }

        public async Task ListData()
        {// Prefix/Data.Waypoint.Extension or Prefix/Data.Extension
            if (this.rawFiler == null)
            {
                return;
            }
            else if (this.waypoints != null)
            {// Already loaded
                return;
            }

            var listResult = await this.rawFiler.ListAsync(this.prefix).ConfigureAwait(false); // "Folder/Data."

            lock (this.syncObject)
            {
                this.waypoints ??= new();

                foreach (var x in listResult.Where(a => a.IsFile))
                {
                    var path = x.Path; // {this.prefix}.waypoint{this.extension}
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

                    if (path.Length < (Waypoint.LengthInBase32 + this.prefix.Length))
                    {
                        continue;
                    }

                    var waypointString = path.Substring(path.Length - Waypoint.LengthInBase32, Waypoint.LengthInBase32);
                    path = path.Substring(0, path.Length - Waypoint.LengthInBase32);
                    if (path.EndsWith(this.prefix))
                    {
                        continue;
                    }

                    if (Waypoint.TryParse(waypointString, out var waypoint))
                    {// Data.Waypoint.Extension
                        this.waypoints.Add(waypoint);
                    }
                }
            }
        }

        public Task<CrystalResult> Save(byte[] data, Waypoint waypoint)
        {
            if (this.rawFiler == null)
            {
                return Task.FromResult(CrystalResult.NotPrepared);
            }

            if (!this.crystalFiler.IsProtected)
            {// Prefix.Extension
                return this.rawFiler.WriteAsync(this.GetFilePath(), 0, new(data));
            }

            lock (this.syncObject)
            {
                this.waypoints ??= new();
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
            else if (this.waypoints == null)
            {
                return Task.FromResult(CrystalResult.Success);
            }

            var numberOfFiles = 1 + this.crystalFiler.configuration.NumberOfFiles;

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

            string path;
            CrystalMemoryOwnerResult result;

            if (!this.crystalFiler.IsProtected)
            {
                path = this.GetFilePath();
                result = await this.rawFiler.ReadAsync(path, 0, -1).ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    return (result, default);
                }

                // List data
                await this.ListData();
            }

            var array = this.GetReverseWaypointArray();
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
            else if (this.waypoints == null)
            {
                return Task.FromResult(CrystalResult.Success);
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
            return $"{this.prefix}{waypoint.ToBase32()}{this.extension}";
        }

        private Waypoint[] GetReverseWaypointArray()
        {
            lock (this.syncObject)
            {
                if (this.waypoints == null)
                {
                    return Array.Empty<Waypoint>();
                }

                return this.waypoints.Reverse().ToArray();
            }
        }

        private bool TryGetLatestWaypoint(out Waypoint waypoint)
        {
            lock (this.syncObject)
            {
                if (this.waypoints == null || this.waypoints.Count == 0)
                {
                    waypoint = default;
                    return false;
                }

                waypoint = this.waypoints.Last();
                return true;
            }
        }
    }

    public CrystalFiler(Crystalizer crystalizer)
    {
        this.crystalizer = crystalizer;
        this.configuration = CrystalConfiguration.Default;
    }

    #region PropertyAndField

    public bool IsProtected => this.configuration.NumberOfFiles > 0;

    private Crystalizer crystalizer;
    private CrystalConfiguration configuration;
    private Destination? main;
    private Destination? backup;

    #endregion

    public async Task<CrystalResult> PrepareAndCheck(PrepareParam param, CrystalConfiguration configuration)
    {
        this.configuration = configuration;

        // Destination
        this.main ??= new(this, this.configuration.FileConfiguration);
        var result = await this.main.PrepareAndCheck(param, this.configuration).ConfigureAwait(false);
        if (result.IsFailure())
        {
            return result;
        }

        if (this.configuration.BackupFileConfiguration != null)
        {
            this.backup ??= new(this, this.configuration.BackupFileConfiguration);
            result = await this.backup.PrepareAndCheck(param, this.configuration).ConfigureAwait(false);
            if (result.IsFailure())
            {
                return result;
            }
        }

        if (this.IsProtected)
        {// List data
            await this.main.ListData();
            _ = this.backup?.ListData();
        }

        return CrystalResult.Success;
    }

    public async Task<CrystalResult> Save(byte[] data, Waypoint waypoint)
    {
        if (this.main is null)
        {
            return CrystalResult.NotPrepared;
        }

        var result = await this.main.Save(data, waypoint).ConfigureAwait(false);
        _ = this.backup?.Save(data, waypoint);
        return result;
    }

    public async Task<CrystalResult> LimitNumberOfFiles()
    {
        if (this.main is null)
        {
            return CrystalResult.NotPrepared;
        }

        var result = await this.main.LimitNumberOfFiles().ConfigureAwait(false);
        _ = this.backup?.LimitNumberOfFiles();
        return result;
    }

    public async Task<(CrystalMemoryOwnerResult Result, Waypoint Waypoint)> LoadLatest()
    {
        if (this.main is null)
        {
            return (new(CrystalResult.NotPrepared), Waypoint.Invalid);
        }

        var result = await this.main.LoadLatest().ConfigureAwait(false);
        return result;
    }

    public async Task<CrystalResult> DeleteAllAsync()
    {
        if (this.main is null)
        {
            return CrystalResult.NotPrepared;
        }

        var result = await this.main.DeleteAllAsync().ConfigureAwait(false);
        _ = this.backup?.DeleteAllAsync();
        return result;
    }
}
