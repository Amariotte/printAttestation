using ask.Model;

public class ZipJobManager
{
    private readonly Dictionary<string, t_job> jobs = new();


    public t_job Create(List<string> nums)
    {
        var job = new t_job
        {
            r_attestations = nums,
            r_total = nums.Count,
            r_job_id = Guid.NewGuid().ToString(),
        };


        jobs[job.r_job_id] = job;

        return job;
    }


    public t_job? Get(string id)
    {
        jobs.TryGetValue(id, out var job);

        return job;
    }
}