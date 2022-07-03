// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace LP.Unit;

public interface ICommandCollection : IList<CommandDescriptor>, ICollection<CommandDescriptor>
{
    public void Add(Type commandType);
}
