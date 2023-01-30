// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValueLink;

namespace ZenItz;

public partial class Zen<TIdentifier>
{
    [ValueLinkObject]
    public partial class FlakeBase
    {
        [Link(Primary = true, Name = "RemoveQueue", Type = ChainType.LinkedList)]
        internal FlakeBase()
        {
        }
    }
}
