using InteroperabiliteProject.Model;
using InteroperabiliteProject.RequestToReceiveDto;

namespace InteroperabiliteProject.Interface
{
    public interface IParticipantsRepo : IbaseRepo<t_participant>
    {
      
        public Task<t_participant> searchParticipant( string codeMembre);

        public Task<List<t_participant>> getAll();

    }
}
