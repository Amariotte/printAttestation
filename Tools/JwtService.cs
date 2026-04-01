using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace InteroperabiliteProject.Tools
{
    public class JwtService
    {
        private string secretKey = "MzYxMDEyMzQ1Njc4OTAxMjM0NTY3OA=="; // Utilisez une clé plus sécurisée et stockez-la de manière sécurisée

        //public string GenerateToken(string username)
        //{
        //    var tokenHandler = new JwtSecurityTokenHandler();
        //    var key = Encoding.ASCII.GetBytes(secretKey);
        //    var tokenDescriptor = new SecurityTokenDescriptor
        //    {
        //        Subject = new ClaimsIdentity(new[] { new Claim("username", username) }),
        //        Expires = DateTime.UtcNow.AddHours(1),
        //        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        //    };

        //    var token = tokenHandler.CreateToken(tokenDescriptor);
        //    return tokenHandler.WriteToken(token);
        //}

        public string EncryptToken(string token)
        {
            var key = Encoding.ASCII.GetBytes(secretKey);
            using (var aesAlg = new AesManaged())
            {
                aesAlg.Key = key;
                aesAlg.GenerateIV();
                var iv = aesAlg.IV;

                var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (var msEncrypt = new System.IO.MemoryStream())
                {
                    msEncrypt.Write(iv, 0, iv.Length); // prepend IV
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (var swEncrypt = new System.IO.StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(token);
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        public string? DecryptToken(string encryptedToken)
        {
            try
            {
                var fullCipher = Convert.FromBase64String(encryptedToken);
                var key = Encoding.ASCII.GetBytes(secretKey);
                using (var aesAlg = new AesManaged())
                {
                    var iv = new byte[aesAlg.BlockSize / 8];
                    var cipherText = new byte[fullCipher.Length - iv.Length];

                    Array.Copy(fullCipher, iv, iv.Length);
                    Array.Copy(fullCipher, iv.Length, cipherText, 0, cipherText.Length);

                    aesAlg.Key = key;
                    aesAlg.IV = iv;
                    var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    using (var msDecrypt = new System.IO.MemoryStream(cipherText))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (var srDecrypt = new System.IO.StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {

                return null;
            }
            
        }
    }
}
