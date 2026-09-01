using System.Data;
using System.Security.Cryptography;
using System.Text;
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
                actif = (u.r_statut == STATUT_USER.ACTIVE),
                sites = new int?[] { u.r_site_id_fk },
                roles = u.r_user_roles != null ? u.r_user_roles.Where(ur => ur.r_role != null).Select(ur => ur.r_role!.r_code).ToArray() : null
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


        public static Task<List<int>> returnUserSiteTypeIds(t_user user)
        {
            try
            {
                if (user == null)
                    return Task.FromResult(new List<int>());

                var siteTypeIds = user.r_sites_types
                    .Select(siteType => (int)siteType)
                    .ToList();

                if (user.r_user_roles != null)
                {
                    siteTypeIds.AddRange(
                        user.r_user_roles
                            .Where(ur =>
                                ur.r_is_active == true &&
                                ur.r_role != null &&
                                ur.r_role.r_is_active == true)
                            .SelectMany(ur => ur.r_role!.r_sites_types)
                            .Select(siteType => (int)siteType)
                    );
                }

                return Task.FromResult(siteTypeIds
                    .Distinct()
                    .ToList());
            }
            catch (Exception)
            {
                return Task.FromResult(new List<int>());
            }
        }


        public static string EquivalenceTypeSite(TYPE_SITE? t)
        {
            switch (t)
            {
                case TYPE_SITE.SIEGE:
                    return "Siège";
                case TYPE_SITE.AGENT_GENERAL:
                    return "Agent général";
                case TYPE_SITE.BUREAU_DIRECT:
                    return "Bureau direct";
                case TYPE_SITE.COURTTIER:
                    return "Courtier";
                case TYPE_SITE.AUTRES:
                    return "Autres";
                default:
                    return "Inconnu";
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
                user = d.r_user != null ? BuildUserToUserResponseDto(d.r_user) : null
            };
        }


        public static RoleResponseDto BuildRoleToRoleResponseDto(t_role? r)
        {

            if (r == null)
                return null;

            return new RoleResponseDto
            {
                id = r.r_id,
                nom = r.r_nom,
                code = r.r_code,
                description = r.r_description,
                scopes = r.r_role_scopes != null ? r.r_role_scopes.Select(rs => BuildScopeToScopeResponseDto(rs.r_scope)).ToArray() : null,
                siteTypes = r.r_sites_types != null ? r.r_sites_types.Select(st => BuildSiteTypeToSiteTypeResponseDto(st)).ToArray() : null,
            };
        }

        public static ScopeResponseDto BuildScopeToScopeResponseDto(t_scope? s)
        {

            if (s == null)
                return null;

            return new ScopeResponseDto
            {
                nom = s.r_nom,
                code = s.r_code,
                description = s.r_description,
               
            };
        }


        public static siteTypeResponseDto BuildSiteTypeToSiteTypeResponseDto(TYPE_SITE? t)
        {

            if (t == null)
                return null;

            return new siteTypeResponseDto
            {
                id = (int)t,
                libelle = EquivalenceTypeSite(t)
            };
        }



    }

}
