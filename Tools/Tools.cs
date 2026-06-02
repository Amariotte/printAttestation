using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;


namespace ask.Tools
{
    public static class Tools
    {

        public static Dictionary<string, string> dt = new Dictionary<string, string>();
       public static string GenerateUniqueCode()
        {

            // Utiliser la date et l'heure actuelle pour garantir l'unicité
            DateTime now = DateTime.Now;

            // Convertir la date et l'heure actuelle en une chaîne de caractères
            string dateTimeString = now.ToString("yyyyMMddHHmmssfff"); // 17 caractères

            // Créer un générateur de nombres aléatoires
            Random random = new Random();

            // Générer un suffixe aléatoire pour compléter les 10 caractères
            StringBuilder uniqueCode = new StringBuilder();

            // Utiliser les derniers caractères de la date et l'heure actuelle
            uniqueCode.Append(dateTimeString.Substring(dateTimeString.Length - 7)); // Les 7 derniers caractères

            // Ajouter 3 caractères aléatoires
            for (int i = 0; i < 3; i++)
            {
                uniqueCode.Append((char)random.Next('A', 'Z' + 1)); // Lettres majuscules aléatoires
            }

            return uniqueCode.ToString();
        }

        public static string GenerateMD5Hash()
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(GenerateUniqueCode());
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2")); // Format en hexadécimal
                }
                return sb.ToString();
            }
        }


 
        public static string Generatechiffrealeatoire(int nbre)
        {
            // Création d'une instance de Random
            Random random = new Random();
            string nb = "";
            // Génération de trois chiffres aléatoires
            for (int i = 0; i < nbre; i++)
            {
                int digit1 = random.Next(0, 10);
                nb += digit1;
            }

            return nb;
        }
        public static string FirstNotNullOrEmpty(params string?[] values)
        {
            foreach (var val in values)
            {
                if (!string.IsNullOrEmpty(val))
                    return val;
            }
            return "";
        }


        public static bool IsValidSearchKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            // Longueur entre 3 et 50 caractères
            if (key.Length < 3 || key.Length > 50)
                return false;

            // Autoriser uniquement : lettres (majuscules/minuscules), chiffres, tirets, underscores, espaces
            // Interdire les caractères spéciaux SQL dangereux
            if (!System.Text.RegularExpressions.Regex.IsMatch(key, @"^[a-zA-Z0-9\-_ ]+$"))
                return false;

            // Interdire les mots-clés SQL dangereux (case insensitive)
            string[] sqlKeywords = { "DROP", "DELETE", "INSERT", "UPDATE", "ALTER", "EXEC", "EXECUTE", "UNION", "SELECT", "--", "/*", "*/", ";", "SCRIPT", "JAVASCRIPT" };
            string upperKey = key.ToUpperInvariant();

            foreach (var keyword in sqlKeywords)
            {
                if (upperKey.Contains(keyword))
                    return false;
            }

            return true;
        }
        public static string MaskPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return "***";
            if (phone.Length <= 4) return "***";
            return new string('*', phone.Length - 2) + phone[^2..];
        }

        public static string MaskEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return "***";
            var parts = email.Split('@');
            if (parts.Length != 2) return "***";

            var local = parts[0];
            var domain = parts[1];

            // Exemple: jo***@gmail.com
            string maskedLocal = local.Length <= 2
                ? local[0] + "***"
                : local.Substring(0, 2) + "***";

            return maskedLocal + "@" + domain;
        }

        public static TYPE_MODELE? EquivalenceOtpEnModele(TYPE_OTP otpType)
        {
            switch (otpType)
            {
                case TYPE_OTP.RESET_PASSWORD:
                    return TYPE_MODELE.MOT_PASSE_OUBLIE;               
                case TYPE_OTP.CONFIRMATION_REGISTER:
                    return TYPE_MODELE.ENVOI_ACCESS;    
                default:
                    return null;
            }
         }

     
     public static bool EstUnNumeroTelephone(string chaine)
        {
            string pattern = @"^\+(?:223|226|228|229|225|221|227|245)\d{8,12}$";
            return VerifieRegExp(pattern, chaine);
        }


        public static long ToUnixTimeSeconds(DateTime utc) => (long)Math.Floor((utc - DateTime.UnixEpoch).TotalSeconds);


        public static bool VerifieRegExp(string pattern, string chaine)
        {
            try
            {
                bool isValid = Regex.IsMatch(chaine, pattern);
                return isValid;
            }
            catch (Exception ex)

            { return false; }

        }

      
        public static JsonDocument ConvertObjectToJsonDocument<T>(T obj)
        {
            // Sérialiser l'objet en chaîne JSON
            string jsonString = JsonSerializer.Serialize(obj);

            // Créer un JsonDocument à partir de la chaîne JSON
            JsonDocument jsonDocument = JsonDocument.Parse(jsonString);

            return jsonDocument;
        }

       

        public static bool RetourIsSucces(int codeRetour)
        {
            return (codeRetour.ToString().Substring(0, 1) == "2");
        }
        public static bool RetourIsSuccesTransfert(int codeRetour)
        {
            return (codeRetour.ToString() == "200");
        }

    }

}
