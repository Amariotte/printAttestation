namespace ask.Dtos.General
{
    /// <summary>
    /// Paramètres de pagination pour les requêtes
    /// </summary>
    public class PaginationParams
    {
        private int _page = 1;
        private int _limit = 10;

        /// <summary>
        /// Numéro de la page (commence à 1)
        /// </summary>
        public int page
        {
            get => _page;
            set => _page = value < 1 ? 1 : value;
        }

        /// <summary>
        /// Nombre d'éléments par page (entre 1 et 100)
        /// </summary>
        public int limit
        {
            get => _limit;
            set => _limit = value < 1 ? 10 : (value > 100 ? 100 : value);
        }

        /// <summary>
        /// Calcule le nombre d'éléments à ignorer (skip)
        /// </summary>
        public int Skip => (page - 1) * limit;

        /// <summary>
        /// Retourne le nombre d'éléments à prendre (take)
        /// </summary>
        public int Take => limit;

        public PaginationParams()
        {
        }

        public PaginationParams(int page, int limit)
        {
            this.page = page;
            this.limit = limit;
        }
    }
}
