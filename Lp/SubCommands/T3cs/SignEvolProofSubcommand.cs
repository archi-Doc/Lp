// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using SimpleCommandLine;
using static Lp.Subcommands.KeyCommand.NewMasterKeySubcommand;

namespace Lp.Subcommands.T3cs;

[SimpleCommand("sign-evolproof")]
public class SignEvolProofSubcommand : ISimpleCommandAsync<SignOptions>
{
    private readonly IUserInterfaceService userInterfaceService;
    private readonly ILogger logger;
    private readonly LpService lpService;

    public SignEvolProofSubcommand(IUserInterfaceService userInterfaceService, ILogger<SignEvolProofSubcommand> logger, LpService lpService)
    {
        this.userInterfaceService = userInterfaceService;
        this.logger = logger;
        this.lpService = lpService;
    }

    public async Task RunAsync(SignOptions options, string[] args)
    {
        var seedKey = await this.lpService.LoadSeedKey(this.logger, options.KeyCode);
        if (seedKey is null)
        {
            return;
        }

        EvolProof? proof = default;
        try
        {
            proof = TinyhandSerializer.DeserializeFromString<EvolProof>(SimpleParserHelper.TrimQuotesAndBracket(options.Proof));
        }
        catch
        {
        }

        if (proof is null)
        {
            return;
        }

        if (!seedKey.TrySign(proof, Mics.FromDays(1)))
        {
            return;
        }

        this.userInterfaceService.WriteLine($"Proof: {proof.ToString()}");
        var st = TinyhandSerializer.SerializeToString(proof);

        /*var credit = creditIdentity.ToCredit();
        if (credit is not null)
        {
            this.userInterfaceService.WriteLine($"Credit: {credit.ToString()}");
        }*/
    }
}

public record SignOptions
{
    [SimpleOption("KeyCode", Description = "Key code (secret key, vault, authority)", Required = true)]
    public string KeyCode { get; init; } = string.Empty;

    [SimpleOption("Proof", Description = "Proof", Required = true)]
    public string Proof { get; init; } = string.Empty;
}
