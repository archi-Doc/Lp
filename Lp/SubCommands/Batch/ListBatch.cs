// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands;

public partial class Batch
{
    [SimpleCommand("list-batch")]
    public class ListBatch : ISimpleCommand
    {
        private readonly VaultControl vaultControl;
        private readonly IUserInterfaceService userInterfaceService;

        public ListBatch(VaultControl vaultControl, IUserInterfaceService userInterfaceService)
        {
            this.vaultControl = vaultControl;
            this.userInterfaceService = userInterfaceService;
        }

        public async Task Execute(string[] args, CancellationToken cancellationToken)
        {
            var names = this.vaultControl.Root.GetNames(Batch.Prefix);
            for (var i = 0; i < names.Length; i++)
            {
                names[i] = names[i].Substring(Batch.Prefix.Length);
            }

            this.userInterfaceService.WriteLine(string.Join(' ', names));
        }
    }
}
