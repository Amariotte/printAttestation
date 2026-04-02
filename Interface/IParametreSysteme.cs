using ask;
using ask.Model;

namespace ask.Interface
{
    public interface IParametreSystemeRepo : IbaseRepo<t_parametre_systeme>
    {
        public Task<string> GetValeur(string cle, string? tag);




    }
}
