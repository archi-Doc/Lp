// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Responder;

internal class TestBlockResponder : NetResponder<TestPacket, TestPacket>
{
    public static readonly NetResponder<TestPacket, TestPacket> Instance = new TestBlockResponder();

    public override TestPacket? Respond(TestPacket value)
    {
        return value;
    }
}
