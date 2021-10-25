// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Net;

public class Terminal : IDisposable
{
    internal Terminal(ulong gene, NodeAddress nodeAddress)
    {
        this.Gene = gene;
        this.NodeAddress = nodeAddress;
    }

    public ulong Gene { get; }

    public NodeAddress NodeAddress { get; }

#pragma warning disable SA1124 // Do not use regions
    #region IDisposable Support
#pragma warning restore SA1124 // Do not use regions

    private bool disposed = false; // To detect redundant calls.

    /// <summary>
    /// Finalizes an instance of the <see cref="Terminal"/> class.
    /// </summary>
    ~Terminal()
    {
        this.Dispose(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// free managed/native resources.
    /// </summary>
    /// <param name="disposing">true: free managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                // free managed resources.
            }

            // free native resources here if there are any.
            this.disposed = true;
        }
    }
    #endregion
}
