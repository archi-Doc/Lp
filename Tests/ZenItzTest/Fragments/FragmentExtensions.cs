// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Fragments;

public static class FragmentExtensions
{
    public static ZenResult SetFragment<TFragment>(this Flake flake, TFragment fragment)
        where TFragment : FragmentBase
    {
        return flake.SetObject(fragment);
    }

    public static async Task<TFragment?> GetFragment<TFragment>(this Flake flake)
        where TFragment : FragmentBase
    {
        var result = await flake.GetObject<TFragment>();
        return result.Object as TFragment;
    }
}
