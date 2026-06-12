using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ask.Dtos;
using ask.Interface;
using ask.Model;
using Microsoft.IdentityModel.Tokens;


namespace ask.Services
{
    public class JwtService
    {
        private readonly string _encryptionKey;
        private readonly IConfiguration _configuration;
        private readonly IRefreshTokenRepo _refreshtoken;

        public JwtService(IConfiguration configuration, IRefreshTokenRepo refreshtoken)
        {
            _configuration = configuration;
            _refreshtoken = refreshtoken;
            _encryptionKey = configuration["JwtSettings:EncryptionKey"] ?? "";
        }


        public string EncryptToken(string token)
        {
            var key = Encoding.ASCII.GetBytes(_encryptionKey);
            using var aesAlg = Aes.Create();
            aesAlg.Key = key;
            aesAlg.GenerateIV();
            var iv = aesAlg.IV;

            var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using var msEncrypt = new MemoryStream();
            msEncrypt.Write(iv, 0, iv.Length);
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(token);
            }
            return Convert.ToBase64String(msEncrypt.ToArray());
        }

        public string? DecryptToken(string encryptedToken)
        {
            try
            {
                var fullCipher = Convert.FromBase64String(encryptedToken);
                var key = Encoding.ASCII.GetBytes(_encryptionKey);
                using var aesAlg = Aes.Create();

                var iv = new byte[aesAlg.BlockSize / 8];
                var cipherText = new byte[fullCipher.Length - iv.Length];

                Array.Copy(fullCipher, iv, iv.Length);
                Array.Copy(fullCipher, iv.Length, cipherText, 0, cipherText.Length);

                aesAlg.Key = key;
                aesAlg.IV = iv;
                var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using var msDecrypt = new MemoryStream(cipherText);
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var srDecrypt = new StreamReader(csDecrypt);
                return srDecrypt.ReadToEnd();
            }
            catch (Exception)
            {
                return null;
            }
        }



        public string GenerateJwtToken(JwtIssueOptions opt)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");

            var secret = jwtSettings["Key"];
            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("JwtSettings:Key manquant.");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var defaultSeconds = int.TryParse(jwtSettings["ExpiryInSecond"], out var s) ? s : 3600;
            var lifeSeconds = opt.LifetimeMinutesOverride.HasValue
                ? opt.LifetimeMinutesOverride.Value * 60
                : defaultSeconds;

            var now = DateTime.UtcNow;
            var exp = now.AddSeconds(lifeSeconds);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, $"user:{opt.UserId}"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, Tools.Tools.ToUnixTimeSeconds(now).ToString(), ClaimValueTypes.Integer64),
                new Claim("iduser", opt.UserId.ToString()),
                new Claim("email", opt.UserEmail),
            };

            // Rôles : un claim par rôle
            foreach (var r in opt.Roles)
                claims.Add(new Claim(ClaimTypes.Role, r));

          
            // Construction du JWT
            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                notBefore: now,
                expires: exp,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        public async Task<t_refresh_token> GenerateRefreshToken(int userId)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");

            int dureeTokenRefreshInHour = int.TryParse(jwtSettings["ExpiryRefreshInheure"], out var h) ? h : 24;
            string refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            t_refresh_token refresh_data = new t_refresh_token
            {
                r_is_active = true,
                r_is_revoked = false,
                r_expires_at = DateTime.UtcNow.AddHours(dureeTokenRefreshInHour),
                r_is_delete = false,
                r_token = refreshToken,
                r_user_id_fk = userId,
            };

            await _refreshtoken.AddAsync(refresh_data);

            return refresh_data;
        }

    }
}
