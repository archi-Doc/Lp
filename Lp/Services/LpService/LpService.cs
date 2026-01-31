// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
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
        /// <summary>
        /// Initializes a new instance of the <see cref="ParseResult"/> struct with the specified result code.
        /// </summary>
        /// <param name="code">The result code indicating the outcome of the parsing operation.</param>
        public ParseResult(ParseResultCode code)
            : this(code, null, default, null)
        {
        }

        /// <summary>
        /// Gets a value indicating whether the parsing was successful.
        /// </summary>
        /// <remarks>
        /// Returns <c>true</c> if <see cref="Code"/> is <see cref="ParseResultCode.Success"/>, and both <see cref="SeedKey"/> and <see cref="Credit"/> are not <c>null</c>.
        /// </remarks>
        [MemberNotNullWhen(true, nameof(SeedKey), nameof(Credit))]
        public bool IsSuccess => this.Code == ParseResultCode.Success && this.SeedKey is not null && this.Credit is not null;

        /// <summary>
        /// Gets a value indicating whether the parsing failed.
        /// </summary>
        /// <remarks>
        /// Returns <c>true</c> if <see cref="Code"/> is not <see cref="ParseResultCode.Success"/>, or either <see cref="SeedKey"/> or <see cref="Credit"/> is <c>null</c>.
        /// </remarks>
        [MemberNotNullWhen(false, nameof(SeedKey), nameof(Credit))]
        public bool IsFailure => this.Code != ParseResultCode.Success || this.SeedKey is null || this.Credit is null;
    }

    #region FieldAndProperty

    public VaultControl VaultControl { get; init; }

    public AuthorityControl AuthorityControl { get; init; }

    public NetTerminal NetTerminal { get; init; }

    public Credentials Credentials { get; init; }

    private readonly IUserInterfaceService userInterfaceService;
    private readonly IConversionOptions conversionOptions;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="LpService"/> class.
    /// </summary>
    /// <param name="userInterfaceService">The user interface service.</param>
    /// <param name="netTerminal">The net terminal.</param>
    /// <param name="authorityControl">The authority control.</param>
    /// <param name="vaultControl">The vault control.</param>
    /// <param name="credentials"> The credentials.</param>
    public LpService(IUserInterfaceService userInterfaceService, NetTerminal netTerminal, AuthorityControl authorityControl, VaultControl vaultControl, Credentials credentials)
    {
        this.userInterfaceService = userInterfaceService;
        this.NetTerminal = netTerminal;
        this.AuthorityControl = authorityControl;
        this.VaultControl = vaultControl;
        this.Credentials = credentials;

        this.conversionOptions = Alias.Instance;
    }

    public Task<ConnectionAndService<TService>> ConnectAndAuthenticate<TService>(Credit credit, SeedKey seedKey, CancellationToken cancellationToken)
        where TService : INetServiceWithOwner
    {
        var credential = this.ResolveMerger(credit);
        if (credential is null)
        {
            return Task.FromResult<ConnectionAndService<TService>>(new(NetResult.NotFound));
        }

        return this.ConnectAndAuthenticate<TService>(credential, seedKey, credit, cancellationToken);
    }

    public async Task<ConnectionAndService<TService>> ConnectAndAuthenticate<TService>(CredentialEvidence credentialEvidence, SeedKey seedKey, Credit? credit, CancellationToken cancellationToken)
        where TService : INetServiceWithOwner
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new(NetResult.Canceled);
        }

        if (credentialEvidence.Proof.State.NetNode is not { } netNode)
        {
            return new(NetResult.NoNetwork);
        }

        using (var connection = await this.NetTerminal.Connect(netNode))
        {
            if (connection is null)
            {
                this.userInterfaceService.WriteLine($"Could not connect to {netNode.ToString()}");
                return new(NetResult.NoNetwork);
            }

            var service = connection.GetService<TService>();
            // var authenticationToken = AuthenticationToken.CreateAndSign(seedKey, connection);
            var ownerToken = OwnerToken.CreateAndSign(seedKey, connection, credit);
            var r = await service.Authenticate(ownerToken);
            if (r != NetResult.Success)
            {
                return new(r);
            }

            return new(connection, service);
        }
    }

    public CredentialEvidence? ResolveMerger(Credit credit)
    {
        this.Credentials.Nodes.TryGet(credit.Mergers[0], out var credentialEvidence);
        return credentialEvidence;
    }

    /// <summary>
    /// Parses a seed key and credit from the given source string.
    /// </summary>
    /// <param name="source">The source string to parse.</param>
    /// <returns>A <see cref="ParseResult"/> containing the parsed data.</returns>
    public async Task<ParseResult> ParseAuthorityAndCredit(string source)
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
            var authority = await this.AuthorityControl.GetAuthority(authorityName).ConfigureAwait(false);
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

    public async Task<SeedKey?> GetSeedKeyFromCode(string code)
    {
        SeedKey? seedKey;

        // Authority
        if (await this.AuthorityControl.GetAuthority(code).ConfigureAwait(false) is { } authority)
        {// Success
            return authority.GetSeedKey();
        }

        // Vault
        if (this.VaultControl.Root.TryGetObject<SeedKey>(code, out seedKey, out var result))
        {// Success
            return seedKey;
        }

        // Raw string
        if (SeedKey.TryParse(code, out seedKey))
        {
            return seedKey;
        }

        this.userInterfaceService.WriteLine(Hashed.Error.NoPrivateKey);
        return default;
    }
}
