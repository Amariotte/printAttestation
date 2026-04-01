using System.Text.Json;

namespace InteroperabiliteProject
{
    public class AliasEvent
    {

        public delegate Task MessageEventHandler(string message);
        public event MessageEventHandler ReturnAlias;

        public async Task TriggerReturnAliasAsync(JsonDocument message)
        {
            if (ReturnAlias != null)
            {
                await ReturnAlias.Invoke(message.ToString());
            }
        }
    }
}

