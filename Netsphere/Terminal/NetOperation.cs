﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public abstract class NetOperation : IDisposable
{
    internal NetOperation(NetTerminal netTerminal)
    {
        this.Terminal = netTerminal.Terminal;
        this.NetTerminal = netTerminal;
    }

    public Terminal Terminal { get; }

    public NetTerminal NetTerminal { get; }

    private GenePool? genePool;

    public ulong GetGene()
    {
        if (this.genePool == null)
        {
            this.genePool = this.NetTerminal.TryFork();
        }

        var gp = this.genePool ?? this.NetTerminal.GenePool;
        return gp.GetSequential();
    }

    public (ulong First, ulong Second) Get2Genes()
    {
        if (this.genePool == null)
        {
            this.genePool = this.NetTerminal.TryFork();
        }

        var gp = this.genePool ?? this.NetTerminal.GenePool;
        return gp.GetSequential2();
    }

    public void GetGenes(Span<ulong> span)
    {
        if (this.genePool == null)
        {
            this.genePool = this.NetTerminal.TryFork();
        }

        var gp = this.genePool ?? this.NetTerminal.GenePool;
        gp.GetSequential(span);
    }

    public virtual async Task<NetResult> EncryptConnectionAsync()
        => NetResult.NoEncryptedConnection;

    #region IDisposable Support

    private bool disposed = false; // To detect redundant calls.

    /// <summary>
    /// Finalizes an instance of the <see cref="NetOperation"/> class.
    /// </summary>
    ~NetOperation()
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
                this.genePool?.Dispose();
                this.genePool = null;
            }

            // free native resources here if there are any.
            this.disposed = true;
        }
    }
    #endregion
}
