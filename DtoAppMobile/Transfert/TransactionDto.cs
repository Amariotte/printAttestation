
namespace InteroperabiliteProject.DtoAppMobile
{

    public class TransactionDto
    {
        /// <summary>Numéro de compte du client connecté (obligatoire).</summary>
        public string compte { get; set; }

        /// <summary>Alias de compte du client connecté.</summary>
        public string? alias { get; set; }

        /// <summary>Canal de communication utilisé pour la transaction.</summary>
        /// <remarks>Exemples : "731", "633", "999", "000", "400", "631", "500", "521", "520", "401"</remarks>
        public string canal { get; set; }

        /// <summary>Montant de la transaction (obligatoire, ≥ 1).</summary>
        public double montant { get; set; }

        /// <summary>Montant des frais appliqués.</summary>
        public double? montantFrais { get; set; }

        /// <summary>Identifiant unique end-to-end (obligatoire).</summary>
        public string endToEndId { get; set; }

        /// <summary>Identifiant technique de la transaction.</summary>
        public string? txId { get; set; }

        /// <summary>Sens de la transaction (obligatoire).</summary>
        /// <remarks>Valeurs possibles : "debit", "credit"</remarks>
        public string sens { get; set; } 

        /// <summary>Motif de l'opération.</summary>
        public string? motif { get; set; }

        /// <summary>Nom du client concerné (obligatoire).</summary>
        public string clientNom { get; set; }

        /// <summary>Code pays du client (obligatoire).</summary>
        public string clientPays { get; set; }

        /// <summary>Identifiant PSP du client.</summary>
        public string? clientPSP { get; set; }

        /// <summary>Nom du PSP du client.</summary>
        public string? clientPSPNom { get; set; }

        /// <summary>URL de la photo du client.</summary>
        public string? clientPhoto { get; set; }

        /// <summary>Compte du client bénéficiaire.</summary>
        public string? clientCompte { get; set; }

        /// <summary>Alias du client bénéficiaire.</summary>
        public string? clientAlias { get; set; }

        /// <summary>Date d'irrévocabilité de la transaction.</summary>
        public DateTime? dateOperation { get; set; }

        /// <summary>Statut courant de la transaction.</summary>
        /// <remarks>Valeurs : "irrevocable", "rejete", "initie", "desactive"</remarks>
        public string? statut { get; set; }

        /// <summary>Raison du statut actuel.</summary>
        public string? statutRaison { get; set; }

        /// <summary>Référence de facture associée.</summary>
        public string? facture { get; set; }

        /// <summary>Date de début pour les paiements programmés.</summary>
        public DateTime? dateDebut { get; set; }

        /// <summary>Date de fin pour les paiements programmés.</summary>
        public DateTime? dateFin { get; set; }

        /// <summary>Fréquence de paiement.</summary>
        /// <remarks>Valeurs : "J", "S", "M", "A"</remarks>
        public string? frequence { get; set; }

        /// <summary>Périodicité en nombre de jours/mois (≥ 2).</summary>
        public int? periodicite { get; set; }

        /// <summary>ID d'abonnement pour paiements récurrents.</summary>
        public string? subscriptionId { get; set; }

        /// <summary>Date de retour de fonds.</summary>
        public DateTime? retourDate { get; set; }

        /// <summary>Statut du retour de fonds.</summary>
        /// <remarks>Valeurs : "irrevocable", "rejete", "initie", "desactive"</remarks>
        public string? retourStatut { get; set; }

        /// <summary>Raison du statut de retour.</summary>
        public string? retourStatutRaison { get; set; }

        /// <summary>Raison d'annulation d'un transfert émis.</summary>
        /// <remarks>Valeurs : "AC03", "AM09", "SVNR", "DUPL", "FRAD"</remarks>
        public string? annulationRaison { get; set; }

        /// <summary>Date d'annulation.</summary>
        public DateTime? annulationDate { get; set; }

        /// <summary>Statut de la demande d'annulation.</summary>
        /// <remarks>Valeurs : "irrevocable", "rejete", "initie", "desactive"</remarks>
        public string? annulationStatut { get; set; }

        /// <summary>Raison du statut d'annulation.</summary>
        public string? annulationStatutRaison { get; set; }

        /// <summary>Date de la demande de paiement.</summary>
        public DateTime? dateDemande { get; set; }

        /// <summary>Date limite de réponse.</summary>
        public DateTime? dateReponse { get; set; }

        /// <summary>Date d'expiration de la demande.</summary>
        public DateTime? dateExpiration { get; set; }

        /// <summary>Montant de remise appliquée.</summary>
        public double? remise { get; set; }

        /// <summary>Montant d'achat pour retrait PICO.</summary>
        public double? retraitAchat { get; set; }

        /// <summary>Montant net du retrait.</summary>
        public double? retraitMontant { get; set; }

        /// <summary>Frais appliqués au retrait.</summary>
        public double? retraitFrais { get; set; }

        /// <summary>Indicateur de débit différé.</summary>
        public bool? differe { get; set; }

        /// <summary>Fréquence de paiement différé.</summary>
        /// <remarks>Valeurs : "J", "S", "M", "A"</remarks>
        public string? differeFrequence { get; set; }

        /// <summary>Nombre d'occurrences différées.</summary>
        public int? differeOccurence { get; set; }

        /// <summary>Montant différé.</summary>
        public double? differeMontant { get; set; }


        public string? clientPaysNom { get; set; }
    }

}
