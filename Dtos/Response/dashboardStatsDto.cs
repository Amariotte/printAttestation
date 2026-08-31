namespace print_attestation.Dtos.Response
{
    public class dashboardStatsDto
    {
        public int croissance { get; set; } = 0;
        public downloadStatsDto telechargements { get; set; } = new downloadStatsDto();
        public searchStatsDto search { get; set; } = new searchStatsDto();
        public usersStatsDto users { get; set; } = new usersStatsDto();
        public jobStatsDto jobs { get; set; } = new jobStatsDto();
        public int attestationsEvolution { get; set; } = 0;
        public int totalEvolution { get; set; } = 0;
        public int totalEvconnexionsolution { get; set; } = 0;


  }

    public class downloadStatsDto
    {
        public int total { get; set; } = 0;
        public int mois { get; set; } = 0;

    }


    public class usersStatsDto
    {
        public int total { get; set; } = 0;
        public int actives { get; set; } = 0;
    
    }


    public class jobStatsDto
    {
     
        public int total { get; set; } = 0;
        public int succes { get; set; } = 0;
        public int failed { get; set; } = 0;
        public int pending { get; set; } = 0;
        public int cancelled { get; set; } = 0;
        public int mois { get; set; } = 0;
        public int previousMois { get; set; } = 0;
        public int croissance { get; set; } = 0;

    }

   

    public class searchStatsDto
    {

        public int total { get; set; } = 0;
        public int mois { get; set; } = 0;
        public int previousMois { get; set; } = 0;
        public int croissance { get; set; } = 0;

    }








}

