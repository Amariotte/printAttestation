namespace print_attestation.Dtos.Reponses
{
  

    public class MetaDto
    {
        public int? total { get; set; }
        public int? prevPage { get; set; }
        public int? nextPage { get; set; }
        public int? currentPage { get; set; }
        public int? limit { get; set; }
        public int? totalPages { get; set; }
  

       public static MetaDto Create(int total, int page, int limit)
        {
            var totalPages = (int)Math.Ceiling(total / (double)limit);

            return new MetaDto
            {
                total = total,
                currentPage = page,
                limit = limit,
                totalPages = totalPages,
                prevPage = page > 1 ? page - 1 : 1,
                nextPage = page < totalPages ? page + 1 : totalPages
            };
        }
    }
    }