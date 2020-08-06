//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.Utilities
{
    public static class TagStringParser
    {
        public static bool TryParse(string tagString, out Dictionary<string, string> parsedTags)
        {
            // Sample tagString: "Department:IT;Environment:Prod;Role:WorkerRole"
            parsedTags = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(tagString))
            {
                return true;
            }

            var tagParts = tagString.Split(';');

            if (tagParts != null && tagParts.Length > 0)
            {
                foreach (var tagPart in tagParts)
                {
                    var delimitIdx = tagPart.IndexOf(':');
                    if (delimitIdx <= 0 || delimitIdx > tagPart.Length - 2)
                    {
                        Console.WriteLine("Cannot parse tags string since it is in invalid format: " + tagString);
                        return false;
                    }

                    var tagKey = tagPart.Substring(0, delimitIdx);
                    var tagValue = tagPart.Substring(delimitIdx + 1);

                    parsedTags[tagKey] = tagValue;
                }
            }

            return true;
        }
    }
}
