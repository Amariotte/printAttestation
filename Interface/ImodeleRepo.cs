using ask.Model;

namespace ask.Interface
{
    public interface ImodeleRepo : IbaseRepo<t_modele>
    {


        public Task<List<t_modele>> GetModelesByType( TYPE_MODELE type);

    }
}
