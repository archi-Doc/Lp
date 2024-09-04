// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.NetServices;

public class AuthenticatedConnectionFactory
{
    public AuthenticatedConnectionFactory(AuthorityVault authorityVault)
    {
        this.authorityVault = authorityVault;
    }

    public async Task<AuthenticatedConnection?> Create(NetTerminal terminal, NetNode node, string authorityName, ILogger? logger)
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
        var token = new AuthenticationToken(connection.Salt);
        authority.Sign(token);
        if (!context.AuthenticationTokenEquals(token.PublicKey))
        {
            var result = await connection.SetAuthenticationToken(token).ConfigureAwait(false);
            if (result != NetResult.Success)
            {
                logger?.TryGet(LogLevel.Error)?.Log(Hashed.Error.Authorization);
                return null;
            }
        }

        return new(connection, context, authority, logger);
    }

    private AuthorityVault authorityVault;
}

public class AuthenticatedConnection : IDisposable, IEquatable<AuthenticatedConnection>
{
    internal AuthenticatedConnection(ClientConnection terminal, ClientConnectionContext context, Authority authority, ILogger? logger)
    {
        this.Connection = terminal;
        this.Context = context;
        this.Authority = authority;
        this.logger = logger;
    }

    public bool Equals(AuthenticatedConnection? other)
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

    public ClientConnectionContext Context { get; private set; }

    public Authority Authority { get; private set; }

    private ILogger? logger;

#pragma warning disable SA1124 // Do not use regions
    #region IDisposable Support
#pragma warning restore SA1124 // Do not use regions

    private bool disposed = false; // To detect redundant calls.

    /// <summary>
    /// Finalizes an instance of the <see cref="AuthenticatedConnection"/> class.
    /// </summary>
    ~AuthenticatedConnection()
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
