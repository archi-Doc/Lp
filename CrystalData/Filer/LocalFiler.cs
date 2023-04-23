// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Results;

namespace CrystalData.Filer;

public class LocalFiler : FilerBase, IRawFiler
{
    public LocalFiler()
        : base(Process)
    {
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

    public static async Task Process(TaskWorker<FilerWork> w, FilerWork work)
    {
        var worker = (LocalFiler)w;
        var tryCount = 0;

        var filePath = Crystalizer.GetRootedFile(worker.Crystalizer, work.Path);
        work.Result = CrystalResult.Started;
        if (work.Type == FilerWork.WorkType.Write)
        {// Write
TryWrite:
            tryCount++;
            if (tryCount > 2)
            {
                work.Result = CrystalResult.FileOperationError;
                work.WriteData.Return();
                return;
            }

            try
            {
                using (var handle = File.OpenHandle(filePath, mode: FileMode.OpenOrCreate, access: FileAccess.Write))
                {
                    await RandomAccess.WriteAsync(handle, work.WriteData.Memory, work.Offset, worker.CancellationToken).ConfigureAwait(false);
                    worker.Logger?.TryGet()?.Log($"Written[{work.WriteData.Memory.Length}] {work.Path}");

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
                    try
                    {
                        Directory.CreateDirectory(directoryPath);
                    }
                    catch
                    {
                        work.Result = CrystalResult.FileOperationError;
                        return;
                    }

                    worker.Logger?.TryGet()?.Log($"CreateDirectory {directoryPath}");
                }
                else
                {
                    work.Result = CrystalResult.FileOperationError;
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
                worker.Logger?.TryGet()?.Log($"Retry {work.Path}");
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
                        work.Result = CrystalResult.FileOperationError;
                        return;
                    }
                }

                using (var handle = File.OpenHandle(filePath, mode: FileMode.Open, access: FileAccess.Read))
                {
                    var memoryOwner = ByteArrayPool.Default.Rent(lengthToRead).ToMemoryOwner(0, lengthToRead);
                    var read = await RandomAccess.ReadAsync(handle, memoryOwner.Memory, offset, worker.CancellationToken).ConfigureAwait(false);
                    if (read != lengthToRead)
                    {
                        File.Delete(filePath);
                        worker.Logger?.TryGet()?.Log($"DeleteAndExit {work.Path}");
                        work.Result = CrystalResult.FileOperationError;
                        return;
                    }

                    work.Result = CrystalResult.Success;
                    work.ReadData = memoryOwner;
                    worker.Logger?.TryGet()?.Log($"Read[{memoryOwner.Memory.Length}] {work.Path}");
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                work.Result = CrystalResult.Aborted;
                return;
            }
            catch
            {
                work.Result = CrystalResult.FileOperationError;
                worker.Logger?.TryGet()?.Log($"Read exception {work.Path}");
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
                worker.Logger?.TryGet()?.Log($"Deleted {work.Path}");
                work.Result = CrystalResult.Success;
            }
            catch
            {
                work.Result = CrystalResult.FileOperationError;
            }
            finally
            {
            }
        }
        else if (work.Type == FilerWork.WorkType.DeleteDirectory)
        {
            try
            {
                Directory.Delete(filePath, true);
                work.Result = CrystalResult.Success;
            }
            catch
            {
                work.Result = CrystalResult.FileOperationError;
            }
        }
        else if (work.Type == FilerWork.WorkType.List)
        {// List
            var list = new List<PathInformation>();
            try
            {
                string directory = string.Empty;
                string prefix = string.Empty;

                if (Directory.Exists(filePath))
                {// filePath is a directory
                    directory = filePath;
                }
                else
                {// "Directory/Prefix"
                    (directory, prefix) = PathHelper.PathToDirectoryAndFile(filePath);
                }

                var directoryInfo = new DirectoryInfo(directory);
                foreach (var x in directoryInfo.EnumerateFileSystemInfos())
                {
                    if (x is FileInfo fi)
                    {
                        if (string.IsNullOrEmpty(prefix) || fi.FullName.StartsWith(prefix))
                        {
                            list.Add(new(fi.FullName, fi.Length));
                        }
                    }
                    else if (x is DirectoryInfo di)
                    {
                        if (string.IsNullOrEmpty(prefix) || di.FullName.StartsWith(prefix))
                        {
                            list.Add(new(di.FullName));
                        }
                    }
                }
            }
            catch
            {
            }

            work.OutputObject = list;
        }
    }

    bool IRawFiler.SupportPartialWrite => true;

    public override string ToString()
        => $"LocalFiler";

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
