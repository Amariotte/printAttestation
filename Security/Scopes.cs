namespace print_attestation.Security
{

    public static class Scopes
    {
        // =========================
        // Attestations
        // =========================

        public const string AttestationsRead = "attestations.read";
        public const string AttestationsReadAll = "attestations.read.all";


        // =========================
        // Tâches
        // =========================

        public const string TachesRead = "taches.read";
        public const string TachesReadSite = "taches.read.site";

        public const string TachesCreate = "taches.create";
        public const string TachesUpdate = "taches.update";
        public const string TachesUpdateSite = "taches.update.site";
        public const string TachesUpdateAll = "taches.update.all";


        // =========================
        // Demandes d'annulation
        // =========================

        public const string DemandesAnnulationsRead =
            "demandes-annulations.read";
        public const string DemandesAnnulationsReadSite =
           "demandes-annulations.read.site";

        public const string DemandesAnnulationsCreate =
            "demandes-annulations.create";

        public const string DemandesAnnulationsUpdate =
            "demandes-annulations.update";

        public const string DemandesAnnulationsDelete =
            "demandes-annulations.delete";


        // =========================
        // Utilisateurs
        // =========================

        public const string UsersRead = "users.read";
        public const string UsersReadSite = "users.read.site";

        public const string UsersCreate = "users.create";
        public const string UsersUpdate = "users.update";
        public const string UsersDelete = "users.delete";


        // =========================
        // Sites
        // =========================

        public const string SitesRead = "sites.read";
        public const string SitesCreate = "sites.create";
        public const string SitesUpdate = "sites.update";
        public const string SitesDelete = "sites.delete";
        public const string SitesUpload = "sites.upload";


        // =========================
        // Roles
        // =========================

        public const string RolesRead = "roles.read";
        public const string RolesCreate = "roles.create";
        public const string RolesUpdate = "roles.update";
        public const string RolesDelete = "roles.delete";

        // =========================
        // Motifs d'annulation
        // =========================

        public const string MotifAnnulationRead =
            "motifs-annulation.read";
        public const string MotifAnnulationCreate =
            "motifs-annulation.create";
        public const string MotifAnnulationUpdate =
            "motifs-annulation.update";
        public const string MotifAnnulationDelete =
            "motifs-annulation.delete";

        // =========================
        // Audits - Actions
        // =========================

        public const string AuditsActionsRead =
            "audits.actions.read";

        public const string AuditsActionsReadSite =
                "audits.actions.read.site";




        // =========================
        // Audits - Accès
        // =========================

        public const string AuditsAccesRead =
            "audits.acces.read";
        public const string AuditsAccesReadSite =
           "audits.acces.read.site";

    }
    }
