// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP;

namespace LPEssentials.Radio;

public static class MessageUI
{
    public record RequestYesOrNo
    {
        public RequestYesOrNo(string description)
        {
            this.Description = description;
            this.Yes = false;
        }

        public string Description { get; init; } = string.Empty;

        public bool Yes { get; set; } = false;
    }

    public record RequestString(string Description);
}
