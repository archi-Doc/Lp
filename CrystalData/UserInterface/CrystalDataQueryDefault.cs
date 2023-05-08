// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.UserInterface;

internal class CrystalDataQueryDefault : ICrystalDataQuery
{
    private enum YesOrNo
    {
        Invalid,
        Yes,
        No,
    }

    private enum YesOrNoOrYesToAll
    {
        Invalid,
        Yes,
        No,
        YesToAll,
    }

    async Task<AbortOrContinue> ICrystalDataQuery.NoCheckFile()
    {
        this.WriteLine(CrystalDataHashed.CrystalDataQueryDefault.NoCheckFile);
        var result = await this.RequestYesOrNo(CrystalDataHashed.CrystalDataQueryDefault.NoCheckFileQuery).ConfigureAwait(false);
        return result.ToAbortOrContinue();
    }

    async Task<AbortOrContinue> ICrystalDataQuery.InconsistentJournal()
    {// yes/no/all
        var response = this.GetCache();
        if (response == YesOrNo.Yes)
        {
            return AbortOrContinue.Continue;
        }

        response = await this.RequestYesOrNo(CrystalDataHashed.CrystalDataQueryDefault.InconsistentJournal).ConfigureAwait(false);
        this.Cache(response);
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

    private async Task<bool?> RequestYesOrNoInternal(ulong hashed)
    {
        var description = HashedString.Get(hashed);
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
                return null; // throw new PanicException();
            }

            input = input.CleanupInput().ToLower();
            if (input == "y" || input == "yes")
            {
                return true;
            }
            else if (input == "n" || input == "no")
            {
                return false;
            }
            else
            {
                this.WriteLineRaw("[Y/n]");
            }
        }
    }

    private Task<bool?> RequestYesOrNo(ulong hashed)
       => this.RequestYesOrNoInternal(hashed);

    #endregion
}
