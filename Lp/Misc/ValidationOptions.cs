// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp;

[Flags]
public enum ValidationOptions : int
{
    Default = 0,
    IgnoreExpiration = 1,
    PreSign = 2,
}
