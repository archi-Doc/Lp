// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

/// <summary>
/// Represents a result of network transmission.
/// </summary>
public enum NetResult : byte
{
    Success,
    UnknownError,
    Timeout,
    Canceled,
    Closed,
    Completed,
    InvalidOperation,
    SerializationFailed,
    DeserializationFailed,
    BlockSizeLimit,
    StreamLengthLimit,
    NoNodeInformation,
    NoNetwork,
    NoNetService,
    NoTransmission,
    NotReceived,
    NotAuthorized,
    NotFound,
}
