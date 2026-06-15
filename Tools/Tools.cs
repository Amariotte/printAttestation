using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;


namespace ask.Tools
{
    public static class Tools
    {

   
 
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

        /// <summary>
        /// Génère un mot de passe aléatoire sécurisé
        /// </summary>
        /// <param name="length">Longueur du mot de passe (minimum 8)</param>
        /// <param name="includeUppercase">Inclure des lettres majuscules</param>
        /// <param name="includeLowercase">Inclure des lettres minuscules</param>
        /// <param name="includeNumbers">Inclure des chiffres</param>
        /// <param name="includeSpecialChars">Inclure des caractères spéciaux</param>
        /// <returns>Mot de passe généré</returns>
        public static string GeneratePassword(
            int length = 12,
            bool includeUppercase = true,
            bool includeLowercase = true,
            bool includeNumbers = true,
            bool includeSpecialChars = true)
        {
            if (length < 8)
                throw new ArgumentException("La longueur du mot de passe doit être au minimum de 8 caractères.", nameof(length));

            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string numbers = "0123456789";
            const string specialChars = "!@#$%^&*()-_=+[]{}|;:,.<>?";

            StringBuilder characterSet = new StringBuilder();
            StringBuilder password = new StringBuilder();

            // Construction du jeu de caractères disponibles
            if (includeUppercase) characterSet.Append(uppercase);
            if (includeLowercase) characterSet.Append(lowercase);
            if (includeNumbers) characterSet.Append(numbers);
            if (includeSpecialChars) characterSet.Append(specialChars);

            if (characterSet.Length == 0)
                throw new ArgumentException("Au moins un type de caractère doit être inclus.");

            // Garantir qu'au moins un caractère de chaque type requis est présent
            List<char> guaranteedChars = new List<char>();
            if (includeUppercase) guaranteedChars.Add(uppercase[RandomNumberGenerator.GetInt32(uppercase.Length)]);
            if (includeLowercase) guaranteedChars.Add(lowercase[RandomNumberGenerator.GetInt32(lowercase.Length)]);
            if (includeNumbers) guaranteedChars.Add(numbers[RandomNumberGenerator.GetInt32(numbers.Length)]);
            if (includeSpecialChars) guaranteedChars.Add(specialChars[RandomNumberGenerator.GetInt32(specialChars.Length)]);

            // Remplir le reste du mot de passe
            int remainingLength = length - guaranteedChars.Count;
            for (int i = 0; i < remainingLength; i++)
            {
                password.Append(characterSet[RandomNumberGenerator.GetInt32(characterSet.Length)]);
            }

            // Ajouter les caractères garantis
            foreach (char c in guaranteedChars)
            {
                password.Append(c);
            }

            // Mélanger le mot de passe pour éviter un pattern prévisible
            return ShuffleString(password.ToString());
        }

        /// <summary>
        /// Mélange aléatoirement les caractères d'une chaîne
        /// </summary>
        private static string ShuffleString(string input)
        {
            char[] array = input.ToCharArray();
            int n = array.Length;

            for (int i = n - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                // Échange
                char temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }

            return new string(array);
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

            // Autorise lettres, chiffres, espaces, -, _, /, ., (, )
            if (!System.Text.RegularExpressions.Regex.IsMatch(
                    key,
                    @"^[a-zA-Z0-9\-_ /\.()]+$"))
                return false;

            string[] sqlKeywords ={"DROP","DELETE","INSERT", "UPDATE", "ALTER","EXEC","EXECUTE","UNION","SELECT","SCRIPT","JAVASCRIPT"};

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
