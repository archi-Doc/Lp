// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using Lp.T3cs;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("lp-create-credit", Alias = "lpcc")]
public class LpCreateCreditSubcommand : ISimpleCommandAsync<LpCreateCreditOptions>
{// lpcc A#Point@Identifier/Mergers: LpPublicKey#0@LpIdentifier/LpPublicKey -> A#InitialPoint@Identifier/Mergers
    public LpCreateCreditSubcommand(IUserInterfaceService userInterfaceService, ILogger<LpCreateCreditOptions> logger, LpService lpService)
    {
        this.userInterfaceService = userInterfaceService;
        this.logger = logger;
        this.lpService = lpService;
    }

    public async Task RunAsync(LpCreateCreditOptions option, string[] args)
    {
        var lpSeedKey = await this.lpService.AuthorityControl.GetLpSeedKey(this.logger);
        if (lpSeedKey is null)
        {
            return;
        }

        var r = await this.lpService.ParseAuthorityAndCredit(option.Source);
        if (!r.IsSuccess)
        {
            this.userInterfaceService.WriteLine(HashedString.FromEnum(r.Code));
            return;
        }

        var publicKey = r.SeedKey.GetSignaturePublicKey();

        // LpPublicKey#0@LpIdentifier/LpPublicKey -> A#InitialPoint@Identifier/Mergers
        var value = new Value(LpConstants.LpPublicKey, 0, LpConstants.LpCredit);
        var targetIdentity = new CreditIdentity(default, publicKey, r.Credit.Mergers);
        var targetCredit = new Credit(targetIdentity.GetIdentifier(), targetIdentity.Mergers);
        var targetValue = new Value(targetIdentity.Originator, r.Point, targetCredit);

        this.userInterfaceService.WriteLine($"{this.GetType().Name}");
        this.userInterfaceService.WriteLine($"Credit:{r.Credit}");
        this.userInterfaceService.WriteLine($"Target value:{targetValue}");

        var evolProof = new EvolProof(value, default, targetValue, targetIdentity);
        lpSeedKey.TrySign(evolProof, Mics.FromDays(1));
        var bin = TinyhandSerializer.Serialize(evolProof);
        var b = evolProof.ValidateAndVerify();

        using (var connectionAndService = await this.lpService.ConnectAndAuthenticate<IMergerClient>(r.Credit, r.SeedKey, default))
        {
            if (connectionAndService.IsFailure)
            {
                this.userInterfaceService.WriteLine(HashedString.FromEnum(connectionAndService.Result));
                return;
            }
        }
    }

    private readonly IUserInterfaceService userInterfaceService;
    private readonly ILogger logger;
    private readonly LpService lpService;
}

public record LpCreateCreditOptions
{
    [SimpleOption("Source", Description = "Authority@Identifier/Mergers", Required = true)]
    public string Source { get; init; } = string.Empty;
}
