

using ask.Model;
using ask.ResponseDto;

namespace ask
{
    public interface IbaseRepo<T> where T : t_base
    {
        Task<ErreurRepos<T>> GetByIdAsync(int id);
        Task<ErreurRepos<IEnumerable<T>>> GetAllAsync();
        Task<ErreurRepos<T>> AddAsync(T entity);
        Task<ErreurRepos<T>> UpdateAsync(T entity);
        Task<ErreurRepos<T>> RemoveAsync(int id);
        Task<ErreurRepos<T>> DesactiveOrActiveAsync(int id, bool _action);
        Task<ErreurRepos<IEnumerable<T>>> AddRangeAsync(IEnumerable<T> entities);
    }
}
