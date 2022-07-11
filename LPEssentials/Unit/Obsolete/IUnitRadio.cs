// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;

namespace LPEssentials.Unit.Obsolete;

/// <summary>
/// <see cref="IUnitRadio"/> provides intra-unit communication (Pub/Sub) services.
/// </summary>
public interface IUnitRadio
{
    public RadioClass GetRadio();

    public void SendPrepare(UnitMessage.Prepare message);

    public Task SendRunAsync(UnitMessage.RunAsync message);

    public Task SendTerminateAsync(UnitMessage.TerminateAsync message);

    public Task SendLoadAsync(UnitMessage.LoadAsync message);

    public Task SendSaveAsync(UnitMessage.SaveAsync message);
}
