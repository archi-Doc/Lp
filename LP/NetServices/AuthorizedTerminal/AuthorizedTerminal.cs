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

    public async Task<AuthorizedTerminal<TService>?> Create<TService>(Terminal terminal, NodeInformation nodeInformation, string authorityName, ILogger? logger)
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
        var clientTerminal = await terminal.CreateAndEncrypt(nodeInformation);
        if (clientTerminal == null)
        {
            logger?.TryGet(LogLevel.Error)?.Log(Hashed.Error.Connect, nodeInformation.ToString());
            return null; // AuthorizedTerminal<TService>.Invalid;
        }

        // Service & authorize
        var service = clientTerminal.GetService<TService>();
        var token = await clientTerminal.CreateToken(Token.Type.Authorize);
        authority.SignToken(token);
        var response = await service.Authorize(token).ResponseAsync;
        if (!response.IsSuccess || response.Value != NetResult.Success)
        {
            logger?.TryGet(LogLevel.Error)?.Log(Hashed.Error.Authorization);
            return null; // AuthorizedTerminal<TService>.Invalid;
        }

        return new(clientTerminal, authority, service, logger);
    }

    private AuthorityVault authorityVault;
}

public class AuthorizedTerminal<TService> : IDisposable, IEquatable<AuthorizedTerminal<TService>>
    where TService : IAuthorizedService
{
    internal AuthorizedTerminal(ClientTerminal terminal, Authority authority, TService service, ILogger? logger)
    {
        this.Terminal = terminal;
        this.Key = authority;
        this.Service = service;
        this.logger = logger;
    }

    public bool Equals(AuthorizedTerminal<TService>? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.Terminal == other.Terminal &&
            typeof(TService) == other.Service.GetType() &&
            this.Key == other.Key;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.Terminal, typeof(TService), this.Key);
    }

    public ClientTerminal Terminal { get; private set; }

    public TService Service { get; private set; }

    public Authority Key { get; private set; }

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
                this.Terminal?.Dispose();
            }

            this.Terminal = default!;
            this.Service = default!;
            this.Key = default!;

            // free native resources here if there are any.
            this.disposed = true;
        }
    }

    #endregion
}
