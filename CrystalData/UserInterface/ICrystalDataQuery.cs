// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.UserInterface;

public interface ICrystalDataQuery
{
    Task<AbortOrContinue> NoCheckFile();
}
