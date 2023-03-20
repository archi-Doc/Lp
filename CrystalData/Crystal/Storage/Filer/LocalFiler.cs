// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using CrystalData.Storage;

#pragma warning disable SA1124 // Do not use regions

namespace CrystalData.Filer;

internal class LocalFiler : TaskWorker<FilerWork>, IFiler
{
    public const int DefaultConcurrentTasks = 4;
    public const int HashSize = 8;

    public LocalFiler()
        : base(null, Process, true)
    {
        this.NumberOfConcurrentTasks = DefaultConcurrentTasks;
        this.SetCanStartConcurrentlyDelegate((workInterface, workingList) =>
        {// Lock IO order
            var path = workInterface.Work.Path;
            foreach (var x in workingList)
            {
                if (x.Work.Path == path)
                {
                    return false;
                }
            }

            return true;
        });

        this.rootedPath = string.Empty;
    }

    public static async Task Process(TaskWorker<FilerWork> w, FilerWork work)
    {
        var worker = (LocalFiler)w;
        string? filePath = null;
        var tryCount = 0;

        if (work.Type == FilerWork.WorkType.Write)
        {// Write
            var hash = new byte[HashSize];
            BitConverter.TryWriteBytes(hash, Arc.Crypto.FarmHash.Hash64(work.WriteData.Memory.Span));

            filePath = worker.GetRootedPath(work);

TryPut:
            tryCount++;
            if (tryCount > 2)
            {
                return;
            }

            try
            {
                using (var handle = File.OpenHandle(filePath, mode: FileMode.OpenOrCreate, access: FileAccess.Write))
                {
                    await RandomAccess.WriteAsync(handle, hash, 0, worker.CancellationToken).ConfigureAwait(false);
                    await RandomAccess.WriteAsync(handle, work.WriteData.Memory, CrystalDirectory.HashSize, worker.CancellationToken).ConfigureAwait(false);
                    // worker.CrystalDirectory.Logger?.TryGet()?.Log($"Written {filePath}, {work.SaveData.Memory.Length}");

                    work.Result = StorageResult.Success;
                }
            }
            catch (DirectoryNotFoundException)
            {
                if (Path.GetDirectoryName(filePath) is string directoryPath)
                {// Create directory
                    Directory.CreateDirectory(directoryPath);
                    // worker.CrystalDirectory.Logger?.TryGet()?.Log($"CreateDirectory {directoryPath}");
                }
                else
                {
                    return;
                }

                goto TryPut;
            }
            catch (OperationCanceledException)
            {
                work.Result = StorageResult.Aborted;
                return;
            }
            catch
            {
                // worker.CrystalDirectory.Logger?.TryGet()?.Log($"Retry {filePath}");
                goto TryPut;
            }
            finally
            {
                work.WriteData.Return();
            }
        }
        else if (work.Type == FilerWork.WorkType.Read)
        {// Read
            try
            {
                filePath = worker.GetRootedPath(work);
                using (var handle = File.OpenHandle(filePath, mode: FileMode.Open, access: FileAccess.Read))
                {
                    var hash = new byte[CrystalDirectory.HashSize];
                    var read = await RandomAccess.ReadAsync(handle, hash, 0, worker.CancellationToken).ConfigureAwait(false);
                    if (read != CrystalDirectory.HashSize)
                    {
                        // worker.CrystalDirectory.Logger?.TryGet()?.Log($"DeleteAndExit1 {filePath}");
                        goto DeleteAndExit;
                    }

                    var memoryOwner = ByteArrayPool.Default.Rent(work.SizeToRead).ToMemoryOwner(0, work.SizeToRead);
                    read = await RandomAccess.ReadAsync(handle, memoryOwner.Memory, CrystalDirectory.HashSize, worker.CancellationToken).ConfigureAwait(false);
                    if (read != work.SizeToRead)
                    {
                        // worker.CrystalDirectory.Logger?.TryGet()?.Log($"DeleteAndExit2 {filePath}");
                        goto DeleteAndExit;
                    }

                    if (BitConverter.ToUInt64(hash) != Arc.Crypto.FarmHash.Hash64(memoryOwner.Memory.Span))
                    {
                        // worker.CrystalDirectory.Logger?.TryGet()?.Log($"DeleteAndExit3 {filePath}");
                        goto DeleteAndExit;
                    }

                    work.ReadData = memoryOwner;
                    // worker.CrystalDirectory.Logger?.TryGet()?.Log($"Read {filePath}, {memoryOwner.Memory.Length}");
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
                // worker.CrystalDirectory.Logger?.TryGet()?.Log($"Read exception {filePath}");
            }
            finally
            {
            }
        }
        else if (work.Type == FilerWork.WorkType.Delete)
        {
            try
            {
                // filePath = Path.Combine(worker.CrystalDirectory.RootedPath, path.Directory, path.File);
                File.Delete(work.Path);
            }
            catch
            {
            }
            finally
            {
            }
        }

        return;

DeleteAndExit:
        if (filePath != null)
        {
            File.Delete(filePath);
            // worker.CrystalDirectory.Logger?.TryGet()?.Log($"DeleteAndExit {filePath}");
        }

        return;
    }

    internal async Task WaitForCompletionAsync()
    {
        await this.worker.WaitForCompletionAsync().ConfigureAwait(false);
    }

    internal async Task StopAsync()
    {
        await this.worker.WaitForCompletionAsync().ConfigureAwait(false);
        await this.SaveDirectoryAsync(this.SnowflakeFilePath, this.SnowflakeBackupPath).ConfigureAwait(false);
    }

    StorageResult IFiler.Write(string path, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared)
    {
        this.AddLast(new(path, dataToBeShared));
        return StorageResult.Started;
    }

    StorageResult IFiler.Delete(string path)
    {
        this.AddLast(new(path));
        return StorageResult.Started;
    }

    async Task<StorageMemoryOwnerResult> IFiler.ReadAsync(string path, int sizeToRead, TimeSpan timeToWait)
    {
        var work = new FilerWork(path, sizeToRead);
        var workInterface = this.AddLast(work);
        await workInterface.WaitForCompletionAsync(timeToWait).ConfigureAwait(false);
        return new(work.Result, work.ReadData);
    }

    async Task<StorageResult> IFiler.WriteAsync(string path, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared, TimeSpan timeToWait)
    {
        var work = new FilerWork(path, dataToBeShared);
        var workInterface = this.AddLast(work);
        await workInterface.WaitForCompletionAsync(timeToWait).ConfigureAwait(false);
        return work.Result;
    }

    async Task<StorageResult> IFiler.DeleteAsync(string path, TimeSpan timeToWait)
    {
        var work = new FilerWork(path);
        var workInterface = this.AddLast(work);
        await workInterface.WaitForCompletionAsync(timeToWait).ConfigureAwait(false);
        return work.Result;
    }

    private string GetRootedPath(FilerWork work)
    {
        return Path.Combine(this.rootedPath, work.Path);
    }

    private string rootedPath;

    #region IDisposable Support

    /// <summary>
    /// Finalizes an instance of the <see cref="LocalFiler"/> class.
    /// </summary>
    ~LocalFiler()
    {
        this.Dispose(false);
    }

    /// <summary>
    /// free managed/native resources.
    /// </summary>
    /// <param name="disposing">true: free managed resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                // free managed resources.
                this.worker.Dispose();
            }

            // free native resources here if there are any.
            this.disposed = true;
        }
    }
    #endregion
}
