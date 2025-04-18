// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.Machines;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record class LpKeyData
{
    public const string Filename = "LpKeyData";

    [KeyAsName]
    public CredentialNode[] CredentialNodes { get; set; } = [];
}

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record class CredentialNode([property:KeyAsName(ConvertToString = true)] NetNode Node, [property: KeyAsName(ConvertToString = true)] SignaturePublicKey RemoteKey, [property: KeyAsName(ConvertToString = true)] SignaturePublicKey MergerKey);

[MachineObject(UseServiceProvider = true)]
public partial class LpKeyMachine : Machine
{// Control: context.AddSingleton<Machines.RelayPeerMachine>();
    public LpKeyMachine(IUserInterfaceService consoleSeuserInterfaceServicevice, ILogger<LpKeyMachine> logger, AuthorityControl authorityControl, LpKeyData lpKeyData)
    {
        this.userInterfaceService = consoleSeuserInterfaceServicevice;
        this.logger = logger;
        this.authorityControl = authorityControl;
        this.lpKeyData = lpKeyData;

        this.DefaultTimeout = TimeSpan.FromSeconds(3);
    }

    public int Count { get; set; }

    [StateMethod(0)]
    protected async Task<StateResult> Initial(StateParameter parameter)
    {
        if (await this.authorityControl.GetLpSeedKey(null) is not { } seedKey)
        {
            return StateResult.Continue;
        }

        this.userInterfaceService.WriteLine("Lp key confirmed.");

        var list = this.lpKeyData.CredentialNodes.ToList();
        list.Add(new(Alternative.NetNode, SeedKey.NewSignature().GetSignaturePublicKey(), SeedKey.NewSignature().GetSignaturePublicKey()));
        this.lpKeyData.CredentialNodes = list.ToArray();

        // this.userInterfaceService.WriteLine($"Single: ({this.Identifier.ToString()}) - {this.Count++}");

        return StateResult.Continue;
    }

    private readonly IUserInterfaceService userInterfaceService;
    private readonly ILogger logger;
    private readonly AuthorityControl authorityControl;
    private readonly LpKeyData lpKeyData;
}
