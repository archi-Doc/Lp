// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.T3CS;
using Netsphere;

namespace LP.NetServices;

public class AuthorizedTerminalFactory
{
    public AuthorizedTerminalFactory(AuthorityVault authorityVault)
    {
        this.authorityVault = authorityVault;
    }

    public async Task<AuthorizedTerminal<TService>?> Create<TService>(NetTerminal terminal, NetNode node, string authorityName, ILogger? logger)
        where TService : IAuthorizedService
    {
        // Authority key
        var authority = await this.authorityVault.GetAuthority(authorityName);
        if (authority == null)
        {
            logger?.TryGet(LogLevel.Error)?.Log(Hashed.Authority.NotFound, authorityName);
            return null; // AuthorizedTerminal<TService>.Invalid;
        }

        // Try to get a cached terminal

        // Terminal
        var connection = await terminal.TryConnect(node);
        if (connection == null)
        {
            logger?.TryGet(LogLevel.Error)?.Log(Hashed.Error.Connect, node.ToString());
            return null; // AuthorizedTerminal<TService>.Invalid;
        }

        // Service & authorize
        var service = connection.GetService<TService>();
        // var token = await clientTerminal.CreateToken(Token.Type.Authorize);
        // authority.SignToken(token);
        // var response = await service.Authorize(token).ResponseAsync;

        var proof = new EngageProof(connection.Salt);
        authority.SignProof(proof, Mics.GetCorrected()); // proof.SignProof(privateKey, Mics.GetCorrected());
        var response = await service.Engage(proof).ResponseAsync;
        if (!response.IsSuccess || response.Value != NetResult.Success)
        {
            logger?.TryGet(LogLevel.Error)?.Log(Hashed.Error.Authorization);
            return null; // AuthorizedTerminal<TService>.Invalid;
        }

        return new(connection, authority, service, logger);
    }

    private AuthorityVault authorityVault;
}

public class AuthorizedTerminal<TService> : IDisposable, IEquatable<AuthorizedTerminal<TService>>
    where TService : IAuthorizedService
{
    internal AuthorizedTerminal(ClientConnection terminal, Authority authority, TService service, ILogger? logger)
    {
        this.Connection = terminal;
        this.Authority = authority;
        this.Service = service;
        this.logger = logger;
    }

    public bool Equals(AuthorizedTerminal<TService>? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.Connection == other.Connection &&
            typeof(TService) == other.Service.GetType() &&
            this.Authority == other.Authority;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.Connection, typeof(TService), this.Authority);
    }

    public ClientConnection Connection { get; private set; }

    public TService Service { get; private set; }

    public Authority Authority { get; private set; }

    private ILogger? logger;

#pragma warning disable SA1124 // Do not use regions
    #region IDisposable Support
#pragma warning restore SA1124 // Do not use regions

    private bool disposed = false; // To detect redundant calls.

    /// <summary>
    /// Finalizes an instance of the <see cref="AuthorizedTerminal{TService}"/> class.
    /// </summary>
    ~AuthorizedTerminal()
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
            this.Service = default!;
            this.Authority = default!;

            // free native resources here if there are any.
            this.disposed = true;
        }
    }

    #endregion
}
