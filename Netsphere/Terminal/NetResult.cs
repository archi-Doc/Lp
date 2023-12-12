// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

/// <summary>
/// Represents a result of network transmission.
/// </summary>
public enum NetResult
{
    Success,
    Timeout,
    Closed,
    InvalidAddress,
    NoDataToSend,
    NoNodeInformation,
    NoNetwork,
    NoEncryptedConnection,
    NoSender,
    NoReceiver,
    SerializationError,
    DeserializationError,
    PacketSizeLimit,
    BlockSizeLimit,
    StreamSizeLimit,
    ReserveError,
    NoNetService,
    NoCallContext,
    UnknownException,
    NotAuthorized,
    TransmissionConsumed,
    NoTransmission,
}
