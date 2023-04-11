// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1124 // Do not use regions

using System.IO;
using CrystalData.Results;
using static CrystalData.Filer.FilerWork;

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

    private Crystalizer? crystalizer;
    private ILogger? logger;

    #endregion

    public static async Task Process(TaskWorker<FilerWork> w, FilerWork work)
    {
        var worker = (LocalFiler)w;
        var tryCount = 0;

        var filePath = Crystalizer.GetRootedFile(worker.crystalizer, work.Path);
        work.Result = CrystalResult.Started;
        if (work.Type == FilerWork.WorkType.Write)
        {// Write
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

                    if (work.Truncate)
                    {
                        try
                        {
                            var newSize = work.Offset + work.WriteData.Memory.Length;
                            if (RandomAccess.GetLength(handle) > newSize)
                            {
                                RandomAccess.SetLength(handle, newSize);
                            }
                        }
                        catch
                        {
                        }
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
        {// Delete
            try
            {
                File.Delete(filePath);
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
        else if (work.Type == FilerWork.WorkType.List)
        {// List
            var list = new List<PathInformation>();
            try
            {
                var directoryInfo = new DirectoryInfo(filePath);
                foreach (var x in directoryInfo.EnumerateFileSystemInfos())
                {
                    if (x is FileInfo fi)
                    {
                        list.Add(new(fi.FullName, fi.Length));
                    }
                    else if (x is DirectoryInfo di)
                    {
                        list.Add(new(di.FullName));
                    }
                }

                /*foreach (var x in Directory.EnumerateFiles(filePath, "*", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        var info = new System.IO.FileInfo(x);
                        list.Add(new(x, info.Length));
                    }
                    catch
                    {
                    }
                }*/
            }
            catch
            {
            }

            work.OutputObject = list;
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

    bool IRawFiler.SupportPartialWrite => true;

    async Task<CrystalResult> IRawFiler.PrepareAndCheck(Crystalizer crystalizer, PathConfiguration configuration)
    {
        this.crystalizer = crystalizer;

        if (crystalizer.EnableLogger)
        {
            this.logger ??= crystalizer.UnitLogger.GetLogger<LocalFiler>();
        }

        return CrystalResult.Success;
    }

    async Task IRawFiler.Terminate()
    {
        await this.WaitForCompletionAsync().ConfigureAwait(false);
        this.Dispose();
    }

    CrystalResult IRawFiler.Write(string path, long offset, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared, bool truncate)
    {
        this.AddLast(new(path, offset, dataToBeShared, truncate));
        return CrystalResult.Started;
    }

    CrystalResult IRawFiler.Delete(string path)
    {
        this.AddLast(new(WorkType.Delete, path));
        return CrystalResult.Started;
    }

    async Task<CrystalMemoryOwnerResult> IRawFiler.ReadAsync(string path, long offset, int length, TimeSpan timeToWait)
    {
        var work = new FilerWork(path, offset, length);
        var workInterface = this.AddLast(work);
        await workInterface.WaitForCompletionAsync(timeToWait).ConfigureAwait(false);
        return new(work.Result, work.ReadData.AsReadOnly());
    }

    async Task<CrystalResult> IRawFiler.WriteAsync(string path, long offset, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared, TimeSpan timeToWait, bool truncate)
    {
        var work = new FilerWork(path, offset, dataToBeShared, truncate);
        var workInterface = this.AddLast(work);
        await workInterface.WaitForCompletionAsync(timeToWait).ConfigureAwait(false);
        return work.Result;
    }

    async Task<CrystalResult> IRawFiler.DeleteAsync(string path, TimeSpan timeToWait)
    {
        var work = new FilerWork(WorkType.Delete, path);
        var workInterface = this.AddLast(work);
        await workInterface.WaitForCompletionAsync(timeToWait).ConfigureAwait(false);
        return work.Result;
    }

    async Task<List<PathInformation>> IRawFiler.ListAsync(string path, TimeSpan timeToWait)
    {
        var work = new FilerWork(WorkType.List, path);
        var workInterface = this.AddLast(work);
        await workInterface.WaitForCompletionAsync(timeToWait).ConfigureAwait(false);
        if (work.OutputObject is List<PathInformation> list)
        {
            return list;
        }
        else
        {
            return new List<PathInformation>();
        }
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
                rootedPath = Path.Combine(crystalizer.RootDirectory, file);
            }

            Directory.CreateDirectory(rootedPath);
            return (true, rootedPath);
        }
        catch
        {
        }

        return (false, rootedPath);
    }
}
