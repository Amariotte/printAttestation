namespace ask.Dtos.RequestToReceiveDto
{
    public class CreateAliasRequestFromAIP
    {
        public string idCreationAlias { get; set; }
        public string alias { get; set; }
        public string shid { get; set; }
        public string type { get; set; }
        public string statut { get; set; }
        
    }

    public class CreateAliasRequestFromAIPSUCCESS:CreateAliasRequestFromAIP
    {
        public DateTime dateCreation { get; set; }
    }
    public class CreateAliasRequestFromAIPECHEC : CreateAliasRequestFromAIP
    {
        public string raisonRejet { get; set; }
        public string informationsAdditionnelles { get; set; }

    }
}
