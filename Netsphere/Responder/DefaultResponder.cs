// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netsphere.Responder;

internal class DefaultResponder
{
    public static void Register(NetControl netControl)
    {
        netControl.AddResponder(TestBlockResponder.Instance);
    }
}
