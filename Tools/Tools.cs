using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Azure.Core;
using InteroperabiliteProject.Dtos;
using QRCoder;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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


        public static string FormatListeOu(IEnumerable<string?> valeurs)
        {
            var items = valeurs
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return items.Length switch
            {
                0 => "aucune valeur autorisée",
                1 => items[0],
                2 => $"{items[0]} ou {items[1]}",
                _ => $"{string.Join(", ", items[..^1])} ou {items[^1]}"
            };
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

        public static string MaskPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return "***";
            if (phone.Length <= 4) return "***";
            return new string('*', phone.Length - 2) + phone[^2..];
        }

        public static string SupprimerEspaces(string numero)
        {
            // Remplace les espaces par des chaînes vides
            return numero.Replace(" ", "");
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

        public static byte[] GenerateQrCodeWithLogoOld(string value, string fileName)
        {

            try
            {
                if (string.IsNullOrEmpty(value)) return null;

                string folderPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", "qrcode");
                string logoPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "assets", "images", "pi.png");
                string logoEntreprisePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "assets", "images", "logo.png");



                // Créez le dossier s'il n'existe pas
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Génération du QR code
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                {
                    // Création des données du QR code
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(value, QRCodeGenerator.ECCLevel.Q);

                    // Utilisez BitmapByteQRCode pour générer une image de type bitmap
                    using (BitmapByteQRCode qrCode = new BitmapByteQRCode(qrCodeData))
                    {
                        // Convertir le QR code en image
                        using (Image<Rgba32> qrCodeImage = Image.Load<Rgba32>(new MemoryStream(qrCode.GetGraphic(20))))
                        {
                            // Charger le logo
                            using (Image<Rgba32> logo = Image.Load<Rgba32>(logoPath))
                            {
                                // Calculer la taille et la position du logo
                                int logoSize = qrCodeImage.Width / 5;  // Redimensionner à 1/5 de la taille du QR code
                                int logoPosX = (qrCodeImage.Width - logoSize) / 2;  // Centrer horizontalement
                                int logoPosY = (qrCodeImage.Height - logoSize) / 2; // Centrer verticalement

                                // Redimensionner le logo
                                logo.Mutate(x => x.Resize(logoSize, logoSize));

                                // Créer un cercle blanc légèrement plus grand que le logo
                                int circleSize = logoSize + 50;
                                int circlePosX = (qrCodeImage.Width - circleSize) / 2;
                                int circlePosY = (qrCodeImage.Height - circleSize) / 2;

                                // Ajouter le cercle blanc dans le QR code
                                qrCodeImage.Mutate(x =>
                                {
                                    // Dessiner un cercle blanc
                                    x.Fill(Color.White, new EllipsePolygon(circlePosX + circleSize / 2, circlePosY + circleSize / 2, circleSize / 2));
                                });

                                // Ajouter le logo dans le cercle
                                qrCodeImage.Mutate(x => x.DrawImage(logo, new Point(logoPosX, logoPosY), 1));

                                // Enregistrer l'image dans le dossier spécifié
                                string filePath = System.IO.Path.Combine(folderPath, fileName);
                                qrCodeImage.Save(filePath); // ImageSharp détermine le format à partir de l'extension


                                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);

                                // Supprimer le fichier après lecture
                                System.IO.File.Delete(fileName);

                                // Retourner le chemin du fichier
                                return fileBytes;
                            }
                        }
                    }
                }
            }


            catch (Exception ex)
            {

                throw;

            }

        }



        public static byte[]? GenerateQrCodeWithLogo(string value, string fileName, string? hexDarkColor = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value)) return null;

                // Dossiers / chemins (adaptez si besoin)
                string root = Directory.GetCurrentDirectory();
                string folderPath = System.IO.Path.Combine(root, "wwwroot", "temp", "qrcode");
                string logoPiPath = System.IO.Path.Combine(root, "assets", "images", "pi.png");    // centre
                string logoEntreprisePath = System.IO.Path.Combine(root, "assets", "images", "logo.png");  // bas-gauche (dans le QR)

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                // Couleurs QR
                var dark = ParseHexColorOrDefault(hexDarkColor, new Rgba32(0, 0, 0, 255)); // noir par défaut
                var light = new Rgba32(255, 255, 255, 255);

                // Paramètres de rendu
                const int ppm = 20;           // pixels par module
                const bool drawQuietZones = true;
                const QRCodeGenerator.ECCLevel ecc = QRCodeGenerator.ECCLevel.Q; // bonne tolérance

                // --- Génération du QR (PNG en mémoire) ---
                using var generator = new QRCodeGenerator();
                using var data = generator.CreateQrCode(value, ecc);

                using var pngQr = new PngByteQRCode(data);
                // QRCoder exige System.Drawing.Color pour la colorisation
                var qrBytes = pngQr.GetGraphic(
                    pixelsPerModule: ppm,
                    darkColor: System.Drawing.Color.FromArgb(dark.A, dark.R, dark.G, dark.B),
                    lightColor: System.Drawing.Color.FromArgb(light.A, light.R, light.G, light.B),
                    drawQuietZones: drawQuietZones
                );

                using var qrImg = Image.Load<Rgba32>(qrBytes);

                // --- Logo PI au centre (couleurs inchangées) ---
                if (File.Exists(logoPiPath))
                {
                    using var logoPiSrc = Image.Load<Rgba32>(logoPiPath);
                    int piSize = qrImg.Width / 5;
                    int piX = (qrImg.Width - piSize) / 2;
                    int piY = (qrImg.Height - piSize) / 2;

                    // Resize sans companding ni pré-multiplication (pour éviter toute dérive de couleur)
                    var resizePi = new ResizeOptions
                    {
                        Size = new Size(piSize, piSize),
                        Mode = ResizeMode.Stretch,
                        Sampler = KnownResamplers.NearestNeighbor, // zéro interpolation => couleurs brutes
                        Compand = false,
                        PremultiplyAlpha = false
                    };
                    using var logoPi = logoPiSrc.Clone(ctx => ctx.Resize(resizePi));

                    // Disque blanc sous le logo (contraste)
                    int padding = Math.Max(6, piSize / 12);
                    int circleSize = piSize + padding * 2;
                    int circleX = (qrImg.Width - circleSize) / 2;
                    int circleY = (qrImg.Height - circleSize) / 2;

                    var gopt = new GraphicsOptions
                    {
                        Antialias = true,
                        BlendPercentage = 1f,
                        AlphaCompositionMode = PixelAlphaCompositionMode.SrcOver,
                        ColorBlendingMode = PixelColorBlendingMode.Normal
                    };

                    qrImg.Mutate(x =>
                    {
                        x.Fill(Color.White, new EllipsePolygon(
                            circleX + circleSize / 2f,
                            circleY + circleSize / 2f,
                            circleSize / 2f
                        ));
                        x.DrawImage(logoPi, new Point(piX, piY), gopt); // opacité 100%, aucune teinte
                    });
                }

                // --- Logo entreprise en bas-gauche, DANS le QR (zone sûre) ---
                // On évite le repère (finder) bas-gauche : ~7 modules + séparateur + marge => ~9 modules
                if (File.Exists(logoEntreprisePath))
                {
                    using var logoEntSrc = Image.Load<Rgba32>(logoEntreprisePath);

                    int entSize = Math.Max(24, qrImg.Width / 7);
                    var resizeEnt = new ResizeOptions
                    {
                        Size = new Size(entSize, entSize),
                        Mode = ResizeMode.Stretch,
                        Sampler = KnownResamplers.NearestNeighbor,
                        Compand = false,
                        PremultiplyAlpha = false
                    };
                    using var logoEnt = logoEntSrc.Clone(ctx => ctx.Resize(resizeEnt));

                    int quietZonePx = drawQuietZones ? 4 * ppm : 0;
                    int finderSafe = 9 * ppm; // marge pour éviter le finder + séparateur + marge
                    int bgPad = Math.Max(4, entSize / 12);

                    // Position bas-gauche (à l’intérieur du QR, mais hors finder)
                    int entX = quietZonePx + finderSafe;
                    int entY = qrImg.Height - (quietZonePx + finderSafe) - entSize;

                    // Fond blanc arrondi sous le logo (contraste)
                    var bgRect = new RectangularPolygon(entX - bgPad, entY - bgPad, entSize + 2 * bgPad, entSize + 2 * bgPad)
                                    .Clip(new EllipsePolygon(
                                        entX - bgPad + (entSize + 2 * bgPad) / 2f,
                                        entY - bgPad + (entSize + 2 * bgPad) / 2f,
                                        (entSize + 2 * bgPad) / 2f));

                    var goptEnt = new GraphicsOptions
                    {
                        Antialias = true,
                        BlendPercentage = 1f,
                        AlphaCompositionMode = PixelAlphaCompositionMode.SrcOver,
                        ColorBlendingMode = PixelColorBlendingMode.Normal
                    };

                    qrImg.Mutate(x =>
                    {
                        x.Fill(Color.White, bgRect);
                        x.DrawImage(logoEnt, new Point(entX, entY), goptEnt); // aucune altération de couleur
                    });
                }

                // --- Sauvegarde PNG RGBA 8 bits ---
                string filePath = System.IO.Path.Combine(folderPath, fileName);
                var encoder = new PngEncoder
                {
                    ColorType = PngColorType.RgbWithAlpha,
                    BitDepth = PngBitDepth.Bit8,
                    CompressionLevel = PngCompressionLevel.Level6
                };

                qrImg.Save(filePath, encoder);
                byte[] bytes = File.ReadAllBytes(filePath);
                File.Delete(filePath);
                return bytes;
            }
            catch
            {
                throw;
            }
        }

        private static Rgba32 ParseHexColorOrDefault(string? hex, Rgba32 fallback)
        {
            if (string.IsNullOrWhiteSpace(hex)) return fallback;
            string h = hex.Trim();
            if (h.StartsWith("#")) h = h[1..];

            if (h.Length == 6 &&
                byte.TryParse(h[..2], System.Globalization.NumberStyles.HexNumber, null, out var r) &&
                byte.TryParse(h.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var g) &&
                byte.TryParse(h.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
                return new Rgba32(r, g, b, 255);

            if (h.Length == 8 &&
                byte.TryParse(h[..2], System.Globalization.NumberStyles.HexNumber, null, out var a) &&
                byte.TryParse(h.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var r2) &&
                byte.TryParse(h.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var g2) &&
                byte.TryParse(h.Substring(6, 2), System.Globalization.NumberStyles.HexNumber, null, out var b2))
                return new Rgba32(r2, g2, b2, a);

            return fallback;
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

        public static string GenerateSixDigitCode()
        {
            Random random = new Random();
            int codeNumber = random.Next(100000, 1000000); // Génère un nombre entre 100000 et 999999
            return codeNumber.ToString("D6"); // Formate le nombre avec des zéros devant si nécessaire
        }


        public static JsonDocument ConvertObjectToJsonDocument<T>(T obj)
        {
            // Sérialiser l'objet en chaîne JSON
            string jsonString = JsonSerializer.Serialize(obj);

            // Créer un JsonDocument à partir de la chaîne JSON
            JsonDocument jsonDocument = JsonDocument.Parse(jsonString);

            return jsonDocument;
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
        public static string GenerateAlphaNumeriquevalue(int nbre)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            StringBuilder result = new StringBuilder(nbre);
            Random random = new Random();

            for (int i = 0; i < nbre; i++)
            {
                result.Append(chars[random.Next(chars.Length)]);
            }

            return result.ToString();
        }



        public static bool RetourIsSucces(int codeRetour)
        {
            return (codeRetour.ToString().Substring(0, 1) == "2");
        }
        public static bool RetourIsSuccesTransfert(int codeRetour)
        {
            return (codeRetour.ToString() == "200");
        }

    


        public static (bool, string, QrCodeDto) DecodeCodeQr(string codeQr)
        {

            try
            {
                QrCodeDto o = new QrCodeDto();
                string newChaine = codeQr;

                (newChaine, string Payload_Format_Indicator) = RecupererValeur(newChaine, "00", "02"); // ok
                (newChaine, string Merchant_Account_Information) = RecupererValeur(newChaine, "36", "56"); // ok

                (Merchant_Account_Information, string guid) = RecupererValeur(Merchant_Account_Information, "00", "12");  // ok
                (Merchant_Account_Information, string alias) = RecupererValeurSansTailleFixe(Merchant_Account_Information, "01");  // ok

                if (guid != "int.bceao.pi")
                    return (false, "Code QR non valide", null);


                (newChaine, string Merchant_Category_Code) = RecupererValeur(newChaine, "52", "04"); // ok
                (newChaine, string Transaction_Currency) = RecupererValeur(newChaine, "53", "03"); // ok


                (newChaine, string Transaction_Amount) = RecupererValeurSansTailleFixe(newChaine, "54"); // ok
                                                                                                         //(newChaine, string Transaction_Amount) = RecupererValeur(newChaine, "54", size_Amount); // ok

                (newChaine, string Country_Code) = RecupererValeur(newChaine, "58", "02"); // ok
                (newChaine, string Merchant_Name) = RecupererValeur(newChaine, "59", "01"); // ok
                (newChaine, string Merchant_City) = RecupererValeur(newChaine, "60", "01");  // ok


                (newChaine, string Additional_Data_Field_Template) = RecupererValeurSansTailleFixe(newChaine, "62"); // ok

                (Additional_Data_Field_Template, string TxId) = RecupererValeurSansTailleFixe(Additional_Data_Field_Template, "05"); // ok
                (Additional_Data_Field_Template, string Merchant_Channel) = RecupererValeur(Additional_Data_Field_Template, "11", "03"); // ok
                (newChaine, string CRC16) = RecupererValeur(newChaine, "63", "04"); // ok

                string CodeCrC16 = CalculateCRC16(codeQr.Substring(0, codeQr.Length - 4));

                if (CRC16 != CodeCrC16)
                    return (false, "Code QR non valide", null);

                o.pays = Country_Code;
                o.alias = alias;
                o.canal = Merchant_Channel;
                o.montant = Transaction_Amount;
                o.txId = TxId;

                return (true, "Code QR decodé avec succés", o);

            }
            catch
            {
                throw;
            }

        }


        public static (string, string) RecupererValeur(string chaine, string ID, string size)

        {

            string NewChaine = chaine;
            string valeurARechercher = ID + size;
            int index = chaine.IndexOf(valeurARechercher);

            if (index == -1)
                return (NewChaine, string.Empty);

            int startIndex = index + valeurARechercher.Length;

            if (!int.TryParse(size, out int intSize) || intSize < 0)
                return (NewChaine, string.Empty);


            // Vérifier que la sous-chaîne demandée ne dépasse pas la longueur de codeQr
            if (startIndex + intSize > chaine.Length)
            {
                return ("", chaine.Substring(startIndex));
            }



            // Utiliser Remove pour supprimer la portion de la chaîne
            NewChaine = NewChaine.Remove(0, valeurARechercher.Length + chaine.Substring(startIndex, intSize).Length);

            return (NewChaine, chaine.Substring(startIndex, intSize));

            // Extraire et retourner la sous-chaîne de la taille spécifiée
        }

        public static (string, string) RecupererValeurSansTailleFixe(string chaine, string ID)

        {

            string size = "";
            int index = chaine.IndexOf(ID);
            if (index != -1)
            {
                size = chaine.Substring(ID.Length, 2);
            }

            return RecupererValeur(chaine, ID, size);
        }

        public static string AjouterValeur(string ID, string word)

        {

            if (string.IsNullOrEmpty(word) || string.IsNullOrEmpty(ID)) return string.Empty;

            string size = word.Length.ToString();
            if (size.Length < 2)
                size = "0" + size;

            string ValeurAInserer = ID + size + word;

            return ValeurAInserer;

        }

   

        public static string GenerationQR(string alias, string _code_pays, string canal, string montant, string txId)
        {


            string valQrcode = "";
            string CurrencyVal = "952";
            string guid = "int.bceao.pi";

            valQrcode += AjouterValeur("00", "01"); // Payload Format Indicator
            valQrcode += "3656"; // Merchant Account Information

            valQrcode += AjouterValeur("00", guid);// GUI
            valQrcode += AjouterValeur("01", alias);// Account proxy
            valQrcode += AjouterValeur("52", "0000"); // Merchant Category Code
            valQrcode += AjouterValeur("53", CurrencyVal); // Transaction Currency
            valQrcode += AjouterValeur("54", montant); // Transaction Amount 
            valQrcode += AjouterValeur("58", _code_pays); // Country Code
            valQrcode += AjouterValeur("59", "X"); // Merchant Name
            valQrcode += AjouterValeur("60", "X"); // Merchant City

            string _ref_label = AjouterValeur("05", txId); // Reference Label
            string _canal = AjouterValeur("11", canal); //  Merchant Chanel

            string data_field = _ref_label + _canal;
            valQrcode += AjouterValeur("62", data_field); // Additional Data Field Template


            valQrcode += "6304";
            valQrcode += CalculateCRC16(valQrcode); //  CRC16 

            return valQrcode;
        }


        public static string ConvertirDateTimeEnFormatJson(DateTime? dateTime)
        {

            if (string.IsNullOrEmpty(dateTime.ToString()))
                return "";

            return dateTime?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }


        public static string ConvertirDateEnFormatJson(DateTime? dateTime)
        {

            if (string.IsNullOrEmpty(dateTime.ToString()))
                return "";

            return dateTime?.ToString("yyyy-MM-dd");
        }


        public static DateTime? CalculateNextExecutionDate(
        DateTime oldNextExecution,
        string? freq,
        int? periodicite,
        DateTime? endExecution)
        {
            if (string.IsNullOrWhiteSpace(freq) || periodicite is null || periodicite <= 0)
                return null;

            DateTime next = oldNextExecution;
            DateTime today = DateTime.Today;

            // Si une date de fin existe et que la prochaine exécution est déjà au-delà, stop
            if (endExecution.HasValue && next > endExecution.Value)
                return null;

            // Avance jusqu'à atteindre aujourd'hui ou plus
            while (next < today)
            {
                switch (freq)
                {
                    case "J":
                        next = next.AddDays((int)periodicite);
                        break;
                    case "S":
                        next = next.AddDays(7 * (int)periodicite);
                        break;
                    case "M":
                        next = next.AddMonths((int)periodicite);
                        break;
                    case "A":
                        next = next.AddYears((int)periodicite);
                        break;
                    default:
                        // Fréquence inconnue : renvoie null
                        return null;
                }

                // Si après incrément on dépasse la date de fin -> null
                if (endExecution.HasValue && next > endExecution.Value)
                    return null;
            }

            // Dernier garde-fou si pas passé par la boucle
            if (endExecution.HasValue && next > endExecution.Value)
                return null;

            return next;
        }

     
        public static string CalculateCRC16(string exampleData)
        {
            byte[] data = Encoding.UTF8.GetBytes(exampleData);
            ushort crc = 0xFFFF; // Valeur initiale pour CRC-16/CCITT-FALSE
            const ushort polynomial = 0x1021; // Polynôme utilisé pour CRC-16/CCITT-FALSE

            // Parcours de chaque octet des données
            foreach (byte b in data)
            {
                crc ^= (ushort)(b << 8); // Applique l'octet courant au CRC

                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x8000) != 0)
                    {
                        crc = (ushort)((crc << 1) ^ polynomial);
                    }
                    else
                    {
                        crc <<= 1;
                    }
                }
            }

            // Convertir le résultat CRC en une chaîne hexadécimale de 4 caractères
            return crc.ToString("X4");
        }


        private static readonly Regex PhoneRegex = new Regex(@"^\+(?:223|226|228|229|225|221|227|245)\d{8,12}$", RegexOptions.Compiled);

        public static bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            return PhoneRegex.IsMatch(phoneNumber);
        }

    }

}
