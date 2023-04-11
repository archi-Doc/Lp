// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using Amazon.S3;
using Amazon.S3.Model;
using CrystalData.Results;
using static CrystalData.Filer.FilerWork;

#pragma warning disable SA1124 // Do not use regions

namespace CrystalData.Filer;

public class S3Filer : TaskWorker<FilerWork>, IRawFiler
{// Vault: S3Bucket/BucketName "AccessKeyId=SecretAccessKey"
    private const int DefaultConcurrentTasks = 4;
    private const string WriteTestFile = "Write.test";

    public S3Filer()
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

    public S3Filer(string bucket)
        : this()
    {
        this.bucket = bucket;
    }

    public static AddStorageResult Check(StorageGroup storageGroup, string bucket, string path)
    {
        if (!storageGroup.StorageKey.TryGetKey(bucket, out var accessKeyPair))
        {
            return AddStorageResult.NoStorageKey;
        }

        return AddStorageResult.Success;
    }

    public override string ToString()
        => $"S3Filer Bucket: {this.bucket}";

    #region FieldAndProperty

    private ILogger? logger;

    private string bucket = string.Empty;

    private string path = string.Empty;

    private AmazonS3Client? client;

    private ConcurrentDictionary<string, int> checkedPath = new();

    #endregion

    public static async Task Process(TaskWorker<FilerWork> w, FilerWork work)
    {
        var worker = (S3Filer)w;
        if (worker.client == null)
        {
            work.Result = CrystalResult.NoFiler;
            return;
        }

        var tryCount = 0;
        work.Result = CrystalResult.Started;
        var filePath = work.Path;
        if (work.Type == FilerWork.WorkType.Write)
        {// Write
TryWrite:
            tryCount++;
            if (tryCount > 1)
            {
                work.Result = CrystalResult.WriteError;
                work.WriteData.Return();
                return;
            }

            try
            {
                using (var ms = new ReadOnlyMemoryStream(work.WriteData.Memory))
                {
                    var request = new Amazon.S3.Model.PutObjectRequest() { BucketName = worker.bucket, Key = filePath, InputStream = ms, };
                    var response = await worker.client.PutObjectAsync(request, worker.CancellationToken).ConfigureAwait(false);
                    if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                    {
                        worker.logger?.TryGet()?.Log($"Written {filePath}, {work.WriteData.Memory.Length}");
                        work.Result = CrystalResult.Success;
                        return;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                work.Result = CrystalResult.Aborted;
                return;
            }
            catch
            {
            }
            finally
            {
                work.WriteData.Return();
            }

            // Retry
            worker.logger?.TryGet()?.Log($"Retry {filePath}");
            goto TryWrite;
        }
        else if (work.Type == FilerWork.WorkType.Read)
        {// Read
            try
            {
                var request = new Amazon.S3.Model.GetObjectRequest() { BucketName = worker.bucket, Key = filePath, };
                if (work.Length > 0)
                {
                    request.ByteRange = new(work.Offset, work.Length);
                }

                var response = await worker.client.GetObjectAsync(request, worker.CancellationToken).ConfigureAwait(false);
                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK ||
                    response.HttpStatusCode == System.Net.HttpStatusCode.PartialContent)
                {
                    using (var ms = new MemoryStream())
                    {
                        response.ResponseStream.CopyTo(ms);
                        work.Result = CrystalResult.Success;
                        work.ReadData = new(ms.ToArray());
                        worker.logger?.TryGet()?.Log($"Read {filePath}, {work.ReadData.Memory.Length}");
                        return;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                work.Result = CrystalResult.Aborted;
                return;
            }
            catch
            {
            }
            finally
            {
            }

            work.Result = CrystalResult.ReadError;
            worker.logger?.TryGet()?.Log($"Read exception {filePath}");
        }
        else if (work.Type == FilerWork.WorkType.Delete)
        {// Delete
            try
            {
                var request = new Amazon.S3.Model.DeleteObjectRequest() { BucketName = worker.bucket, Key = filePath, };
                var response = await worker.client.DeleteObjectAsync(request, worker.CancellationToken).ConfigureAwait(false);
                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    work.Result = CrystalResult.Success;
                }
            }
            catch
            {
            }

            work.Result = CrystalResult.DeleteError;
        }
        else if (work.Type == WorkType.DeleteDirectory)
        {
            if (!filePath.EndsWith(PathHelper.Slash))
            {
                filePath += PathHelper.Slash;
            }

            while (true)
            {
                var listRequest = new ListObjectsV2Request() { BucketName = worker.bucket, Prefix = filePath, };
                var listResponse = await worker.client.ListObjectsV2Async(listRequest).ConfigureAwait(false);
                if (listResponse.HttpStatusCode != System.Net.HttpStatusCode.OK)
                {
                    work.Result = CrystalResult.DeleteError;
                    return;
                }

                if (listResponse.KeyCount == 0)
                {// No file left
                    work.Result = CrystalResult.Success;
                    return;
                }

                var deleteRequest = new DeleteObjectsRequest() { BucketName = worker.bucket, };
                foreach (var x in listResponse.S3Objects)
                {
                    deleteRequest.AddKey(x.Key);
                }

                var deleteResponse = await worker.client.DeleteObjectsAsync(deleteRequest).ConfigureAwait(false);
                if (deleteResponse.HttpStatusCode != System.Net.HttpStatusCode.OK)
                {
                    work.Result = CrystalResult.DeleteError;
                    return;
                }
            }
        }
        else if (work.Type == FilerWork.WorkType.List)
        {// List
            var list = new List<PathInformation>();
            if (!filePath.EndsWith(PathHelper.Slash))
            {
                filePath += PathHelper.Slash;
            }

            try
            {
                string? continuationToken = null;
RepeatList:
                var request = new Amazon.S3.Model.ListObjectsV2Request() { BucketName = worker.bucket, Prefix = filePath, Delimiter = PathHelper.SlashString, ContinuationToken = continuationToken, };

                var response = await worker.client.ListObjectsV2Async(request, worker.CancellationToken).ConfigureAwait(false);
                foreach (var x in response.S3Objects)
                {
                    list.Add(new(x.Key, x.Size));
                }

                foreach (var x in response.CommonPrefixes)
                {
                    list.Add(new(x));
                }

                if (response.IsTruncated)
                {
                    continuationToken = response.NextContinuationToken;
                    goto RepeatList;
                }
            }
            catch
            {
            }

            work.OutputObject = list;
        }

        return;
    }

    #region IFiler

    bool IRawFiler.SupportPartialWrite => false;

    async Task<CrystalResult> IRawFiler.PrepareAndCheck(Crystalizer crystalizer, PathConfiguration configuration)
    {
        if (!crystalizer.StorageKey.TryGetKey(this.bucket, out var accessKeyPair))
        {
            return CrystalResult.NoStorageKey;
        }

        if (this.client == null)
        {
            try
            {
                this.client = new AmazonS3Client(accessKeyPair.AccessKeyId, accessKeyPair.SecretAccessKey);
            }
            catch
            {
                return CrystalResult.NoStorageKey;
            }
        }

        // Write test.
        if (this.checkedPath.TryAdd(configuration.Path, 0))
        {
            try
            {
                var path = PathHelper.CombineWithSlash(configuration.Path, WriteTestFile);
                using (var ms = new MemoryStream())
                {
                    var request = new Amazon.S3.Model.PutObjectRequest() { BucketName = this.bucket, Key = path, InputStream = ms, };
                    var response = await this.client.PutObjectAsync(request).ConfigureAwait(false);
                    if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
                    {
                        return CrystalResult.WriteError;
                    }
                }
            }
            catch
            {
                return CrystalResult.WriteError;
            }
        }

        if (crystalizer.EnableLogger)
        {
            this.logger = crystalizer.UnitLogger.GetLogger<S3Filer>();
        }

        return CrystalResult.Success;
    }

    async Task IRawFiler.Terminate()
    {
        await this.WaitForCompletionAsync().ConfigureAwait(false);
        this.client?.Dispose();
        this.Dispose();
    }

    CrystalResult IRawFiler.Write(string path, long offset, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared, bool truncate)
    {
        if (offset != 0 || !truncate)
        {// Not supported
            return CrystalResult.NoPartialWriteSupport;
        }

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
        if (offset != 0 || !truncate)
        {// Not supported
            return CrystalResult.NoPartialWriteSupport;
        }

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

    async Task<CrystalResult> IRawFiler.DeleteDirectoryAsync(string path, TimeSpan timeToWait)
    {
        var work = new FilerWork(WorkType.DeleteDirectory, path);
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
}
