// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LP;

namespace LPRunner;

public class RunnerBase
{
    public RunnerBase(LPBase lpBase, RunnerInformation runnerInformation)
    {
        this.LPBase = lpBase;
        this.Information = runnerInformation;
    }

    internal LPBase LPBase { get; set; }

    internal RunnerInformation Information { get; set; }
}
