namespace InteroperabiliteProject.DtoAppMobile
{
    public class QueryUpdatePlanificationDto
    {
        public string? dateExecution { get; set; }
        public string? motif { get; set; }

    }


    public class QueryConfirmPlanificationDto
    {
        public bool decision { get; set; }
        public string raison { get; set; }

    }



}