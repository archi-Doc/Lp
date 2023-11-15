// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Crypto;

public enum KeyClass : byte
{
    /// <summary>
    /// T3CS key for encryption (secp256r1/ECDH).
    /// </summary>
    T3CS_Encryption,

    /// <summary>
    /// T3CS key for signature (secp256r1/ECDsa).
    /// </summary>
    T3CS_Signature,

    /// <summary>
    /// Node key for encryption (secp256r1/ECDH).
    /// </summary>
    Node_Encryption,
}
