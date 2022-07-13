// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LP;

public static class Version
{
    public static string Get()
    {
        var version = "1.0.0";
        var asm = Assembly.GetEntryAssembly();
        var infoVersion = asm!.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (infoVersion != null)
        {
            version = infoVersion.InformationalVersion;
        }
        else
        {
            var asmVersion = asm!.GetCustomAttribute<AssemblyVersionAttribute>();
            if (asmVersion != null)
            {
                version = asmVersion.Version;
            }
        }

        return version;
    }
}
