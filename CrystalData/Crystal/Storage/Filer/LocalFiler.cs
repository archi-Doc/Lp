// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1124 // Do not use regions

namespace CrystalData.Filer;

internal class LocalFiler : TaskWorker<FilerWork>, IFiler
{
    public const int DefaultConcurrentTasks = 4;

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
            filePath = worker.GetRootedPath(work);

TryWrite:
            tryCount++;
            if (tryCount > 2)
            {
                work.Result = StorageResult.WriteError;
                work.WriteData.Return();
                return;
            }

            try
            {
                using (var handle = File.OpenHandle(filePath, mode: FileMode.OpenOrCreate, access: FileAccess.Write))
                {
                    await RandomAccess.WriteAsync(handle, work.WriteData.Memory, 0, worker.CancellationToken).ConfigureAwait(false);
                    worker.logger?.TryGet()?.Log($"Written {filePath}, {work.WriteData.Memory.Length}");

                    work.Result = StorageResult.Success;
                }
            }
            catch (DirectoryNotFoundException)
            {
                if (Path.GetDirectoryName(filePath) is string directoryPath)
                {// Create directory
                    Directory.CreateDirectory(directoryPath);
                    worker.logger?.TryGet()?.Log($"CreateDirectory {directoryPath}");
                }
                else
                {
                    return;
                }

                goto TryWrite;
            }
            catch (OperationCanceledException)
            {
                work.Result = StorageResult.Aborted;
                return;
            }
            catch
            {
                worker.logger?.TryGet()?.Log($"Retry {filePath}");
                goto TryWrite;
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
                var sizeToRead = work.SizeToRead;
                if (sizeToRead < 0)
                {
                    try
                    {
                        var fileInfo = new FileInfo(filePath);
                        sizeToRead = (int)fileInfo.Length;
                    }
                    catch
                    {
                        work.Result = StorageResult.ReadError;
                        return;
                    }
                }

                using (var handle = File.OpenHandle(filePath, mode: FileMode.Open, access: FileAccess.Read))
                {
                    var memoryOwner = ByteArrayPool.Default.Rent(sizeToRead).ToMemoryOwner(0, sizeToRead);
                    var read = await RandomAccess.ReadAsync(handle, memoryOwner.Memory, 0, worker.CancellationToken).ConfigureAwait(false);
                    if (read != sizeToRead)
                    {
                        goto DeleteAndExit;
                    }

                    work.ReadData = memoryOwner;
                    worker.logger?.TryGet()?.Log($"Read {filePath}, {memoryOwner.Memory.Length}");
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
                worker.logger?.TryGet()?.Log($"Read exception {filePath}");
            }
            finally
            {
            }
        }
        else if (work.Type == FilerWork.WorkType.Delete)
        {
            try
            {
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
            worker.logger?.TryGet()?.Log($"DeleteAndExit {filePath}");
        }

        return;
    }

    bool IFiler.PrepareAndCheck(StorageControl storage)
    {
        try
        {
            if (Path.IsPathRooted(this.DirectoryPath))
            {
                this.RootedPath = this.DirectoryPath;
            }
            else
            {
                this.RootedPath = Path.Combine(this.Options.RootPath, this.DirectoryPath);
            }

            Directory.CreateDirectory(this.RootedPath);

            // Check directory file
            try
            {
                using (var handle = File.OpenHandle(this.SnowflakeFilePath, mode: FileMode.Open, access: FileAccess.ReadWrite))
                {
                }
            }
            catch
            {
                using (var handle = File.OpenHandle(this.SnowflakeBackupPath, mode: FileMode.Open, access: FileAccess.ReadWrite))
                {
                }
            }
        }
        catch
        {// No directory file
            return false;
        }

        if (storage.Options.EnableLogger)
        {
            this.logger = storage.UnitLogger.GetLogger<LocalFiler>();
        }

        return true;
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

    private ILogger? logger;
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
