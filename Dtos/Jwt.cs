namespace ask.Dtos
{
   

    public sealed class JwtIssueOptions
    {
        public int UserId { get; set; }
        public string UserEmail { get; set; } = default!;
        public string[] Roles { get; set; } = Array.Empty<string>();
        public int? LifetimeMinutesOverride { get; set; }   
    }
}
