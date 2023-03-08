// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using static Arc.Unit.ByteArrayPool;

namespace CrystalData;

internal class CrystalDirectoryWorker : TaskWorker<CrystalDirectoryWork>
{
    public const int DefaultConcurrentTasks = 4;
    public const int RetryInterval = 10; // 10 ms

    public CrystalDirectoryWorker(ThreadCoreBase parent, CrystalDirectory crystalDirectory)
        : base(parent, Process, true)
    {
        this.NumberOfConcurrentTasks = DefaultConcurrentTasks;
        this.SetCanStartConcurrentlyDelegate((workInterface, workingList) =>
        {// Lock IO order
            var id = workInterface.Work.SnowflakeId;
            foreach (var x in workingList)
            {
                if (x.Work.SnowflakeId == id)
                {
                    return false;
                }
            }

            return true;
        });

        this.CrystalDirectory = crystalDirectory;
    }

    public static async Task Process(TaskWorker<CrystalDirectoryWork> w, CrystalDirectoryWork work)
    {
        var worker = (CrystalDirectoryWorker)w;
        string? filePath = null;
        var tryCount = 0;

        if (work.Type == CrystalDirectoryWork.WorkType.Save)
        {// Save
            var hash = new byte[CrystalDirectory.HashSize];
            BitConverter.TryWriteBytes(hash, Arc.Crypto.FarmHash.Hash64(work.SaveData.Memory.Span));

            var path = worker.CrystalDirectory.GetSnowflakePath(work.SnowflakeId);
            var directoryPath = Path.Combine(worker.CrystalDirectory.RootedPath, path.Directory);

TrySave:
            tryCount++;
            if (tryCount > 2)
            {
                return;
            }

            try
            {
                filePath = Path.Combine(directoryPath, path.File);
                using (var handle = File.OpenHandle(filePath, mode: FileMode.OpenOrCreate, access: FileAccess.Write))
                {
                    await RandomAccess.WriteAsync(handle, hash, 0, worker.CancellationToken).ConfigureAwait(false);
                    await RandomAccess.WriteAsync(handle, work.SaveData.Memory, CrystalDirectory.HashSize, worker.CancellationToken).ConfigureAwait(false);
                    worker.CrystalDirectory.Logger?.TryGet()?.Log($"Written {filePath}, {work.SaveData.Memory.Length}");
                }
            }
            catch (DirectoryNotFoundException)
            {
                Directory.CreateDirectory(directoryPath);
                worker.CrystalDirectory.Logger?.TryGet()?.Log($"CreateDirectory {directoryPath}");
                goto TrySave;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
                worker.CrystalDirectory.Logger?.TryGet()?.Log($"Retry {filePath}");
                goto TrySave;
            }
            finally
            {
                work.SaveData.Return();
            }
        }
        else if (work.Type == CrystalDirectoryWork.WorkType.Load)
        {// Load
            try
            {
                var path = worker.CrystalDirectory.GetSnowflakePath(work.SnowflakeId);
                filePath = Path.Combine(worker.CrystalDirectory.RootedPath, path.Directory, path.File);
                using (var handle = File.OpenHandle(filePath, mode: FileMode.Open, access: FileAccess.Read))
                {
                    var hash = new byte[CrystalDirectory.HashSize];
                    var read = await RandomAccess.ReadAsync(handle, hash, 0, worker.CancellationToken).ConfigureAwait(false);
                    if (read != CrystalDirectory.HashSize)
                    {
                        goto DeleteAndExit;
                    }

                    var memoryOwner = ByteArrayPool.Default.Rent(work.LoadSize).ToMemoryOwner(0, work.LoadSize);
                    read = await RandomAccess.ReadAsync(handle, memoryOwner.Memory, CrystalDirectory.HashSize, worker.CancellationToken).ConfigureAwait(false);
                    if (read != work.LoadSize)
                    {
                        goto DeleteAndExit;
                    }

                    if (BitConverter.ToUInt64(hash) != Arc.Crypto.FarmHash.Hash64(memoryOwner.Memory.Span))
                    {
                        goto DeleteAndExit;
                    }

                    work.LoadData = memoryOwner;
                    worker.CrystalDirectory.Logger?.TryGet()?.Log($"Read {filePath}, {memoryOwner.Memory.Length}");
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
                worker.CrystalDirectory.Logger?.TryGet()?.Log($"Read exception {filePath}");
            }
            finally
            {
            }
        }
        else if (work.Type == CrystalDirectoryWork.WorkType.Delete)
        {
            try
            {
                var path = worker.CrystalDirectory.GetSnowflakePath(work.SnowflakeId);
                filePath = Path.Combine(worker.CrystalDirectory.RootedPath, path.Directory, path.File);
                File.Delete(filePath);
            }
            catch
            {
            }
            finally
            {
                worker.CrystalDirectory.RemoveSnowflake(work.SnowflakeId);
            }
        }

        return;

DeleteAndExit:
        if (filePath != null)
        {
            File.Delete(filePath);
            worker.CrystalDirectory.Logger?.TryGet()?.Log($"DeleteAndExit {filePath}");
        }

        return;
    }

    public CrystalDirectory CrystalDirectory { get; }

    /*private bool CachedCreateDirectory(string path)
    {
        if (this.createdDirectories.Add(path))
        {// Create
            Directory.CreateDirectory(path);
            return true;
        }
        else
        {// Already created
            return false;
        }
    }

    private HashSet<string> createdDirectories = new();*/
}

internal class CrystalDirectoryWork : IEquatable<CrystalDirectoryWork>
{
    public enum WorkType
    {
        Save,
        Load,
        Delete,
    }

    public WorkType Type { get; }

    public uint SnowflakeId { get; }

    public ByteArrayPool.ReadOnlyMemoryOwner SaveData { get; }

    public int LoadSize { get; }

    public ByteArrayPool.MemoryOwner LoadData { get; internal set; }

    public CrystalDirectoryWork(uint snowflakeId, ByteArrayPool.ReadOnlyMemoryOwner saveData)
    {// Save
        this.Type = WorkType.Save;
        this.SnowflakeId = snowflakeId;
        this.SaveData = saveData;
    }

    public CrystalDirectoryWork(uint snowflakeId, int loadSize)
    {// Load
        this.Type = WorkType.Load;
        this.SnowflakeId = snowflakeId;
        this.LoadSize = loadSize;
    }

    public CrystalDirectoryWork(uint snowflakeId)
    {// Remove
        this.Type = WorkType.Delete;
        this.SnowflakeId = snowflakeId;
    }

    public override int GetHashCode()
        => HashCode.Combine(this.Type, this.SnowflakeId, this.SaveData.Memory.Length, this.LoadSize);

    public bool Equals(CrystalDirectoryWork? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.Type == other.Type &&
            this.SnowflakeId == other.SnowflakeId &&
            this.SaveData.Memory.Span.SequenceEqual(other.SaveData.Memory.Span) &&
            this.LoadSize == other.LoadSize;
    }
}
