namespace InteroperabiliteProject.DtoAppBusiness
{
    public class CreerWebHookDto
    {
        public string callbackUrl { get; set; }
        public string? alias { get; set; }
        public string?[] events { get; set; }
    }
}
