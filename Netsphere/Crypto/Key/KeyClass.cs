// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Crypto;

public enum KeyClass : byte
{
    /// <summary>
    /// T3CS key for encryption (secp256r1/ECDH).
    /// </summary>
    Encryption,

    /// <summary>
    /// T3CS key for signature (secp256r1/ECDsa).
    /// </summary>
    Signature,

    /// <summary>
    /// Node key for encryption (secp256r1/ECDH).
    /// </summary>
    NodeEncryption,
}
