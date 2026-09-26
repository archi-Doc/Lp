// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;
using Lp.Logging;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using xUnitTest.Lp;

namespace xUnitTest;

[Collection(LpFixtureCollection.Name)]
public class ModestLoggerTest
{
    private readonly ILogger logger;

    public ModestLoggerTest(LpFixture fixture)
    {
        this.logger = fixture.ServiceProvider.GetRequiredService<ILogger<ModestLoggerTest>>();
    }

    [Fact]
    public void AConsecutiveMessageIsLoggedAgainAfterAReset()
    {
        var modestLogger = new ModestLogger(this.logger);
        Assert.NotNull(modestLogger.NonConsecutive(1, LogLevel.Error));
        Assert.Null(modestLogger.NonConsecutive(1, LogLevel.Error));
        Assert.NotNull(modestLogger.NonConsecutive(2, LogLevel.Error));

        modestLogger.ResetNonConsecutive(); // e.g., the error has been resolved.
        Assert.NotNull(modestLogger.NonConsecutive(2, LogLevel.Error));
    }
}
