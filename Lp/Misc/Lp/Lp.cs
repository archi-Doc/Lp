// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Lp;

public static partial class Lp
{
    public static ParameterClass Parameters { get; } = new();

    [TinyhandObject(ImplicitMemberNameAsKey = true)]
    public partial class ParameterClass
    {
        public int DomainRadiantQueueCapacity { get; set; } = 32;

        public TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(3);

        public int ExitDelayMilliseconds { get; set; } = 300;
    }
}
