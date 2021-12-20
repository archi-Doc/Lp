// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netsphere;

public static class NetConstants
{
    public const int SendingAckIntervalInMilliseconds = 10;

    public const double ResendWaitMilliseconds = 500;
    public const int SendCountMax = 3;
}
