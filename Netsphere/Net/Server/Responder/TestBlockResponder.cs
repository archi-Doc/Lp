// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Block;
using Netsphere.Server;

namespace Netsphere.Responder;

public class TestBlockResponder : SyncResponder<TestBlock, TestBlock>
{
    public static readonly INetResponder Instance = new TestBlockResponder();

    public override TestBlock? RespondSync(TestBlock value)
    {
        return value;
    }
}
