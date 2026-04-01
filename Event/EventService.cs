using Newtonsoft.Json;

namespace InteroperabiliteProject.Event
{
    public class EventService
    {
        private readonly Dictionary<string, TaskCompletionSource<string>> _requests = new Dictionary<string, TaskCompletionSource<string>>();
        //private readonly Dictionary<string,DateTime>_requestCreation = new Dictionary<string,DateTime>();


        //Cette méthode enregistre une nouvelle requête et retourne un identifiant unique.
        public string RegisterRequest(string id_requette)
        {
            var requestId = id_requette;
            _requests[requestId] = new TaskCompletionSource<string>();
            //_requestCreation[requestId]=DateTime.Now;
            //Task.Run(() => {
            //    Task.Delay(15000).Wait();
            //    _requests.Remove(requestId);
            //    _requestCreation.Remove(requestId);
            //    throw new Exception("Temps d'attente de la requette atteint");
            //});
            return requestId;
        }



        //Cette méthode récupère la tâche associée à une requête en utilisant son identifiant
        public Task<string> GetTasks(string requestId)
        {
            if (_requests.TryGetValue(requestId, out var tcs))
            {
                return tcs.Task;
            }
            
            throw new InvalidOperationException("Request not found.");
            
        }

        //Cette méthode déclenche l'événement pour une requête spécifique et envoie le message
        public void TriggerEvent(string requestId, string message)
        {
            if (_requests.TryGetValue(requestId, out var tcs))
            {
                tcs.TrySetResult(message);
                _requests.Remove(requestId);
                //_requestCreation.Remove(requestId);
            }
            else
            {
               //throw new InvalidOperationException("Request not found.");
            }
        }

        //public async Task DeleteAllLongTask()
        //{
        //    foreach (var date in _requestCreation)
        //    {
        //        TimeSpan difference = DateTime.Now - date.Value;
        //        if(difference.TotalSeconds > 30)
        //        {
        //            _requests.Remove(date.Key);
        //            _requestCreation.Remove(date.Key);
        //        }
        //    }
        //}
    }
}
