// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using Lp.T3cs;
using Netsphere.Crypto;
using SimpleCommandLine;
using SimplePrompt;

namespace Lp.Subcommands.OperateCredit;

public class NestedCommand : NestedCommand<NestedCommand>
{
    public static void Configure(IUnitConfigurationContext context)
    {
        var t = typeof(NestedCommand);
        context.TryAddSingleton(t);

        var group = context.GetCommandGroup(t);
        group.AddCommand(typeof(TestSubcommand));
    }

    public NestedCommand(UnitContext context, UnitCore core, SimpleConsole simpleConsole)
        : base(context, core, simpleConsole)
    {
        this.ReadLineOptions = new ReadLineOptions
        {
            Prompt = "operate-credit>> ",
            MultilinePrompt = LpConstants.MultilinePromptString,
        };
    }

    public SeedKey? SeedKey { get; set; }

    public CreditIdentity? CreditIdentity { get; set; }

    public Credit? Credit { get; set; }
}

[SimpleCommand("operate-credit")]
public class OperateCreditCommand : ISimpleCommand<OperateCreditCommand.Options>
{
    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly NestedCommand nestedcommand;
    private readonly LpService lpService;
    private readonly RobustConnection.Factory robustConnectionFactory;

    public record Options
    {
        [SimpleOption("CreditIdentity", Description = "CreditIdentity", Required = true)]
        public CreditIdentity CreditIdentity { get; init; } = CreditIdentity.UnsafeConstructor();

        [SimpleOption("Code", Description = LpConstants.CodeDescription, Required = true)]
        public string Code { get; init; } = string.Empty;
    }

    public OperateCreditCommand(ILogger<OperateCreditCommand> logger, IUserInterfaceService userInterfaceService, NestedCommand nestedcommand, LpService lpService, RobustConnection.Factory robustConnectionFactory)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.nestedcommand = nestedcommand;
        this.lpService = lpService;
        this.robustConnectionFactory = robustConnectionFactory;
    }

    public async Task Execute(OperateCreditCommand.Options options, string[] args, CancellationToken cancellationToken)
    {
        // Code
        var seedKey = await this.lpService.GetSeedKeyFromCode(options.Code);
        if (seedKey is null)
        {
            return;
        }

        this.nestedcommand.SeedKey = seedKey;
        this.nestedcommand.CreditIdentity = options.CreditIdentity;
        this.nestedcommand.Credit = options.CreditIdentity.ToCredit();
        if (this.nestedcommand.Credit is null)
        {
            return;
        }

        this.userInterfaceService.WriteLine($"Credit: {this.nestedcommand.Credit}");
        this.userInterfaceService.WriteLine($"Seed key: {seedKey.GetSignaturePublicKey()}");

        await this.nestedcommand.MainAsync();
    }
}
