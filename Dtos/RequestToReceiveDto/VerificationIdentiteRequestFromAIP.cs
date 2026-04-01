using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ask.Dtos.RequestToReceiveDto
{
    public class VerificationIdentiteRequestFromAIP
    {
        [Required]
        [RegularExpression(@"^MPIUMOA[a-zA-Z0-9]{28}$")]
        public string msgId { get; set; }

        [Required]
        [RegularExpression(@"^E(CI|SN|ML|BF|BJ|TG|NE|GW)[BCDF]\d{3}\d{14}[a-zA-Z0-9]{14}$")]
        public string endToEndId { get; set; }

        [RegularExpression(@"^(?:CI|SN|ML|GW|BF|NE|BJ|TG)\d{2}(BJ|BF|CI|GW|ML|NE|SN|TG)\d{22}$")]
        public string ibanClient { get; set; }
        public string otherClient { get; set; }

        [Required]
        [RegularExpression(@"(BJ|BF|CI|GW|ML|NE|SN|TG)[BCDEF]{1}[0-9]{3}")]
        public string codeMembreParticipant { get; set; }


    public void Validate()
        {
            if (string.IsNullOrWhiteSpace(msgId))
                throw new ArgumentException("msgId est obligatoire.");

            if (!Regex.IsMatch(msgId, @"^MPIUMOA[a-zA-Z0-9]{28}$"))
                throw new ArgumentException("msgId invalide. Format attendu : ^MPIUMOA[a-zA-Z0-9]{28}$");

            if (string.IsNullOrWhiteSpace(endToEndId))
                throw new ArgumentException("endToEndId est obligatoire.");
            if (!Regex.IsMatch(endToEndId, @"^E(CI|SN|ML|BF|BJ|TG|NE|GW)[BCDF]\d{3}\d{14}[a-zA-Z0-9]{14}$"))
                throw new ArgumentException("endToEndId invalide. Format attendu non respecté.");

            if (string.IsNullOrWhiteSpace(codeMembreParticipant))
                throw new ArgumentException("codeMembreParticipant est obligatoire.");
            if (!Regex.IsMatch(codeMembreParticipant, @"(BJ|BF|CI|GW|ML|NE|SN|TG)[BCDEF]{1}[0-9]{3}"))
                throw new ArgumentException("codeMembreParticipant invalide. Format attendu non respecté.");

            // Vérification : au moins un des deux champs doit être fourni
            if (string.IsNullOrWhiteSpace(ibanClient) && string.IsNullOrWhiteSpace(otherClient))
                throw new ArgumentException("ibanClient ou otherClient doit être renseigné.");

            // Si IBAN présent, le vérifier
            if (!string.IsNullOrWhiteSpace(ibanClient) &&
                !Regex.IsMatch(ibanClient, @"^(?:CI|SN|ML|GW|BF|NE|BJ|TG)\d{2}(BJ|BF|CI|GW|ML|NE|SN|TG)\d{22}$"))
            {
                throw new ArgumentException("ibanClient invalide. Format IBAN UMOA non respecté.");
            }
        }


        //{
        //    if (string.IsNullOrWhiteSpace(msgId) ||
        //        string.IsNullOrWhiteSpace(endToEndId) ||
        //        string.IsNullOrWhiteSpace(codeMembreParticipant) ||
        //         ( 
        //         string.IsNullOrWhiteSpace(ibanClient) || string.IsNullOrWhiteSpace(otherClient)
        //         ))
        //    {
        //        throw new ArgumentException("Champs obligatoires manquants dans VerificationIdentiteRequestFromAIP.");
        //    }
        //}
    }
}
