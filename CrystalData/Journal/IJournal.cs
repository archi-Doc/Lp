﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public interface IJournal
{
    Task<CrystalStartResult> Prepare();

    bool Prepared { get; }
}
