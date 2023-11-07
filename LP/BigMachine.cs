// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

[BigMachineObject(Inclusive = true)]
[AddMachine<Netsphere.Machines.NtpMachine>]
[AddMachine<Netsphere.Machines.PublicIPMachine>]
[AddMachine<Netsphere.Machines.NetStatMachine>]
public partial class BigMachine;
