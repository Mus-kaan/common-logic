//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using System;
using System.Collections;
using System.IO;

namespace Microsoft.Liftr.JwtLibrary.Tests
{
    public static class JwtTokenHelpers
    {
        public static void GenerateCertificateAndPrivateKey(string certfile, string pkfile, string alias, string password)
        {
            AsymmetricCipherKeyPair kp;
            var certificate = NewCert(pkfile, out kp);
            SavePublicKey(certificate, certfile);
            SavePrivateKey(certificate, kp, pkfile, alias, password);
        }

        public static X509Certificate NewCert(string id, out AsymmetricCipherKeyPair kp)
        {
            var kpgen = new RsaKeyPairGenerator();
            kpgen.Init(new KeyGenerationParameters(
                SecureRandom.GetInstance("SHA256PRNG", true),
                2048));
            kp = kpgen.GenerateKeyPair();
            var gen = new X509V3CertificateGenerator();
            var certName = new X509Name("CN=" + Environment.MachineName + id);
            var serialNo = BigIntegers.CreateRandomBigInteger(120, SecureRandom.GetInstance("SHA256PRNG", true));

            gen.SetSerialNumber(serialNo);
            gen.SetSubjectDN(certName);
            gen.SetIssuerDN(certName);
            gen.SetNotAfter(DateTime.Now.AddYears(1));
            gen.SetNotBefore(DateTime.Now.Subtract(new TimeSpan(7, 0, 0, 0)));
            gen.SetPublicKey(kp.Public);

            gen.AddExtension(
                X509Extensions.AuthorityKeyIdentifier.Id,
                false,
                new AuthorityKeyIdentifier(
                    SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(kp.Public),
                    new GeneralNames(new GeneralName(certName)),
                    serialNo));

            gen.AddExtension(
                X509Extensions.ExtendedKeyUsage.Id,
                false,
                new ExtendedKeyUsage(new ArrayList()
                {
                    new DerObjectIdentifier("1.3.6.1.5.5.7.3.1"),
                }.ToArray()));

            var random = SecureRandom.GetInstance("SHA256PRNG", true);
            var signatureFactory = new Asn1SignatureFactory("SHA512WITHRSA", kp.Private, random);
            return gen.Generate(signatureFactory);
        }

        public static void SavePublicKey(X509Certificate certificate, string filename)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }

            var encoded = certificate.GetEncoded();
            using (var certFile = File.Create(filename))
            {
                certFile.Write(encoded, 0, encoded.Length);
            }
        }

        public static void SavePrivateKey(
            X509Certificate certificate,
            AsymmetricCipherKeyPair kp,
            string filename,
            string alias,
            string password)
        {
            if (kp == null)
            {
                throw new ArgumentNullException(nameof(kp));
            }

            if (certificate == null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }

            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            var store = new Pkcs12Store();

            var certEntry = new X509CertificateEntry(certificate);

            store.SetCertificateEntry(
                alias,
                certEntry);

            store.SetKeyEntry(
                alias,
                new AsymmetricKeyEntry(kp.Private),
                new[] { certEntry });

            using (var certFile = File.Create(filename))
            {
                store.Save(
                    certFile,
                    password.ToCharArray(),
                    SecureRandom.GetInstance("SHA256PRNG", true));
            }
        }
    }
}
