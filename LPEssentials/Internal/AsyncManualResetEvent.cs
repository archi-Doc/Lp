// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace LP;

public class AsyncManualResetEvent
{
    private volatile TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Task => this.tcs.Task;

    public void Set() => this.tcs.TrySetResult(true);

    public void Reset()
    {
        while (true)
        {
            var tcs = this.tcs;
            if (!tcs.Task.IsCompleted ||
                Interlocked.CompareExchange(ref this.tcs, new TaskCompletionSource<bool>(), tcs) == tcs)
            {
                return;
            }
        }
    }
}
