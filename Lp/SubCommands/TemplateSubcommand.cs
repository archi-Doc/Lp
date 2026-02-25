// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using SimpleCommandLine;

namespace Lp.Subcommands;

/// <summary>
/// This is a template implementation of a subcommand.<br/>
/// To create a custom subcommand, copy the code, remove any unnecessary parts, and use constructor injection to obtain the required objects.<br/>
/// Do not forget to register the subcommand type with context.AddSubcommand().
/// </summary>
[SimpleCommand(Name)]
public class TemplateSubcommand : ISimpleCommandAsync<TemplateSubcommand.Options>
{
    public const string Name = "template-subcommand";

    public record Options
    {
        [SimpleOption("Credit", Description = "Credit", Required = true)]
        public Credit Credit { get; init; } = Credit.UnsafeConstructor();

        [SimpleOption("Code", Description = LpConstants.CodeDescription, Required = true)]
        public string Code { get; init; } = string.Empty;
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly LpService lpService;

    public TemplateSubcommand(SimpleParser simpleParser, ILogger<TemplateSubcommand> logger, IUserInterfaceService userInterfaceService, LpService lpService)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.lpService = lpService;

        if (simpleParser.TryGetOption(Name, "Credit", out var option))
        {
            option.Description = Example.Credit.ConvertToString();
        }
    }

    public async Task RunAsync(Options options, string[] args)
    {
        this.userInterfaceService.WriteLine("Template subcommand");

        var seedKey = await this.lpService.GetSeedKeyFromCode(options.Code).ConfigureAwait(false);
        if (seedKey is null)
        {
            return;
        }

        this.logger.TryGet()?.Log(seedKey.GetSignaturePublicKey().ToString());
        this.logger.TryGet()?.Log(options.ToString());
    }
}
