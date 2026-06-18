using print_attestation.Dtos.Reponses;

namespace ask.Dtos.General
{
    /// <summary>
    /// Réponse standardisée pour les listes paginées
    /// </summary>
    public class PaginatedResponse<T>
    {
        public List<T>? data { get; set; }
        public MetaDto? meta { get; set; }

        public PaginatedResponse()
        {
        }

        public PaginatedResponse(List<T> data, int total, int page, int limit)
        {
            this.data = data;
            this.meta = MetaDto.Create(total, page, limit);
        }

        /// <summary>
        /// Crée une réponse paginée
        /// </summary>
        public static PaginatedResponse<T> Create(List<T> data, int total, int page, int limit)
        {
            return new PaginatedResponse<T>(data, total, page, limit);
        }
    }
}
