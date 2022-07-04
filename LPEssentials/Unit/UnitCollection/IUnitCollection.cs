// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace LP.Unit;

public interface IUnitCollection : IList<UnitDescriptor>, ICollection<UnitDescriptor>, IEnumerable<UnitDescriptor>
{
    public void AddUnit<TUnit>(bool createInstance)
        where TUnit : IUnit;
}
