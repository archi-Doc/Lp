// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.IO;

namespace ZenItz;

internal class ZenDirectoryWorker : TaskWorker<ZenDirectoryWork>
{
    public const int DefaultConcurrentTasks = 4;
    public const int RetryInterval = 10; // 10 ms

    public ZenDirectoryWorker(ThreadCoreBase parent, ZenDirectory zenDirectory)
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

        this.ZenDirectory = zenDirectory;
        // this.logger = Zen.UnitLogger.GetLogger<ZenDirectoryWorker>();
    }

    public static async Task Process(TaskWorker<ZenDirectoryWork> w, ZenDirectoryWork work)
    {
        var worker = (ZenDirectoryWorker)w;
        string? filePath = null;
        var tryCount = 0;

        if (work.Type == ZenDirectoryWork.WorkType.Save)
        {// Save
            var hash = new byte[ZenDirectory.HashSize];
            BitConverter.TryWriteBytes(hash, Arc.Crypto.FarmHash.Hash64(work.SaveData.Memory.Span));

            var path = worker.ZenDirectory.GetSnowflakePath(work.SnowflakeId);
            var directoryPath = Path.Combine(worker.ZenDirectory.RootedPath, path.Directory);

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
                    await RandomAccess.WriteAsync(handle, hash, 0, worker.CancellationToken);
                    await RandomAccess.WriteAsync(handle, work.SaveData.Memory, ZenDirectory.HashSize, worker.CancellationToken);
                }
            }
            catch (DirectoryNotFoundException)
            {
                Directory.CreateDirectory(directoryPath);
                goto TrySave;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
                goto TrySave;
            }
            finally
            {
                work.SaveData.Return();
            }
        }
        else if (work.Type == ZenDirectoryWork.WorkType.Load)
        {// Load
            try
            {
                var path = worker.ZenDirectory.GetSnowflakePath(work.SnowflakeId);
                filePath = Path.Combine(worker.ZenDirectory.RootedPath, path.Directory, path.File);
                using (var handle = File.OpenHandle(filePath, mode: FileMode.Open, access: FileAccess.Read))
                {
                    var hash = new byte[ZenDirectory.HashSize];
                    var read = await RandomAccess.ReadAsync(handle, hash, 0, worker.CancellationToken);
                    if (read != ZenDirectory.HashSize)
                    {
                        goto DeleteAndExit;
                    }

                    var memoryOwner = FlakeFragmentPool.Rent(work.LoadSize).ToMemoryOwner(0, work.LoadSize);
                    read = await RandomAccess.ReadAsync(handle, memoryOwner.Memory, ZenDirectory.HashSize, worker.CancellationToken);
                    if (read != work.LoadSize)
                    {
                        goto DeleteAndExit;
                    }

                    if (BitConverter.ToUInt64(hash) != Arc.Crypto.FarmHash.Hash64(memoryOwner.Memory.Span))
                    {
                        goto DeleteAndExit;
                    }

                    work.LoadData = memoryOwner;
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
            }
            finally
            {
            }
        }
        else if (work.Type == ZenDirectoryWork.WorkType.Delete)
        {
            try
            {
                var path = worker.ZenDirectory.GetSnowflakePath(work.SnowflakeId);
                filePath = Path.Combine(worker.ZenDirectory.RootedPath, path.Directory, path.File);
                File.Delete(filePath);
            }
            catch
            {
            }
            finally
            {
                worker.ZenDirectory.RemoveSnowflake(work.SnowflakeId);
            }
        }

        return;

DeleteAndExit:
        if (filePath != null)
        {
            File.Delete(filePath);
        }

        return;
    }

    public ZenDirectory ZenDirectory { get; }

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

    // private ILogger<ZenDirectoryWorker>? logger;
}

internal class ZenDirectoryWork : IEquatable<ZenDirectoryWork>
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

    public ZenDirectoryWork(uint snowflakeId, ByteArrayPool.ReadOnlyMemoryOwner saveData)
    {// Save
        this.Type = WorkType.Save;
        this.SnowflakeId = snowflakeId;
        this.SaveData = saveData;
    }

    public ZenDirectoryWork(uint snowflakeId, int loadSize)
    {// Load
        this.Type = WorkType.Load;
        this.SnowflakeId = snowflakeId;
        this.LoadSize = loadSize;
    }

    public ZenDirectoryWork(uint snowflakeId)
    {// Remove
        this.Type = WorkType.Delete;
        this.SnowflakeId = snowflakeId;
    }

    public override int GetHashCode()
        => HashCode.Combine(this.Type, this.SnowflakeId, this.SaveData.Memory.Length, this.LoadSize);

    public bool Equals(ZenDirectoryWork? other)
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
