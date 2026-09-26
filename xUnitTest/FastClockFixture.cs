// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;
using Xunit;

[assembly: AssemblyFixture(typeof(xUnitTest.FastClockFixture))]

namespace xUnitTest;

/// <summary>
/// Keeps the cached clocks (e.g. <see cref="Mics.FastCorrected"/>) up to date, as NetSender does in the application.<br/>
/// Proof validation compares the signed time with <see cref="Mics.FastCorrected"/> within a margin of a few seconds,
/// so tests that sign and validate would fail once the test run takes longer than that margin.
/// </summary>
public sealed class FastClockFixture : IDisposable
{
    private readonly ManualResetEventSlim stopEvent = new();
    private readonly Thread thread;

    public FastClockFixture()
    {// A dedicated thread is not delayed when the thread pool is busy.
        this.thread = new Thread(this.Run) { IsBackground = true, Name = nameof(FastClockFixture), };
        this.thread.Start();
    }

    public void Dispose()
    {
        this.stopEvent.Set();
        this.thread.Join();
        this.stopEvent.Dispose();
    }

    private void Run()
    {
        do
        {
            Mics.UpdateFastSystem();
            Mics.UpdateFastApplication();
            Mics.UpdateFastUtcNow();
            Mics.UpdateFastFixedUtcNow();
            Mics.UpdateFastCorrected();
        }
        while (!this.stopEvent.Wait(100));
    }
}
