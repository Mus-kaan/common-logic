//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Microsoft.Liftr.ImageBuilder
{
    public static class X509CertificateExtensions
    {
        /// <summary>
        /// Export a certificate to a PEM format string
        /// </summary>
        /// <param name="cert">The certificate to export</param>
        /// <returns>A PEM encoded string</returns>
        public static string ExportCertificateAsPEM(this X509Certificate2 cert)
        {
            if (cert == null)
            {
                throw new ArgumentNullException(nameof(cert));
            }

            byte[] certificateBytes = cert.RawData;

            AsymmetricAlgorithm key = cert.GetRSAPrivateKey();
            if (key == null)
            {
                key = cert.GetECDsaPrivateKey();
            }

            byte[] pubKeyBytes = key.ExportSubjectPublicKeyInfo();
            byte[] privKeyBytes = key.ExportPkcs8PrivateKey();

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("-----BEGIN PRIVATE KEY-----");
            builder.AppendLine(
                Convert.ToBase64String(privKeyBytes, Base64FormattingOptions.InsertLineBreaks));
            builder.AppendLine("-----END PRIVATE KEY-----");

            builder.AppendLine("-----BEGIN CERTIFICATE-----");
            builder.AppendLine(
                Convert.ToBase64String(certificateBytes, Base64FormattingOptions.InsertLineBreaks));
            builder.AppendLine("-----END CERTIFICATE-----");

            return builder.ToString();
        }
    }
}
