using print_attestation.Model;

namespace print_attestation.Interface
{
    public interface ImodeleRepo : IbaseRepo<t_modele>
    {


        public Task<List<t_modele>> GetModelesByType( TYPE_MODELE type);

    }
}
