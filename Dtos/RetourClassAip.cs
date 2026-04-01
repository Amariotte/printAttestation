namespace InteroperabiliteProject.Dtos
{
    public class RetourClassAip
    {
        public bool operationResult { get; set; } = false;
        public string messageResult { get; set; }
        public string? idoperation { get; set; } = null;
        public int? _statuscode { get; set; }
        public string? erreur { get; set; }
        public override string ToString()
        {
            return $"retour de RetourClassAIP =====================> boolen {operationResult} messageresult {messageResult} idoperation {idoperation}";
        }
    }
    
}
