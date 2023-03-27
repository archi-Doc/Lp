// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Amazon.S3;
using Amazon.S3.Model;
using CrystalData.Results;

#pragma warning disable SA1124 // Do not use regions

namespace CrystalData.Filer;

[TinyhandObject(ExplicitKeyOnly = true)]
public partial class S3Filer : TaskWorker<FilerWork>, IFiler
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

    public S3Filer(string bucket, string path)
        : this()
    {
        this.bucket = bucket;
        this.path = path.TrimEnd('/');
    }

    public static AddStorageResult Check(StorageControl storageControl, string bucket, string path)
    {
        if (!storageControl.Key.TryGetKey(bucket, out var accessKeyPair))
        {
            return AddStorageResult.NoStorageKey;
        }

        return AddStorageResult.Success;
    }

    public override string ToString()
        => $"S3Filer Bucket: {this.bucket}, Path: {this.path}";

    #region FieldAndProperty

    public string FilerPath => $"{this.bucket}/{this.path}";

    private ILogger? logger;

    [Key(0)]
    private string bucket = string.Empty;

    [Key(1)]
    private string path = string.Empty;

    private AmazonS3Client? client;

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
        var filePath = worker.GetPath(work.Path);
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
        {
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

        return;
    }

    #region IFiler

    async Task<CrystalResult> IFiler.PrepareAndCheck(StorageControl storage, bool newStorage)
    {
        this.client?.Dispose();
        if (!storage.Key.TryGetKey(this.bucket, out var accessKeyPair))
        {
            return CrystalResult.NoStorageKey;
        }

        try
        {
            this.client = new AmazonS3Client(accessKeyPair.AccessKeyId, accessKeyPair.SecretAccessKey);
        }
        catch
        {
            return CrystalResult.NoStorageKey;
        }

        // Write test
        using (var ms = new MemoryStream())
        {
            var request = new Amazon.S3.Model.PutObjectRequest() { BucketName = this.bucket, Key = this.GetPath(WriteTestFile), InputStream = ms, };
            var response = await this.client.PutObjectAsync(request).ConfigureAwait(false);
            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                return CrystalResult.WriteError;
            }
        }

        if (storage.Options.EnableLogger)
        {
            this.logger = storage.UnitLogger.GetLogger<S3Filer>();
        }

        return CrystalResult.Success;
    }

    async Task IFiler.Terminate()
    {
        await this.WaitForCompletionAsync().ConfigureAwait(false);
        this.client?.Dispose();
        this.Dispose();
    }

    async Task<CrystalResult> IFiler.DeleteAllAsync()
    {
        if (this.client == null)
        {
            return CrystalResult.NoFiler;
        }

        while (true)
        {
            var listRequest = new ListObjectsV2Request() { BucketName = this.bucket, MaxKeys = 1000, Prefix = this.path, };
            var listResponse = await this.client.ListObjectsV2Async(listRequest).ConfigureAwait(false);
            if (listResponse.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                return CrystalResult.DeleteError;
            }

            if (listResponse.KeyCount == 0)
            {// No file left
                return CrystalResult.Success;
            }

            var deleteRequest = new DeleteObjectsRequest() { BucketName = this.bucket, };
            foreach (var x in listResponse.S3Objects)
            {
                deleteRequest.AddKey(x.Key);
            }

            var deleteResponse = await this.client.DeleteObjectsAsync(deleteRequest).ConfigureAwait(false);
            if (deleteResponse.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                return CrystalResult.DeleteError;
            }
        }
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
        if (offset != 0)
        {// Not supported
            return CrystalResult.NoOffsetSupport;
        }

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

    private string GetPath(string file)
    {
        if (string.IsNullOrEmpty(this.path))
        {
            return file;
        }
        else
        {
            return $"{this.path}/{file}";
        }
    }
}
