// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

[BigMachineObject(Inclusive = true)]
[AddMachine<Netsphere.Machines.NtpMachine>]
[AddMachine<Netsphere.Machines.NetStatsMachine>]
[AddMachine<Netsphere.Machines.EssentialNetMachine>]
public partial class BigMachine;
