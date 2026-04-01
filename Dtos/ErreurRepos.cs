namespace ask.ResponseDto
{
    public class ErreurRepos<T> where T : class
    {
        public string Code { get; set; }
        public bool actionresult { get; set; }
        public string descriptionResult { get; set; }
        public T? data { get; set; }

    }
}
