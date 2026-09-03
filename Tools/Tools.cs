using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using print_attestation.Dtos.Response;
using print_attestation.Dtos.Response.auth;
using print_attestation.Model;


namespace print_attestation.Tools
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

       

        public static long ToUnixTimeSeconds(DateTime utc) => (long)Math.Floor((utc - DateTime.UnixEpoch).TotalSeconds);



        public static byte[] ConvertBase64ToImageBytes(string base64String)
        {
            if (string.IsNullOrWhiteSpace(base64String))
                throw new ArgumentException("La chaîne Base64 ne peut pas être vide", nameof(base64String));

            // Nettoyer le préfixe data:image si présent
            if (base64String.Contains(","))
            {
                base64String = base64String.Split(',')[1];
            }

            return Convert.FromBase64String(base64String);
        }


        public static bool RetourIsSucces(int codeRetour)
        {
            return (codeRetour.ToString().Substring(0, 1) == "2");
        }


        public static UserResponseDto BuildUserToUserResponseDto(t_user? u)
        {
            if (u == null) return null;

            return new UserResponseDto
            {
                id = u.r_id,
                nom = u.r_nom,
                prenom = u .r_prenom,
                email = u.r_email,
                telephone = u.r_telephone,
                siteId = u.r_site_id_fk,
                actif = (u.r_statut == STATUT_USER.ACTIVE),
                site = u.r_site != null ? BuildSiteToSiteResponseDto(u.r_site) : null,
                roleId = (int?)u.r_type,
                role = u.r_type != null ? EquivalenceTypeUtilisateur(u.r_type) : null
            };
        }


        public static string ReplaceCaracteres(string mot , string[] caracteres)
        {
            if (string.IsNullOrWhiteSpace(mot))
                return mot;

            foreach (var character in caracteres)
            {
                mot = mot.Replace(character, "");
            }

            return mot;
        }

        public static jobReponseDto BuildJobToJobResponseDto(t_job j)
        {
            return new jobReponseDto
            {
                id = j.r_id,
                jobId = j.r_job_id,
                userId = j.r_user_id_fk,
                completedAt = (DateTime?)j.r_completed_at,
                fileName = j.r_file_name,
                createdAt = j.r_created_at,
                type = j.r_type?.ToString(),
                nbTotal = j.r_total,
                nbSuccess = j.r_success,
                nbErrors = j.r_errors,
                status = j.r_status,
                user = j.r_user != null ? BuildUserToUserResponseDto(j.r_user) : null,
                details = j.r_job_details != null ? j.r_job_details.Select(BuildJobDetailResponseDto).ToArray() : null
            };
        }

        public static jobDetailReponseDto BuildJobDetailResponseDto(t_job_details d)
        {
            return new jobDetailReponseDto
            {
                id = d.r_id,
                success = d.r_success,
                numAttestation = d.r_attestation,
                raisonEchec = d.r_desc_error
            };
        }


        
        public static MotifAnnulationResponseDto BuildMotifAnnulationToMotifAnnulationResponseDto(t_motif_annulation m)
        {
            return new MotifAnnulationResponseDto
            {
                id = m.r_id,
                libelle = m.r_libelle
            };
               
        }


        

        public static string EquivalenceTypeSite(TYPE_SITE? t)
        {
            switch (t)
            {
                case TYPE_SITE.SIEGE:
                    return "Siège";
                case TYPE_SITE.BUREAU_DIRECT:
                    return "Bureau direct";
                case TYPE_SITE.BANCASSURANCE:
                    return "Bancassurance";
                case TYPE_SITE.AGENT_GENERAL:
                    return "Agent général";
                case TYPE_SITE.COURTTIER:
                    return "Courtier";
                case TYPE_SITE.AUTRES:
                    return "Autres";
                default:
                    return "Inconnu";
             
            }
        }




        public static siteTypeResponseDto BuildSiteTypeToSiteTypeResponseDto(TYPE_SITE? t)
        {
            return new siteTypeResponseDto
            {
                id = (int)t,
                libelle = t != null ? EquivalenceTypeSite(t) : null
            };
        }

            
        

        public static string EquivalenceTypeUtilisateur(TYPE_UTILISATEUR? t)
        {
            switch (t)
            {
                case TYPE_UTILISATEUR.UTILISATEUR:
                    return "Utilisateur";
                case TYPE_UTILISATEUR.BUREAU_DIRECT:
                    return "Bureau direct";
                case TYPE_UTILISATEUR.RESPONSABLE_RESEAU:
                    return "Responsable réseau";
                case TYPE_UTILISATEUR.RESPONSABLE_INTERMEDIAIRE:
                    return "Responsable intermédiaire";
                case TYPE_UTILISATEUR.ADMINISTRATEUR:
                    return "Administrateur";
               
                default:
                    return "Utilisateur";

            }
        }




        public static SiteResponseDto BuildSiteToSiteResponseDto(t_site? s)
        {

            if (s == null)
                return null;

            return new SiteResponseDto
            {
                id = s.r_id,
                nom = s.r_nom,
                code = s.r_code,
                type = s.r_type,
                typeLibelle = s.r_type != null ? EquivalenceTypeSite(s.r_type) : null

            };
        }


        public static demandeAnnulationResponseDto BuildDemandeAnnulationResponseDto(t_demande_annulation d)
        {
            return new demandeAnnulationResponseDto
            {
                id = d.r_id,
                motifLibelle = d.r_motif_annulation.r_libelle,
                status = d.r_status,
                numAttestation = d.r_num_attestation,
                numImmatriculation = d.r_num_immatriculation,
                createdAt = d.r_created_at,
                dateTraitement = d.r_date_traitement,
                motifId = d.r_motif_annulation.r_id,
                numPolice = d.r_num_police,
                motifRejet = d.r_motif_rejet,
                fichiers = d.r_fichiers != null ? d.r_fichiers.Select(BuildDemandeAnnulationFichierResponseDto).ToList() : null,
                user = d.r_user != null ? BuildUserToUserResponseDto(d.r_user) : null,
                site = d.r_site != null ? BuildSiteToSiteResponseDto(d.r_site) : null
            };
        }



        public static demandeAnnulationFichierResponseDto BuildDemandeAnnulationFichierResponseDto(t_demande_annulation_fichier f)
        {
            return new demandeAnnulationFichierResponseDto
            {
                id = f.r_id,
                nomFichier = f.r_nom_fichier,
                nomFichierSave = f.r_nom_fichier_save,
            };
        }


        [NonAction]
        public static string GetFolderPath(IWebHostEnvironment _env, params string[] segments)
        {
            var webRoot = string.IsNullOrWhiteSpace(_env.WebRootPath)
                ? Path.Combine(_env.ContentRootPath, "wwwroot")
                : _env.WebRootPath;

            var allSegments = new List<string> { webRoot };
            if (segments != null && segments.Length > 0)
                allSegments.AddRange(segments.Where(s => !string.IsNullOrWhiteSpace(s)));

            var folderPath = Path.Combine(allSegments.ToArray());
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            return folderPath;
        }

    }

}
