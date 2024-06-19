// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("ls")]
public class NodeSubcommandLs : ISimpleCommand
{
    public NodeSubcommandLs(Control control, IUserInterfaceService userInterfaceService)
    {
        this.control = control;
        this.userInterfaceService = userInterfaceService;
    }

    public void Run(string[] args)
    {
    }

    private Control control;
    private IUserInterfaceService userInterfaceService;
}
