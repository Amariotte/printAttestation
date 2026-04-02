using System.Security.Cryptography;
using System.Text;

namespace ask.Tools
{
    public static class HMACHelper
    {
        // Méthode pour générer la signature HMAC d'un événement
        public static string GenerateHMAC(string message, string secretKey)
        {
            // Convertir la clé secrète en tableau d'octets
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);

            // Convertir le message (événement) en tableau d'octets
            var messageBytes = Encoding.UTF8.GetBytes(message);

            // Utiliser HMAC avec SHA256
            using (var hmacsha256 = new HMACSHA256(keyBytes))
            {
                // Calculer le HMAC
                var hashMessage = hmacsha256.ComputeHash(messageBytes);

                // Convertir le HMAC en chaîne hexadécimale
                return BitConverter.ToString(hashMessage).Replace("-", "").ToLower();
            }
        }



        // Méthode pour vérifier la signature HMAC
        public static bool ValidateHMAC(string message, string providedHMAC, string secretKey)
        {
            // Générer le HMAC pour comparer
            string generatedHMAC = HMACHelper.GenerateHMAC(message, secretKey);

            // Comparer les deux HMAC
            return generatedHMAC.Equals(providedHMAC, StringComparison.InvariantCultureIgnoreCase);
        }

        public static void SendEvent(string eventMessage, string secretKey)
        {
            // Générer la signature HMAC de l'événement
            string hmacSignature = HMACHelper.GenerateHMAC(eventMessage, secretKey);

            // Envoyer l'événement avec la signature (exemple simplifié)
            Console.WriteLine($"Message: {eventMessage}");
            Console.WriteLine($"Signature: {hmacSignature}");

            // Par exemple, envoyer l'événement via une API ou un webhook
            // Avec l'événement et la signature HMAC incluse dans les en-têtes ou dans le message lui-même
        }

        public static void ReceiveEvent(string eventMessage, string receivedHMAC, string secretKey)
        {
            // Valider la signature HMAC reçue
            bool isValid = ValidateHMAC(eventMessage, receivedHMAC, secretKey);

            if (isValid)
            {
                Console.WriteLine("Événement valide et authentique.");
                // Traiter l'événement ici
            }
            else
            {
                Console.WriteLine("Événement modifié ou signature invalide.");
            }
        }

        //****************GENERER SHA-256*******************
        public static string GenerateRandomKey(int keySize = 32)  // keySize en octets, 32 octets = 256 bits
        {
            // Générer des octets aléatoires cryptographiquement sécurisés
            byte[] keyBytes = new byte[keySize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(keyBytes);  // Remplir le tableau avec des octets aléatoires
            }

            // Convertir les octets en chaîne hexadécimale
            return BitConverter.ToString(keyBytes).Replace("-", "").ToLower();
        }
    }
}
