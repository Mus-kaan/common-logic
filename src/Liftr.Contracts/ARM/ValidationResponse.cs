//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Contracts.ARM
{
    public class ValidationResponse
    {
        private const string ValidationResponseSuccess = "Successful";
        private const string ValidationResponseFailed = "Failed";

        public string Status { get; set; }

        public ValidationError Error { get; set; }

        public static ValidationResponse BuildValidationResponseSuccessful()
        {
            return new ValidationResponse()
            {
                Status = ValidationResponseSuccess,
                Error = null,
            };
        }

        public static ValidationResponse BuildValidationResponseFailed(string code, string message)
        {
            return new ValidationResponse()
            {
                Status = ValidationResponseFailed,
                Error = new ValidationError()
                {
                    Code = code,
                    Message = message,
                },
            };
        }

        public static ValidationResponse BuildBadRequestValidationResponse(string message)
        {
            return BuildValidationResponseFailed("BadRequest", message);
        }

        public bool IsSuccessful()
        {
            return Status == ValidationResponseSuccess;
        }

        public bool IsFailed()
        {
            return Status == ValidationResponseFailed;
        }
    }

    public class ValidationError
    {
        public string Code { get; set; }

        public string Message { get; set; }
    }
}
