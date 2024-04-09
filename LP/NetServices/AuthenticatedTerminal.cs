// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.T3CS;
using Netsphere.Crypto;

namespace LP.NetServices;

public class AuthenticatedTerminalFactory
{
    public AuthenticatedTerminalFactory(AuthorityVault authorityVault)
    {
        this.authorityVault = authorityVault;
    }

    public async Task<AuthenticatedTerminal?> Create(NetTerminal terminal, NetNode node, string authorityName, ILogger? logger)
    {
        // Authority key
        var authority = await this.authorityVault.GetAuthority(authorityName);
        if (authority == null)
        {
            logger?.TryGet(LogLevel.Error)?.Log(Hashed.Authority.NotFound, authorityName);
            return null; // AuthorizedTerminal<TService>.Invalid;
        }

        // Connect
        var connection = await terminal.Connect(node);
        if (connection == null)
        {
            logger?.TryGet(LogLevel.Error)?.Log(Hashed.Error.Connect, node.ToString());
            return null; // AuthorizedTerminal<TService>.Invalid;
        }

        var context = connection.GetContext();
        if (!context.IsAuthenticated)
        {
            var token = new AuthenticationToken(connection.Salt);
            authority.SignToken(token);
            // authority.SignProof(proof, Mics.GetCorrected()); // proof.SignProof(privateKey, Mics.GetCorrected());
            var result = await connection.Authenticate(token).ConfigureAwait(false);
            if (result != NetResult.Success)
            {
                logger?.TryGet(LogLevel.Error)?.Log(Hashed.Error.Authorization);
                return null; // AuthorizedTerminal<TService>.Invalid;
            }

            context.AuthenticationToken = token;
        }

        return new(connection, authority, logger);
    }

    private AuthorityVault authorityVault;
}

public class AuthenticatedTerminal : IDisposable, IEquatable<AuthenticatedTerminal>
{
    internal AuthenticatedTerminal(ClientConnection terminal, Authority authority, ILogger? logger)
    {
        this.Connection = terminal;
        this.Authority = authority;
        this.logger = logger;
    }

    public bool Equals(AuthenticatedTerminal? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.Connection == other.Connection &&
            this.Authority == other.Authority;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.Connection, this.Authority);
    }

    public ClientConnection Connection { get; private set; }

    public Authority Authority { get; private set; }

    private ILogger? logger;

#pragma warning disable SA1124 // Do not use regions
    #region IDisposable Support
#pragma warning restore SA1124 // Do not use regions

    private bool disposed = false; // To detect redundant calls.

    /// <summary>
    /// Finalizes an instance of the <see cref="AuthenticatedTerminal"/> class.
    /// </summary>
    ~AuthenticatedTerminal()
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
                this.Connection?.Dispose();
            }

            this.Connection = default!;
            this.Authority = default!;

            // free native resources here if there are any.
            this.disposed = true;
        }
    }

    #endregion
}
