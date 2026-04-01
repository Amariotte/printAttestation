
namespace InteroperabiliteProject.Event
{
    public class TimedHostedService : IHostedService, IDisposable
    {
        private Timer _timer;
        private readonly EventService _event;

        public TimedHostedService()
        {
            _event = new EventService();
        }
        public void Dispose()
        {
            _timer?.Dispose();
        }

        Task IHostedService.StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
            return Task.CompletedTask;
        }

        Task IHostedService.StopAsync(CancellationToken cancellationToken)
        {
            // Arrête le timer
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }



        private void DoWork(object state)
        {
            // Logique de la tâche à exécuter
         //   _event.DeleteAllLongTask();
            // Ajouter ici le code de la tâche à exécuter.
        }
    }
}
