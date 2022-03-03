//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Bson.Serialization.Attributes;

namespace Microsoft.Liftr.DBService.Contracts
{
    public class UserDetail
    {
        [EntityUpdateAttribute(allowed: true)]
        [BsonElement("firstName")]
        public string FirstName { get; set; }

        [EntityUpdateAttribute(allowed: true)]
        [BsonElement("lastName")]
        public string LastName { get; set; }

        [EntityUpdateAttribute(allowed: true)]
        [BsonElement("emailAddress")]
        public string EmailAddress { get; set; }

        [EntityUpdateAttribute(allowed: true)]
        [BsonElement("upn")]
        public string Upn { get; set; }

        [EntityUpdateAttribute(allowed: true)]
        [BsonElement("phoneNumber")]
        public string PhoneNumber { get; set; }
    }
}