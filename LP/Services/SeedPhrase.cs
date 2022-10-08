// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace LP;

public class SeedPhrase
{
    private const string TinyhandPath = "Strings.english.tinyhand";

    public SeedPhrase()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        try
        {
            using (var stream = assembly.GetManifestResourceStream(assembly.GetName().Name + "." + TinyhandPath))
            {
                if (stream != null)
                {
                    var words = TinyhandSerializer.Deserialize<string[]>(stream, TinyhandSerializerOptions.Lz4);
                    if (words != null)
                    {
                        this.words = words;
                    }
                }
            }
        }
        catch
        {
        }
    }

    private string[] words = Array.Empty<string>();
}
