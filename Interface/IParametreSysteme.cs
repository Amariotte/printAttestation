using InteroperabiliteProject.Model;

namespace InteroperabiliteProject.Interface
{
    public interface IParametreSystemeRepo : IbaseRepo<t_parametre_systeme>
    {
        public Task<string> GetValeur(string cle, string? tag);




    }
}
