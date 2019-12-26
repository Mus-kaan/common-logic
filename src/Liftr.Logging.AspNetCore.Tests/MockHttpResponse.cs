//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Logging.AspNetCore.Tests
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "<Pending>")]
    public class MockHttpResponse : HttpResponse
    {
        private MemoryStream _ms = new MemoryStream();

        public MockHttpResponse()
        {
            Body = _ms;
        }

        public override HttpContext HttpContext { get; }

        public override int StatusCode { get; set; }

        public override IHeaderDictionary Headers { get; }

        public override Stream Body { get; set; }

        public override long? ContentLength { get; set; }

        public override string ContentType { get; set; }

        public override IResponseCookies Cookies { get; }

        public override bool HasStarted { get; }

        public override void OnCompleted(Func<object, Task> callback, object state)
        {
            throw new NotImplementedException();
        }

        public override void OnStarting(Func<object, Task> callback, object state)
        {
            throw new NotImplementedException();
        }

        public override void Redirect(string location, bool permanent)
        {
            throw new NotImplementedException();
        }

        public string GetContent()
        {
            return Encoding.ASCII.GetString(_ms.ToArray());
        }
    }
}
