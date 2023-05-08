// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.UserInterface;

internal class CrystalDataQueryDefault : ICrystalDataQuery
{
    #region FieldAndProperty

    private Dictionary<ulong, YesOrNo> yesOrNoCache = new();

    #endregion

    async Task<AbortOrContinue> ICrystalDataQuery.NoCheckFile()
    {
        var result = await this.RequestYesOrNo(CrystalDataHashed.CrystalDataQueryDefault.NoCheckFile).ConfigureAwait(false);
        return result.ToAbortOrContinue();
    }

    async Task<AbortOrContinue> ICrystalDataQuery.InconsistentJournal(string path)
    {// yes/no/all
        var hash = CrystalDataHashed.CrystalDataQueryDefault.InconsistentJournal;
        if (this.yesOrNoCache.TryGetValue(hash, out var response))
        {
            return response.ToAbortOrContinue();
        }

        response = await this.RequestYesOrNo(CrystalDataHashed.CrystalDataQueryDefault.InconsistentJournal, path).ConfigureAwait(false);
        if (response == YesOrNo.Yes)
        {
            this.yesOrNoCache[hash] = response;
        }

        return response.ToAbortOrContinue();
    }

    #region Misc

    private void WriteRaw(string? message = null)
        => Console.Write(message);

    private void WriteLineRaw(string? message = null)
        => Console.WriteLine(message);

    private void Write(ulong hashed)
        => this.WriteRaw(HashedString.Get(hashed));

    private void WriteLine(ulong hashed)
        => this.WriteLineRaw(HashedString.Get(hashed));

    private string? ReadLine()
    {
        try
        {
            return Console.ReadLine();
        }
        catch
        {
            return null;
        }
    }

    private async Task<YesOrNo> RequestYesOrNoInternal(string message)
    {
        var description = message;
        if (!string.IsNullOrEmpty(description))
        {
            this.WriteLineRaw(description + " [Y/n]");
        }

        while (true)
        {
            var input = Console.ReadLine();
            if (input == null)
            {// Ctrl+C
                this.WriteLineRaw();
                return YesOrNo.Invalid; // throw new PanicException();
            }

            input = input.CleanupInput().ToLower();
            if (input == "y" || input == "yes")
            {
                return YesOrNo.Yes;
            }
            else if (input == "n" || input == "no")
            {
                return YesOrNo.No;
            }
            else
            {
                this.WriteLineRaw("[Y/n]");
            }
        }
    }

    private Task<YesOrNo> RequestYesOrNo(ulong hash)
       => this.RequestYesOrNoInternal(HashedString.Get(hash));

    private Task<YesOrNo> RequestYesOrNo(ulong hash, object obj1)
       => this.RequestYesOrNoInternal(string.Format(HashedString.Get(hash), obj1));

    #endregion
}
