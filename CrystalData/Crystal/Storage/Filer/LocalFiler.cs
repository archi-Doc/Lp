// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1124 // Do not use regions

namespace CrystalData.Filer;

[TinyhandObject(ExplicitKeyOnly = true)]
internal partial class LocalFiler : TaskWorker<FilerWork>, IFiler
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
    }

    public LocalFiler(string path)
        : this()
    {
        this.path = path;
    }

    public override string ToString()
        => $"LocalFiler Path: {this.rootedPath}";

    #region FieldAndProperty

    public string FilerPath => this.rootedPath;

    private ILogger? logger;

    [Key(0)]
    private string path = string.Empty;

    private string rootedPath = string.Empty;

    #endregion

    public static async Task Process(TaskWorker<FilerWork> w, FilerWork work)
    {
        var worker = (LocalFiler)w;
        string? filePath = null;
        var tryCount = 0;

        work.Result = CrystalResult.Started;
        if (work.Type == FilerWork.WorkType.Write)
        {// Write
            filePath = worker.GetRootedPath(work);

TryWrite:
            tryCount++;
            if (tryCount > 2)
            {
                work.Result = CrystalResult.WriteError;
                work.WriteData.Return();
                return;
            }

            try
            {
                using (var handle = File.OpenHandle(filePath, mode: FileMode.OpenOrCreate, access: FileAccess.Write))
                {
                    await RandomAccess.WriteAsync(handle, work.WriteData.Memory, work.Offset, worker.CancellationToken).ConfigureAwait(false);
                    worker.logger?.TryGet()?.Log($"Written {filePath}, {work.WriteData.Memory.Length}");

                    try
                    {
                        if (RandomAccess.GetLength(handle) > work.WriteData.Memory.Length)
                        {
                            RandomAccess.SetLength(handle, work.WriteData.Memory.Length);
                        }
                    }
                    catch
                    {
                    }

                    work.Result = CrystalResult.Success;
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
                    work.Result = CrystalResult.WriteError;
                    return;
                }

                goto TryWrite;
            }
            catch (OperationCanceledException)
            {
                work.Result = CrystalResult.Aborted;
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
                var offset = work.Offset;
                var lengthToRead = work.Length;
                if (lengthToRead < 0)
                {
                    try
                    {
                        var fileInfo = new FileInfo(filePath);
                        lengthToRead = (int)fileInfo.Length;
                        offset = 0;
                    }
                    catch
                    {
                        work.Result = CrystalResult.ReadError;
                        return;
                    }
                }

                using (var handle = File.OpenHandle(filePath, mode: FileMode.Open, access: FileAccess.Read))
                {
                    var memoryOwner = ByteArrayPool.Default.Rent(lengthToRead).ToMemoryOwner(0, lengthToRead);
                    var read = await RandomAccess.ReadAsync(handle, memoryOwner.Memory, offset, worker.CancellationToken).ConfigureAwait(false);
                    if (read != lengthToRead)
                    {
                        work.Result = CrystalResult.ReadError;
                        goto DeleteAndExit;
                    }

                    work.Result = CrystalResult.Success;
                    work.ReadData = memoryOwner;
                    worker.logger?.TryGet()?.Log($"Read {filePath}, {memoryOwner.Memory.Length}");
                }
            }
            catch (OperationCanceledException)
            {
                work.Result = CrystalResult.Aborted;
                return;
            }
            catch
            {
                work.Result = CrystalResult.ReadError;
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
                work.Result = CrystalResult.Success;
            }
            catch
            {
                work.Result = CrystalResult.DeleteError;
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

    #region IFiler

    async Task<CrystalResult> IFiler.PrepareAndCheck(StorageControl storage)
    {
        try
        {
            if (Path.IsPathRooted(this.path))
            {
                this.rootedPath = this.path;
            }
            else
            {
                this.rootedPath = Path.Combine(storage.Options.RootPath, this.path);
            }

            Directory.CreateDirectory(this.rootedPath);
        }
        catch
        {
            return CrystalResult.WriteError;
        }

        if (storage.Options.EnableLogger)
        {
            this.logger = storage.UnitLogger.GetLogger<LocalFiler>();
        }

        return CrystalResult.Success;
    }

    async Task IFiler.Terminate()
    {
        await this.WaitForCompletionAsync().ConfigureAwait(false);
        this.Dispose();
    }

    async Task<CrystalResult> IFiler.DeleteAllAsync()
    {
        return PathHelper.TryDeleteDirectory(this.rootedPath) ? CrystalResult.Success : CrystalResult.DeleteError;
    }

    CrystalResult IFiler.Write(string path, long offset, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared)
    {
        this.AddLast(new(path, offset, dataToBeShared));
        return CrystalResult.Started;
    }

    CrystalResult IFiler.Delete(string path)
    {
        this.AddLast(new(path));
        return CrystalResult.Started;
    }

    async Task<CrystalMemoryOwnerResult> IFiler.ReadAsync(string path, long offset, int length, TimeSpan timeToWait)
    {
        var work = new FilerWork(path, offset, length);
        var workInterface = this.AddLast(work);
        await workInterface.WaitForCompletionAsync(timeToWait).ConfigureAwait(false);
        return new(work.Result, work.ReadData.AsReadOnly());
    }

    async Task<CrystalResult> IFiler.WriteAsync(string path, long offset, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared, TimeSpan timeToWait)
    {
        var work = new FilerWork(path, offset, dataToBeShared);
        var workInterface = this.AddLast(work);
        await workInterface.WaitForCompletionAsync(timeToWait).ConfigureAwait(false);
        return work.Result;
    }

    async Task<CrystalResult> IFiler.DeleteAsync(string path, TimeSpan timeToWait)
    {
        var work = new FilerWork(path);
        var workInterface = this.AddLast(work);
        await workInterface.WaitForCompletionAsync(timeToWait).ConfigureAwait(false);
        return work.Result;
    }

    #endregion

    private string GetRootedPath(FilerWork work)
    {
        return Path.Combine(this.rootedPath, work.Path);
    }

    #region IDisposable Support

    /*/// <summary>
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
                this.Dispose();
            }

            // free native resources here if there are any.
            this.disposed = true;
        }
    }*/
    #endregion
}
