// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimplePrompt;

namespace Lp;

public static class IUserInterfaceServiceExtention
{
    public static void WriteLine(this IUserInterfaceService service, ulong hash)
        => service.WriteLine(HashedString.Get(hash));

    public static void WriteLine(this IUserInterfaceService service, ulong hash, object obj1)
        => service.WriteLine(HashedString.Get(hash, obj1));

    public static void WriteLine(this IUserInterfaceService service, ulong hash, object obj1, object obj2)
        => service.WriteLine(HashedString.Get(hash, obj1, obj2));

    public static Task<InputResultKind> ReadYesNo(this IUserInterfaceService viewService, bool cancelOnEscape, ulong hash)
        => viewService.ReadYesNo(cancelOnEscape, HashedString.Get(hash));

    public static Task<InputResultKind> ReadYesNo(this IUserInterfaceService viewService, bool cancelOnEscape, ulong hash, object obj1)
        => viewService.ReadYesNo(cancelOnEscape, HashedString.Get(hash, obj1));

    public static Task<InputResultKind> ReadYesNo(this IUserInterfaceService viewService, bool cancelOnEscape, ulong hash, object obj1, object obj2)
        => viewService.ReadYesNo(cancelOnEscape, HashedString.Get(hash, obj1, obj2));

    public static Task<InputResult> RequestString(this IUserInterfaceService viewService, bool cancelOnEscape, ulong hash)
        => viewService.ReadLine(cancelOnEscape, HashedString.Get(hash));

    public static Task<InputResult> RequestString(this IUserInterfaceService viewService, bool cancelOnEscape, ulong hash, object obj1)
        => viewService.ReadLine(cancelOnEscape, HashedString.Get(hash, obj1));

    public static Task<InputResult> RequestString(this IUserInterfaceService viewService, bool cancelOnEscape, ulong hash, object obj1, object obj2)
        => viewService.ReadLine(cancelOnEscape, HashedString.Get(hash, obj1, obj2));

    public static Task<InputResult> ReadPassword(this IUserInterfaceService viewService, bool cancelOnEscape, ulong hash)
        => viewService.ReadPassword(cancelOnEscape, HashedString.Get(hash));

    public static Task<InputResult> ReadPassword(this IUserInterfaceService viewService, bool cancelOnEscape, ulong hash, object obj1)
        => viewService.ReadPassword(cancelOnEscape, HashedString.Get(hash, obj1));

    public static Task<InputResult> ReadPassword(this IUserInterfaceService viewService, bool cancelOnEscape, ulong hash, object obj1, object obj2)
        => viewService.ReadPassword(cancelOnEscape, HashedString.Get(hash, obj1, obj2));

    public static Task Notify(this IUserInterfaceService viewService, LogLevel level, ulong hash)
        => viewService.Notify(level, HashedString.Get(hash));

    public static Task Notify(this IUserInterfaceService viewService, LogLevel level, ulong hash, object obj1)
        => viewService.Notify(level, HashedString.Get(hash, obj1));

    public static Task Notify(this IUserInterfaceService viewService, LogLevel level, ulong hash, object obj1, object obj2)
        => viewService.Notify(level, HashedString.Get(hash, obj1, obj2));

    public static async Task<InputResult> ReadPasswordAndConfirm(this IUserInterfaceService viewService, bool cancelOnEscape, ulong hash, ulong hash2)
    {
        InputResult result;
        while (true)
        {
            result = await viewService.ReadPassword(cancelOnEscape, hash).ConfigureAwait(false);
            if (!result.IsSuccess)
            {// Canceled or Terminated
                return result;
            }
            else if (result.Text == string.Empty)
            {// Empty password
                viewService.WriteLine(Hashed.Dialog.Password.EmptyWarning);
                var resultKind = await viewService.ReadYesNo(cancelOnEscape, Hashed.Dialog.Password.EmptyConfirm).ConfigureAwait(false);
                if (resultKind.IsPositive)
                {// Yes (Empty password)
                    return result;
                }
                else if (resultKind.IsNegative)
                {// No
                    continue;
                }
                else
                {// Canceled or Terminated
                    return new(resultKind);
                }
            }
            else
            {
                break;
            }
        }

        while (true)
        {
            var confirmResult = await viewService.ReadPassword(cancelOnEscape, hash2).ConfigureAwait(false);
            if (!confirmResult.IsSuccess)
            {// Canceled or Terminated
                return confirmResult;
            }

            if (result.Text != confirmResult.Text)
            {// Does not match
                viewService.WriteLine(Hashed.Dialog.Password.NotMatch);
            }
            else
            {
                return result;
            }
        }
    }
}
