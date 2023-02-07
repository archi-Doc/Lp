// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

[TinyhandGenerateHash("strings-en.tinyhand")]
public static partial class Hashed
{
    public static string FromEnum<T>(T enumValue)
        where T : Enum
        => HashedString.GetOrAlternative($"{typeof(T).Name}.{enumValue.ToString()}", "No string");
}
