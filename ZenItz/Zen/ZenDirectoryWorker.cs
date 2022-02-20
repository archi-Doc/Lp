// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenItz;

internal class ZenDirectoryWorker : TaskWorker<ZenDirectoryWork>
{
    public ZenDirectoryWorker(ThreadCoreBase parent, ZenDirectory zenDirectory)
        : base(parent, Process, true)
    {
        this.ZenDirectory = zenDirectory;
    }

    public static async Task<AbortOrComplete> Process(TaskWorker<ZenDirectoryWork> w, ZenDirectoryWork work)
    {
        var worker = (ZenDirectoryWorker)w;
        if (work.Type == ZenDirectoryWork.WorkType.Save)
        {// Save
            var hash = new byte[ZenDirectory.HashSize];
            BitConverter.TryWriteBytes(hash, Arc.Crypto.FarmHash.Hash64(work.SaveData.Memory.Span));

            try
            {
                var path = worker.ZenDirectory.GetSnowflakePath(work.SnowflakeId);
                var directoryPath = Path.Combine(worker.ZenDirectory.DirectoryPath, path.Directory);
                Directory.CreateDirectory(directoryPath);

                var filePath = Path.Combine(directoryPath, path.File);
                using (var handle = File.OpenHandle(filePath, mode: FileMode.OpenOrCreate, access: FileAccess.Write))
                {
                    await RandomAccess.WriteAsync(handle, hash, 0, worker.CancellationToken);
                    await RandomAccess.WriteAsync(handle, work.SaveData.Memory, ZenDirectory.HashSize, worker.CancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                return AbortOrComplete.Abort;
            }
            catch
            {
            }
            finally
            {
                work.SaveData.Return();
            }
        }
        else if (work.Type == ZenDirectoryWork.WorkType.Load)
        {
            try
            {
                var path = worker.ZenDirectory.GetSnowflakePath(work.SnowflakeId);
                var filePath = Path.Combine(worker.ZenDirectory.DirectoryPath, path.Directory, path.File);
                using (var handle = File.OpenHandle(filePath, mode: FileMode.Open, access: FileAccess.Read))
                {
                    var hash = new byte[ZenDirectory.HashSize];
                    var read = await RandomAccess.ReadAsync(handle, hash, 0, worker.CancellationToken);
                    if (read != ZenDirectory.HashSize)
                    {
                        return AbortOrComplete.Abort;
                    }

                    var memoryOwner = new ByteArrayPool.MemoryOwner(new byte[100]); // tempcode Zen.FragmentPool.Rent(work.LoadSize).ToMemoryOwner();
                    read = await RandomAccess.ReadAsync(handle, memoryOwner.Memory, ZenDirectory.HashSize, worker.CancellationToken);
                    if (read != work.LoadSize)
                    {
                        return AbortOrComplete.Abort;
                    }

                    if (BitConverter.ToUInt64(hash) != Arc.Crypto.FarmHash.Hash64(memoryOwner.Memory.Span))
                    {
                        return AbortOrComplete.Abort;
                    }

                    work.Result = memoryOwner;
                }
            }
            catch (OperationCanceledException)
            {
                return AbortOrComplete.Abort;
            }
            catch
            {
            }
        }

        return AbortOrComplete.Complete;
    }

    public ZenDirectory ZenDirectory { get; }
}

internal class ZenDirectoryWork : TaskWork, IEquatable<ZenDirectoryWork>
{
    public enum WorkType
    {
        Save,
        Load,
    }

    public WorkType Type { get; }

    public uint SnowflakeId { get; }

    public ByteArrayPool.ReadOnlyMemoryOwner SaveData { get; }

    public int LoadSize { get; }

    public ZenDirectoryWork(uint snowflakeId, ByteArrayPool.ReadOnlyMemoryOwner saveData)
    {// Save
        this.Type = WorkType.Save;
        this.SnowflakeId = snowflakeId;
        this.SaveData = saveData;
    }

    public ZenDirectoryWork(uint snowflakeId, int loadSize)
    {// Save
        this.Type = WorkType.Load;
        this.SnowflakeId = snowflakeId;
        this.LoadSize = loadSize;
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
