﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

public interface ISignable
{
    bool SetSignature(int signer, byte[] signature);
}
