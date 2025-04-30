// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.Services;

[TinyhandObject]
public sealed partial record class LpDogmaInformation(
    [property: Key(0)] EncryptionPublicKey NodeKey,
    [property: Key(1)] SignaturePublicKey MergerKey,
    [property: Key(2)] SignaturePublicKey RelayMergerKey,
    [property: Key(3)] SignaturePublicKey LinkerKey);
