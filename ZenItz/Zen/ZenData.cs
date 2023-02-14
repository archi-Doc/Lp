// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public record ZenDataInformation(int Id, Func<BaseData> CreateInstance);

public static class ZenData
{
    public static bool Register(ZenDataInformation information)
    {
        if (information.Id == 0 || array.Any(x => x.Id == information.Id))
        {
            return false;
        }

        Array.Resize(ref array, array.Length + 1);
        array[array.Length - 1] = information;
        return true;
    }

    internal static BaseData? TryCreateInstance(int id)
    {
        foreach (var x in array)
        {
            if (x.Id == id)
            {
                return x.CreateInstance();
            }
        }

        return null;
    }

    private static ZenDataInformation[] array = Array.Empty<ZenDataInformation>();
}
