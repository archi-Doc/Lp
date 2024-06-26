// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp;

[BigMachineObject(Inclusive = true)]
[AddMachine<Netsphere.Machines.NtpMachine>]
// [AddMachine<Netsphere.Machines.NetStatsMachine>]
[AddMachine<Lp.Machines.NodeControlMachine>]
public partial class BigMachine;
