
using ask.ContextDb;
using ask.Model;
using ask.ResponseDto;
using Microsoft.EntityFrameworkCore;


namespace ask.Implementation
{
    public class BaseRepo<T> : IbaseRepo<T> where T : t_base
    {
        protected readonly askContext _context;
        protected readonly DbSet<T> _dbSet;

        public BaseRepo(askContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }
        public async Task<ErreurRepos<T>> GetByIdAsync(int id)
        {
            try
            {
                return new ErreurRepos<T>
                {
                    actionresult = true,
                    Code = "SCS003",
                    descriptionResult = $"ENREGISTREMENT {typeof(T)}",
                    data = await _dbSet.Where(p => p.r_id == id && p.r_is_delete == false).FirstOrDefaultAsync(),
                };
            }
            catch (Exception ex)
            {
                return new ErreurRepos<T> { actionresult = false, descriptionResult = "Exception systeme veuillez contacter l'administrateur", data = null, Code = "Ecc001" };
            }
        }
        public async Task <ErreurRepos<IEnumerable<T>>> GetAllAsync()
        {
            try
            {
                return new ErreurRepos<IEnumerable<T>>
                {
                    actionresult = true,
                    Code = "SCS003",
                    descriptionResult = $"LISTE DES {typeof(T)}",
                    data = await _dbSet.Where(p => p.r_is_delete != true).ToListAsync(),
                };
            }
            catch (Exception ex)
            {
                return new ErreurRepos<IEnumerable<T>> { actionresult = false, descriptionResult = "Exception systeme veuillez contacter l'administrateur", data = null, Code = "Ecc001" };
            }
        }
        public async Task<ErreurRepos<T>> AddAsync(T entity)
        {
            try
            {
                entity.r_created_at = DateTime.Now;


                _dbSet.Add(entity);
                await _context.SaveChangesAsync();
                return new ErreurRepos<T>
                {
                    actionresult = true,
                    Code = "SCS005",
                    descriptionResult = "Ajout effectué avec succés",
                    data = null,
                };
            }
            catch (Exception ex)
            {
                return new ErreurRepos<T> { actionresult = false, descriptionResult = "Exception systeme veuillez contacter l'administrateur", data = null, Code = "Ecc001" };
            }

        }
        public async Task<ErreurRepos<T>> UpdateAsync(T entity)
        {
            try
            {
                entity.r_updated_at = DateTime.Now;

                _dbSet.Update(entity);
                await _context.SaveChangesAsync();
                return new ErreurRepos<T>
                {
                    actionresult = true,
                    Code = "SCS004",
                    descriptionResult = "Mise à jour effectuée avec succes",
                    data = null,
                };
            }
            catch (Exception ex)
            {
                return new ErreurRepos<T> { actionresult = false, descriptionResult = "Exception systeme veuillez contacter l'administrateur", data = null, Code = "Ecc001" };
            }

        }
        public async Task<ErreurRepos<T>> RemoveAsync(int id)
        {
            try
            {
                var ret = _dbSet.Where(p => p.r_id == id).FirstOrDefaultAsync().Result;
                if (ret == null)
                {
                    return new ErreurRepos<T>
                    {
                        actionresult = false,
                        Code = "Err002",
                        descriptionResult = "Entité inexistante",
                        data = null,
                    };
                }
                ret.r_is_delete = true;
                _dbSet.Update(ret);
                await _context.SaveChangesAsync();
                return new ErreurRepos<T>
                {
                    actionresult = true,
                    Code = "SCS006",
                    descriptionResult = "Suppression effectué avec succes",
                    data = null,
                };
            }
            catch (Exception ex)
            {
                return new ErreurRepos<T> { actionresult = false, descriptionResult = "Exception systeme veuillez contacter l'administrateur", data = null, Code = "Ecc001" };
            }

        }
        public async Task<ErreurRepos<T>> DesactiveOrActiveAsync(int id,bool _action)
        {
            try
            {
                var ret = await _dbSet.Where(p => p.r_id == id).FirstOrDefaultAsync();
                if (ret != null)
                {
                    ret.r_is_active = _action;
                    await _context.SaveChangesAsync();
                    return new ErreurRepos<T>
                    {
                        actionresult = true,
                        Code = "SCS003",
                        descriptionResult = "Operation effectuée",
                        data = null,
                    };
                }
                else
                {
                    return new ErreurRepos<T>
                    {
                        actionresult = false,
                        Code = "Err005",
                        descriptionResult = "Donnée inexistante",
                        data = null,
                    };
                }
            }
            catch (Exception ex)
            {
                return new ErreurRepos<T> { actionresult = false, descriptionResult = "Exception systeme veuillez contacter l'administrateur", data = null, Code = "Ecc001" };
            }
        }



        public async Task<ErreurRepos<IEnumerable<T>>> AddRangeAsync(IEnumerable<T> entities)
        {
            try
            {
                // Matérialiser une seule fois
                var list = entities?.ToList() ?? new List<T>();
                if (list.Count == 0)
                {
                    return new ErreurRepos<IEnumerable<T>>
                    {
                        actionresult = true,
                        Code = "SCS000",
                        descriptionResult = "Aucune entité à insérer.",
                        data = list
                    };
                }

                // Pré-traitements communs
                var now = DateTime.UtcNow;
                foreach (var entity in list)
                {
                    // r_createdon si dispo
                    var propCreated = entity.GetType().GetProperty("r_created_at");
                    if (propCreated != null && propCreated.CanWrite && propCreated.PropertyType == typeof(DateTime))
                        propCreated.SetValue(entity, now);

                }

                _dbSet.AddRange(list);
                await _context.SaveChangesAsync();

                return new ErreurRepos<IEnumerable<T>>
                {
                    actionresult = true,
                    Code = "SCS006",
                    descriptionResult = $"Ajout de {list.Count} entité(s) effectué avec succès.",
                    data = list
                };
            }
            catch (Exception)
            {
                return new ErreurRepos<IEnumerable<T>>
                {
                    actionresult = false,
                    Code = "Ecc001",
                    descriptionResult = "Exception système, veuillez contacter l'administrateur.",
                    data = null
                };
            }
        }
    }
}
