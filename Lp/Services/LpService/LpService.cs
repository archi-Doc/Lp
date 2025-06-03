// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Arc;
using Lp.Services;
using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp;

/// <summary>
/// Represents the LpService class.
/// </summary>
public class LpService
{
    public enum ParseResultCode
    {
        Success,
        InvalidFormat,
        InvalidSeedKey,
        InvalidCredit,
        InvalidAuthority,
    }

    /// <summary>
    /// Represents the result of parsing a seed key and credit.
    /// </summary>
    public readonly record struct ParseResult(ParseResultCode Code, SeedKey? SeedKey, Point Point, Credit? Credit)
    {
        public ParseResult(ParseResultCode code)
            : this(code, null, default, null)
        {
        }

        /// <summary>
        /// Gets a value indicating whether the parsing was successful.
        /// </summary>
        [MemberNotNullWhen(true, nameof(SeedKey), nameof(Credit))]
        public bool IsSuccess => this.Code == ParseResultCode.Success && this.SeedKey is not null && this.Credit is not null;

        // public bool IsFailure => !this.IsSuccess;
    }

    #region FieldAndProperty

    private readonly IUserInterfaceService userInterfaceService;
    private readonly AuthorityControl authorityControl;
    private readonly VaultControl vaultControl;
    private readonly Credentials credentials;
    private readonly IConversionOptions conversionOptions;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="LpService"/> class.
    /// </summary>
    /// <param name="userInterfaceService">The user interface service.</param>
    /// <param name="authorityControl">The authority control.</param>
    /// <param name="vaultControl">The vault control.</param>
    /// <param name="credentials"> The credentials.</param>
    public LpService(IUserInterfaceService userInterfaceService, AuthorityControl authorityControl, VaultControl vaultControl, Credentials credentials)
    {
        this.userInterfaceService = userInterfaceService;
        this.authorityControl = authorityControl;
        this.vaultControl = vaultControl;
        this.credentials = credentials;
        this.conversionOptions = Alias.Instance;
    }

    public async Task<ConnectionAndService<TService>> ConnectAndAuthenticate<TService>(CredentialEvidence credentialEvidence, SeedKey seedKey, CancellationToken cancellationToken)
        where TService : INetServiceAuthentication
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return default;
        }

        if (credentialEvidence.Proof.State.NetNode is not { } netNode)
        {
            return default;
        }

        using (var connection = await this.netTerminal.Connect(netNode))
        {
            if (connection is null)
            {
                this.userInterfaceService.WriteLine($"Could not connect to {netNode.ToString()}");
                return default;
            }

            var service = connection.GetService<TService>();
            var auth = AuthenticationToken.CreateAndSign(seedKey, connection);
            var r = await service.Authenticate(auth);
        }
    }

    public CredentialEvidence? ResolveMerger(Credit credit)
    {
        this.credentials.Nodes.TryGet(credit.Mergers[0], out var credentialEvidence);
        return credentialEvidence;
    }

    /// <summary>
    /// Parses a seed key and credit from the given source string.
    /// </summary>
    /// <param name="source">The source string to parse.</param>
    /// <returns>A <see cref="ParseResult"/> containing the parsed data.</returns>
    public async Task<ParseResult> ParseSeedKeyAndCredit(string source)
    {
        int read = default;
        SeedKey? seedKey;
        Point point = default;
        Credit? credit;

        var memory = source.AsMemory();
        if (memory.Length > 3 && memory.Span.StartsWith(SeedKeyHelper.PrivateKeyBracket))
        {// SeedKey@Identifier/Mergers
            if (!SeedKey.TryParse(memory.Span, out seedKey, out read, this.conversionOptions))
            {
                return new(ParseResultCode.InvalidSeedKey);
            }

            memory = memory.Slice(read);
            TryParsePoint();

            if (!Credit.TryParse(memory.Span, out credit, out read, this.conversionOptions))
            {
                return new(ParseResultCode.InvalidCredit);
            }
        }
        else if (memory.Length > 0 && memory.Span[0] != SeedKeyHelper.PublicKeyOpenBracket)
        {// Authority@Ideitifier/Mergers
            var index = memory.Span.IndexOfAny(LpConstants.PointSymbol, LpConstants.CreditSymbol);
            if (index < 0)
            {
                return new(ParseResultCode.InvalidAuthority);
            }

            var authorityName = memory.Slice(0, index).ToString();
            var authority = await this.authorityControl.GetAuthority(authorityName).ConfigureAwait(false);
            if (authority is null)
            {
                return new(ParseResultCode.InvalidAuthority);
            }

            memory = memory.Slice(index);
            TryParsePoint();

            if (!Credit.TryParse(memory.Span, out credit, out read, this.conversionOptions))
            {
                return new(ParseResultCode.InvalidCredit);
            }

            seedKey = authority.GetSeedKey(credit);
        }
        else
        {
            return new(ParseResultCode.InvalidFormat);
        }

        return new(ParseResultCode.Success, seedKey, point, credit);

        void TryParsePoint()
        {
            var pointIndex = memory.Span.IndexOf(LpConstants.PointSymbol);
            if (pointIndex >= 0)
            {
                pointIndex++;
                var creditIndex = memory.Span.Slice(pointIndex).IndexOf(LpConstants.CreditSymbol);
                if (creditIndex >= 0)
                {
                    Point.TryParse(memory.Span.Slice(pointIndex, creditIndex), out point);
                    memory = memory.Slice(pointIndex + creditIndex);
                }
            }
        }
    }

    /// <summary>
    /// Loads a seed key based on the given code.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="code">The code to load the seed key for.</param>
    /// <returns>A <see cref="SeedKey"/> if found; otherwise, null.</returns>
    public async Task<SeedKey?> LoadSeedKey(ILogger? logger, string code)
    {
        SeedKey? seedKey;

        // Authority
        if (await this.authorityControl.GetAuthority(code).ConfigureAwait(false) is { } auth)
        {// Success
            return auth.GetSeedKey();
        }

        // Vault
        if (this.vaultControl.Root.TryGetObject<SeedKey>(code, out seedKey, out var result))
        {// Success
            return seedKey;
        }

        // Raw string
        if (SeedKey.TryParse(code, out seedKey))
        {
            return seedKey;
        }

        logger?.TryGet(LogLevel.Error)?.Log(Hashed.Error.NoPrivateKey);
        return default;
    }
}
