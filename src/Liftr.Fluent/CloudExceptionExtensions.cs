//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Rest.Azure;
using System.Net;

namespace Microsoft.Liftr
{
    public static class CloudExceptionExtensions
    {
        public static bool IsNotFound(this CloudException ex)
            => ex?.Response?.StatusCode == HttpStatusCode.NotFound;

        public static bool IsConflict(this CloudException ex)
           => ex?.Response?.StatusCode == HttpStatusCode.Conflict;

        public static bool IsDuplicatedRoleAssignment(this CloudException ex)
           => ex?.Response?.StatusCode == HttpStatusCode.Conflict && (ex?.Message?.Contains("The role assignment already exists") == true);

        public static bool IsMissUseAppIdAsObjectId(this CloudException ex)
           => ex?.Message?.Contains("Principals of type Application cannot validly be used in role assignments") == true;
    }
}