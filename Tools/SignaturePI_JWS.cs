namespace InteroperabiliteProject.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class JwsSignatureGenerator
    {
        private readonly RSA _privateKey;
        private readonly X509Certificate2 _cert;

        public JwsSignatureGenerator(RSA privateKey, X509Certificate2 cert)
        {
            _privateKey = privateKey;
            _cert = cert;
        }

        public string GenerateJwsSignature(string payload)
        {
            string signatureDetache = string.Empty;
            try
            {
                // Création du JWS Header avec la clé publique
                var header = new Dictionary<string, object>
            {
                { "typ", "JOSE" },
                { "alg", "SHA256withRSA" },
                { "jwk", new { kty = "RSA", e = Convert.ToBase64String(_cert.GetPublicKey()), n = Convert.ToBase64String(_cert.GetRawCertData()) } }
            };

                // Encodage du header comme une chaîne JSON
                string headerJson = JsonSerializer.Serialize(header);

                // Concaténer le header et le payload avec un point (.) en séparateur
                string encodedHeader = Base64UrlEncode(headerJson);
                string unsignedJwt = encodedHeader + "." + Base64UrlEncode(payload);

                // Signer le JWT en utilisant la clé privée
                byte[] unsignedJwtBytes = Encoding.UTF8.GetBytes(unsignedJwt);
                byte[] signedJwtBytes;
                using (var rsa = RSA.Create())
                {
                    rsa.ImportParameters(_privateKey.ExportParameters(true));
                    signedJwtBytes = rsa.SignData(unsignedJwtBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }

                string signedJwt = Base64UrlEncode(signedJwtBytes);

                // Output du JWT signé
                Console.WriteLine("Payload inclus = " + unsignedJwt + "." + signedJwt); // TODO use logger

                signatureDetache = encodedHeader + ".." + signedJwt;
                Console.WriteLine("Signature détachée = " + signatureDetache); // TODO use logger
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return signatureDetache;
        }

        //public bool VerifyJwsSignature(string jwSignature, string payload)
        //{
        //    // Récupérer les deux parties de la signature à savoir le header et la signature encodée
        //    string[] parts = jwSignature.Split(new[] { ".." }, StringSplitOptions.None);
        //    if (parts.Length != 2)
        //    {
        //        throw new ArgumentException("Invalid JWS signature: expected 2 parts, but found " + parts.Length);
        //    }

        //    // Récupérer le header encodé
        //    string encodedHeader = parts[0];

        //    // Récupérer la signature encodée
        //    string encodedSigned = parts[1];

        //    // Récupérer la clé publique, qui nous permettra de vérifier la signature
        //    RSA publicKey;
        //    try
        //    {
        //        publicKey = RecupererClePublique(encodedHeader);
        //    }
        //    catch (Exception e)
        //    {
        //        throw new Exception("Impossible de récupérer la clé publique de la signature", e);
        //    }

        //    try
        //    {
        //        // Construire le message à vérifier
        //        string messageAverifier = encodedHeader + '.' + Base64UrlEncode(payload);

        //        // Initier la vérification
        //        byte[] messageBytes = Encoding.UTF8.GetBytes(messageAverifier);
        //        //byte[] decodedSignature = Base64UrlDecode(encodedSigned);
        //        //string decodedSignature = Base64UrlDecode(encodedSigned);
                    
        //        // Vérifier la signature
        //        //return publicKey.VerifyData(messageBytes, decodedSignature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        //    }
        //    catch (Exception e)
        //    {
        //        throw new Exception("Erreur lors de la vérification de la signature", e);
        //    }
        //}

        private RSA RecupererClePublique(string encodedHeader)
        {
            // Decode the JOSE header
            string joseHeader = Base64UrlDecode(encodedHeader);

            // Transformer le jose header en JSON
            var headerMap = JsonSerializer.Deserialize<Dictionary<string, object>>(joseHeader);

            // Récupérer l'objet jwk, à partir du quel nous allons construire la clé publique
            var jwk = headerMap["jwk"] as JsonElement?;
            if (jwk == null)
                throw new Exception("Invalid JWK in header");

            var e = Convert.FromBase64String(jwk.Value.GetProperty("e").GetString());
            var n = Convert.FromBase64String(jwk.Value.GetProperty("n").GetString());

            var rsaKeyInfo = new RSAParameters { Exponent = e, Modulus = n };
            var rsa = RSA.Create();
            rsa.ImportParameters(rsaKeyInfo);

            return rsa;
        }

        private static string Base64UrlEncode(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static string Base64UrlEncode(byte[] value)
        {
            return Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static string Base64UrlDecode(string value)
        {
            string base64 = value.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        }
    }

}
