// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

public interface ISignable
{
    bool PrepareForSigning(ref SignaturePublicKey publicKey, int validitySeconds);

    bool SetSignature(SignaturePair signaturePair);
}
