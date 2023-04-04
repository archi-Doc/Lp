// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1124 // Do not use regions

using CrystalData.Results;

namespace CrystalData.Filer;

public class LocalFiler : TaskWorker<FilerWork>, IRawFiler
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

    public static AddStorageResult Check(Crystalizer crystalizer, string directory)
    {
        var result = CheckPath(crystalizer, directory);
        if (!result.Success)
        {
            return AddStorageResult.WriteError;
        }

        return AddStorageResult.Success;
    }

    public override string ToString()
        => $"LocalFiler";

    #region FieldAndProperty

    private ILogger? logger;

    #endregion

    public static async Task Process(TaskWorker<FilerWork> w, FilerWork work)
    {
        var worker = (LocalFiler)w;
        var tryCount = 0;

        var filePath = work.Path;
        work.Result = CrystalResult.Started;
        if (work.Type == FilerWork.WorkType.Write)
        {// Write
            filePath = work.Path;

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
                    worker.logger?.TryGet()?.Log($"Written[{work.WriteData.Memory.Length}] {work.Path}");

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
                worker.logger?.TryGet()?.Log($"Retry {work.Path}");
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
                    worker.logger?.TryGet()?.Log($"Read[{memoryOwner.Memory.Length}] {work.Path}");
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
                worker.logger?.TryGet()?.Log($"Read exception {work.Path}");
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
            worker.logger?.TryGet()?.Log($"DeleteAndExit {work.Path}");
        }

        return;
    }

    #region IFiler

    async Task<CrystalResult> IRawFiler.PrepareAndCheck(Crystalizer crystalizer, FilerConfiguration configuration)
    {
        string? directory = null;
        try
        {
            directory = Path.GetDirectoryName(configuration.File);
        }
        catch
        {
        }

        var result = CheckPath(crystalizer, directory ?? string.Empty);
        if (!result.Success)
        {
            return CrystalResult.WriteError;
        }

        if (crystalizer.Options.EnableLogger)
        {
            this.logger = crystalizer.UnitLogger.GetLogger<LocalFiler>();
        }

        return CrystalResult.Success;
    }

    async Task IRawFiler.Terminate()
    {
        await this.WaitForCompletionAsync().ConfigureAwait(false);
        this.Dispose();
    }

    CrystalResult IRawFiler.Write(string path, long offset, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared)
    {
        this.AddLast(new(path, offset, dataToBeShared));
        return CrystalResult.Started;
    }

    CrystalResult IRawFiler.Delete(string path)
    {
        this.AddLast(new(path));
        return CrystalResult.Started;
    }

    async Task<CrystalMemoryOwnerResult> IRawFiler.ReadAsync(string path, long offset, int length, TimeSpan timeToWait)
    {
        var work = new FilerWork(path, offset, length);
        var workInterface = this.AddLast(work);
        await workInterface.WaitForCompletionAsync(timeToWait).ConfigureAwait(false);
        return new(work.Result, work.ReadData.AsReadOnly());
    }

    async Task<CrystalResult> IRawFiler.WriteAsync(string path, long offset, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared, TimeSpan timeToWait)
    {
        var work = new FilerWork(path, offset, dataToBeShared);
        var workInterface = this.AddLast(work);
        await workInterface.WaitForCompletionAsync(timeToWait).ConfigureAwait(false);
        return work.Result;
    }

    async Task<CrystalResult> IRawFiler.DeleteAsync(string path, TimeSpan timeToWait)
    {
        var work = new FilerWork(path);
        var workInterface = this.AddLast(work);
        await workInterface.WaitForCompletionAsync(timeToWait).ConfigureAwait(false);
        return work.Result;
    }

    #endregion

    private static (bool Success, string RootedPath) CheckPath(Crystalizer crystalizer, string file)
    {
        string rootedPath = string.Empty;
        try
        {
            if (Path.IsPathRooted(file))
            {
                rootedPath = file;
            }
            else
            {
                rootedPath = Path.Combine(crystalizer.Options.RootPath, file);
            }

            Directory.CreateDirectory(rootedPath);
            return (true, rootedPath);
        }
        catch
        {
        }

        return (false, rootedPath);
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
