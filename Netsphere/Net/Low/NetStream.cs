// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netsphere.Net;

public interface NetStream : IDisposable
{
    Task<NetResult> SendAsync(Span<byte> data);
}
