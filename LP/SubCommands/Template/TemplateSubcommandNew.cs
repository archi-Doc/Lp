// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("ls")]
public class TemplateSubcommandLs : ISimpleCommandAsync
{
    public TemplateSubcommandLs(Control control, IUserInterfaceService userInterfaceService)
    {
        this.control = control;
        this.userInterfaceService = userInterfaceService;
    }

    public async Task RunAsync(string[] args)
    {
        this.userInterfaceService.WriteLine("Template");
    }

    private readonly Control control;
    private readonly IUserInterfaceService userInterfaceService;
}
