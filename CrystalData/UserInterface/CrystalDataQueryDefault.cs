// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.UserInterface;

internal class CrystalDataQueryDefault : ICrystalDataQuery
{
    async Task<AbortOrContinue> ICrystalDataQuery.NoCheckFile()
    {
        this.WriteLine("Check file not found.");
        var result = await this.RequestYesOrNo($"Do you want to force a launch? (enter 'y' if this is the first launch)").ConfigureAwait(false);
        return result.ToAbortOrContinue();
    }

    #region Misc

    private void Write(string? message = null)
        => Console.Write(message);

    private void WriteLine(string? message = null)
        => Console.WriteLine(message);

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

    private async Task<bool?> RequestYesOrNoInternal(string? description)
    {
        if (!string.IsNullOrEmpty(description))
        {
            this.WriteLine(description + " [Y/n]");
        }

        while (true)
        {
            var input = Console.ReadLine();
            if (input == null)
            {// Ctrl+C
                this.WriteLine();
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
                this.WriteLine("[Y/n]");
            }
        }
    }

    private Task<bool?> RequestYesOrNo(string? description)
       => this.RequestYesOrNoInternal(description);

    #endregion
}
