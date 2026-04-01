namespace ask.Dtos.RequestToSendDto
{
    public class AIPDATA
    {

        public string codemembre { get; set; }
        public int? timeOutReponse { get; set; } = 30;
        public string codepays { get; set; }
        public string typeparticipant { get; set; }
        public string BaseUriBusinessApi { get; set; }
        public string BaseUriClientApi { get; set; }
        public string compteProduit { get; set; }
        public string baseUriaifComplet { get; set; }
        public string cleprive { get; set; }
        public string liencertificat { get; set; }
        public string baseUriaif { get; set; }
        public int tailleIban { get; set; }
        public string devise { get; set; } = "XOF";
        public string colorQr { get; set; } = "XOF";
        public bool enabledInternalTransfer { get; set; }
    }
}
