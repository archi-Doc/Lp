// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace
global using System;
global using System.IO;
global using Arc.Collections;
global using Arc.Crypto;
global using Tinyhand;

namespace LP;

public static class Constants
{
    public const string ECCurveName = "secp256r1";
    public const string HashName = "SHA3-256";
    public const uint HashBits = 256;
    public const uint HashBytes = HashBits / 8;
}
