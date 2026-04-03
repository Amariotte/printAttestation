using ask.ContextDb;
using ask.Dtos.General;
using ask.Dtos.Reponses;
using ask.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ask.Controllers
{
    [Route("api/[controller]")]
    public class adminController : ControllerBase
    {
        private readonly askContext _dbContext;
        private readonly ILogger<adminController> _logger;

        public adminController(askContext dbContext, ILogger<adminController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        #region ========================= DIRECTIONS =========================

        [Authorize]
        [HttpGet("directions")]
        public async Task<IActionResult> GetDirections()
        {
            const string _desc_route = "Liste des directions";

            try
            {
                var respQuery = await _dbContext.t_direction
                    .Where(e => e.r_is_delete != true)
                    .OrderBy(e => e.r_nom)
                    .ToListAsync();

                var directionsDto = respQuery.Select(d => new DirectionDto
                {
                    id = d.r_id,
                    nom = d.r_nom,
                    code = d.r_code
                }).ToList();

                return Ok(new { data = directionsDto, meta = new { total = directionsDto.Count } });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpPost("directions")]
        public async Task<IActionResult> CreateDirection([FromBody] DirectionDto _body)
        {
            const string _desc_route = "Créer une direction";

            try
            {
                List<InvalidParam> invalidParams = new();

                if (string.IsNullOrEmpty(_body.nom))
                    invalidParams.Add(new InvalidParam { name = "nom", reason = "Le nom est requis" });

                if (string.IsNullOrEmpty(_body.code))
                    invalidParams.Add(new InvalidParam { name = "code", reason = "Le code est requis" });

                if (invalidParams.Count > 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Les données ne sont pas conformes", instance: HttpContext.Request.Path, invalidParams: invalidParams));

                var exist = await _dbContext.t_direction
                    .Where(e => e.r_code == _body.code && e.r_is_delete != true)
                    .FirstOrDefaultAsync();

                if (exist != null)
                    return StatusCode(403, GeneraleRetour.BuildForbid(detail: "Le code de la direction existe déjà", instance: HttpContext.Request.Path));

                t_direction entity = new t_direction
                {
                    r_nom = _body.nom,
                    r_code = _body.code,
                };

                _dbContext.t_direction.Add(entity);
                await _dbContext.SaveChangesAsync();

                return Ok(new DirectionDto
                {
                    id = entity.r_id,
                    nom = entity.r_nom,
                    code = entity.r_code,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpPut("directions/{id}")]
        public async Task<IActionResult> UpdateDirection(int id, [FromBody] DirectionDto _body)
        {
            const string _desc_route = "Modifier une direction";

            try
            {
                if (id <= 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant de la direction est manquant", instance: HttpContext.Request.Path));

                var resQuery = await _dbContext.t_direction
                    .Where(e => e.r_id == id && e.r_is_delete != true)
                    .FirstOrDefaultAsync();

                if (resQuery == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "La direction n'existe pas", instance: HttpContext.Request.Path));

                var resQueryExist = await _dbContext.t_direction
                    .Where(e => e.r_code == _body.code && e.r_is_delete != true && e.r_id != id)
                    .FirstOrDefaultAsync();

                if (resQueryExist != null)
                    return StatusCode(403, GeneraleRetour.BuildForbid(detail: "Le code existe déjà pour une autre direction", instance: HttpContext.Request.Path));

                if (!string.IsNullOrEmpty(_body.nom)) resQuery.r_nom = _body.nom;
                if (!string.IsNullOrEmpty(_body.code)) resQuery.r_code = _body.code;

                _dbContext.t_direction.Update(resQuery);
                await _dbContext.SaveChangesAsync();

                return Ok(new DirectionDto
                {
                    id = resQuery.r_id,
                    nom = resQuery.r_nom,
                    code = resQuery.r_code,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpDelete("directions/{id}")]
        public async Task<IActionResult> DeleteDirection(int id)
        {
            const string _desc_route = "Supprimer une direction";

            try
            {
                if (id <= 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant de la direction est manquant", instance: HttpContext.Request.Path));

                var resQuery = await _dbContext.t_direction
                    .Where(e => e.r_id == id && e.r_is_delete != true)
                    .FirstOrDefaultAsync();

                if (resQuery == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "La direction n'existe pas", instance: HttpContext.Request.Path));

                resQuery.r_is_delete = true;
                _dbContext.t_direction.Update(resQuery);
                await _dbContext.SaveChangesAsync();

                return StatusCode(204, "Direction supprimée avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        #endregion

        #region ========================= ENTITES =========================

        [Authorize]
        [HttpGet("entites")]
        public async Task<IActionResult> GetEntites()
        {
            const string _desc_route = "Liste des entités";

            try
            {
                var respQuery = await _dbContext.t_entite
                    .Where(e => e.r_is_delete != true)
                    .OrderBy(e => e.r_nom)
                    .ToListAsync();

                var entitesDto = respQuery.Select(e => new EntiteDto
                {
                    id = e.r_id,
                    nom = e.r_nom,
                    description = e.r_description,
                    code = e.r_code
                }).ToList();

                return Ok(new { data = entitesDto, meta = new { total = entitesDto.Count } });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpPost("entites")]
        public async Task<IActionResult> CreateEntite([FromBody] EntiteDto _body)
        {
            const string _desc_route = "Créer une entité";

            try
            {
                List<InvalidParam> invalidParams = new();

                if (string.IsNullOrEmpty(_body.nom))
                    invalidParams.Add(new InvalidParam { name = "nom", reason = "Le nom est requis" });

                if (string.IsNullOrEmpty(_body.code))
                    invalidParams.Add(new InvalidParam { name = "code", reason = "Le code est requis" });

                if (invalidParams.Count > 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Les données ne sont pas conformes", instance: HttpContext.Request.Path, invalidParams: invalidParams));

                var exist = await _dbContext.t_entite
                    .Where(e => e.r_code == _body.code && e.r_is_delete != true)
                    .FirstOrDefaultAsync();

                if (exist != null)
                    return StatusCode(403, GeneraleRetour.BuildForbid(detail: "Le code de l'entité existe déjà", instance: HttpContext.Request.Path));

                t_entite entity = new t_entite
                {
                    r_nom = _body.nom,
                    r_description = _body.description,
                    r_code = _body.code,
                };

                _dbContext.t_entite.Add(entity);
                await _dbContext.SaveChangesAsync();

                return Ok(new EntiteDto
                {
                    id = entity.r_id,
                    nom = entity.r_nom,
                    description = entity.r_description,
                    code = entity.r_code,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpPut("entites/{id}")]
        public async Task<IActionResult> UpdateEntite(int id, [FromBody] EntiteDto _body)
        {
            const string _desc_route = "Modifier une entité";

            try
            {
                if (id <= 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant de l'entité est manquant", instance: HttpContext.Request.Path));

                var resQuery = await _dbContext.t_entite
                    .Where(e => e.r_id == id && e.r_is_delete != true)
                    .FirstOrDefaultAsync();

                if (resQuery == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "L'entité n'existe pas", instance: HttpContext.Request.Path));

                var resQueryExist = await _dbContext.t_entite
                    .Where(e => e.r_code == _body.code && e.r_is_delete != true && e.r_id != id)
                    .FirstOrDefaultAsync();

                if (resQueryExist != null)
                    return StatusCode(403, GeneraleRetour.BuildForbid(detail: "Le code existe déjà pour une autre entité", instance: HttpContext.Request.Path));

                if (!string.IsNullOrEmpty(_body.nom)) resQuery.r_nom = _body.nom;
                if (!string.IsNullOrEmpty(_body.description)) resQuery.r_description = _body.description;
                if (!string.IsNullOrEmpty(_body.code)) resQuery.r_code = _body.code;

                _dbContext.t_entite.Update(resQuery);
                await _dbContext.SaveChangesAsync();

                return Ok(new EntiteDto
                {
                    id = resQuery.r_id,
                    nom = resQuery.r_nom,
                    description = resQuery.r_description,
                    code = resQuery.r_code,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpDelete("entites/{id}")]
        public async Task<IActionResult> DeleteEntite(int id)
        {
            const string _desc_route = "Supprimer une entité";

            try
            {
                if (id <= 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant de l'entité est manquant", instance: HttpContext.Request.Path));

                var resQuery = await _dbContext.t_entite
                    .Where(e => e.r_id == id && e.r_is_delete != true)
                    .FirstOrDefaultAsync();

                if (resQuery == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "L'entité n'existe pas", instance: HttpContext.Request.Path));

                resQuery.r_is_delete = true;
                _dbContext.t_entite.Update(resQuery);
                await _dbContext.SaveChangesAsync();

                return StatusCode(204, "Entité supprimée avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        #endregion

        #region ========================= EMPLOYES =========================

        [Authorize]
        [HttpGet("employes")]
        public async Task<IActionResult> GetEmployes()
        {
            const string _desc_route = "Liste des employés";

            try
            {
                var respQuery = await _dbContext.t_employe
                    .Include(e => e.r_directionTab)
                    .Where(e => e.r_is_delete != true)
                    .OrderBy(e => e.r_nom)
                    .ToListAsync();

                var employesDto = respQuery.Select(e => new EmployeDto
                {
                    id = e.r_id,
                    nom = e.r_nom,
                    prenom = e.r_prenom,
                    adresse = e.r_adresse,
                    sexe = e.r_sexe,
                    nationalite = e.nationalite,
                    telephone = e.r_telephone,
                    dateNaissance = e.r_date_naiss,
                    villeNaissance = e.r_ville_naiss,
                    directionId = e.r_direction_FK,
                    directionNom = e.r_directionTab?.r_nom
                }).ToList();

                return Ok(new { data = employesDto, meta = new { total = employesDto.Count } });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpPost("employes")]
        public async Task<IActionResult> CreateEmploye([FromBody] EmployeDto _body)
        {
            const string _desc_route = "Créer un employé";

            try
            {
                List<InvalidParam> invalidParams = new();

                if (string.IsNullOrEmpty(_body.nom))
                    invalidParams.Add(new InvalidParam { name = "nom", reason = "Le nom est requis" });

                if (invalidParams.Count > 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Les données ne sont pas conformes", instance: HttpContext.Request.Path, invalidParams: invalidParams));

                if (_body.directionId.HasValue)
                {
                    var directionExist = await _dbContext.t_direction
                        .Where(d => d.r_id == _body.directionId && d.r_is_delete != true)
                        .FirstOrDefaultAsync();

                    if (directionExist == null)
                        return NotFound(GeneraleRetour.BuildNotFound(detail: "La direction spécifiée n'existe pas", instance: HttpContext.Request.Path));
                }

                t_employe entity = new t_employe
                {
                    r_nom = _body.nom,
                    r_prenom = _body.prenom,
                    r_adresse = _body.adresse,
                    r_sexe = _body.sexe,
                    nationalite = _body.nationalite,
                    r_telephone = _body.telephone,
                    r_date_naiss = _body.dateNaissance,
                    r_ville_naiss = _body.villeNaissance,
                    r_direction_FK = _body.directionId,
                };

                _dbContext.t_employe.Add(entity);
                await _dbContext.SaveChangesAsync();

                return Ok(new EmployeDto
                {
                    id = entity.r_id,
                    nom = entity.r_nom,
                    prenom = entity.r_prenom,
                    adresse = entity.r_adresse,
                    sexe = entity.r_sexe,
                    nationalite = entity.nationalite,
                    telephone = entity.r_telephone,
                    dateNaissance = entity.r_date_naiss,
                    villeNaissance = entity.r_ville_naiss,
                    directionId = entity.r_direction_FK,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpPut("employes/{id}")]
        public async Task<IActionResult> UpdateEmploye(int id, [FromBody] EmployeDto _body)
        {
            const string _desc_route = "Modifier un employé";

            try
            {
                if (id <= 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant de l'employé est manquant", instance: HttpContext.Request.Path));

                var resQuery = await _dbContext.t_employe
                    .Include(e => e.r_directionTab)
                    .Where(e => e.r_id == id && e.r_is_delete != true)
                    .FirstOrDefaultAsync();

                if (resQuery == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "L'employé n'existe pas", instance: HttpContext.Request.Path));

                if (_body.directionId.HasValue)
                {
                    var directionExist = await _dbContext.t_direction
                        .Where(d => d.r_id == _body.directionId && d.r_is_delete != true)
                        .FirstOrDefaultAsync();

                    if (directionExist == null)
                        return NotFound(GeneraleRetour.BuildNotFound(detail: "La direction spécifiée n'existe pas", instance: HttpContext.Request.Path));

                    resQuery.r_direction_FK = _body.directionId;
                }

                if (!string.IsNullOrEmpty(_body.nom)) resQuery.r_nom = _body.nom;
                if (!string.IsNullOrEmpty(_body.prenom)) resQuery.r_prenom = _body.prenom;
                if (!string.IsNullOrEmpty(_body.adresse)) resQuery.r_adresse = _body.adresse;
                if (!string.IsNullOrEmpty(_body.sexe)) resQuery.r_sexe = _body.sexe;
                if (!string.IsNullOrEmpty(_body.nationalite)) resQuery.nationalite = _body.nationalite;
                if (!string.IsNullOrEmpty(_body.telephone)) resQuery.r_telephone = _body.telephone;
                if (!string.IsNullOrEmpty(_body.dateNaissance)) resQuery.r_date_naiss = _body.dateNaissance;
                if (!string.IsNullOrEmpty(_body.villeNaissance)) resQuery.r_ville_naiss = _body.villeNaissance;
                if (_body.entiteId != null) resQuery.r_entite_id_fk = _body.entiteId;
                if (_body.directionId != null) resQuery.r_entite_id_fk = _body.directionId;
                if (_body.fonctionId != null) resQuery.r_fonction_id_fk = _body.fonctionId;

                _dbContext.t_employe.Update(resQuery);
                await _dbContext.SaveChangesAsync();

                return Ok(new EmployeDto
                {
                    id = resQuery.r_id,
                    nom = resQuery.r_nom,
                    prenom = resQuery.r_prenom,
                    adresse = resQuery.r_adresse,
                    sexe = resQuery.r_sexe,
                    nationalite = resQuery.nationalite,
                    telephone = resQuery.r_telephone,
                    dateNaissance = resQuery.r_date_naiss,
                    villeNaissance = resQuery.r_ville_naiss,
                    directionId = resQuery.r_direction_FK,
                    directionNom = resQuery.r_directionTab?.r_nom,
                    entiteId = resQuery.r_entite_id_fk,
                    entiteNom = resQuery.r_entiteTab.r_nom,
                    fonctionId = resQuery.r_fonction_id_fk,
                    fonctionNom = resQuery.r_fonctionTab.r_libelle,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpDelete("employes/{id}")]
        public async Task<IActionResult> DeleteEmploye(int id)
        {
            const string _desc_route = "Supprimer un employé";

            try
            {
                if (id <= 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant de l'employé est manquant", instance: HttpContext.Request.Path));

                var resQuery = await _dbContext.t_employe
                    .Where(e => e.r_id == id && e.r_is_delete != true)
                    .FirstOrDefaultAsync();

                if (resQuery == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "L'employé n'existe pas", instance: HttpContext.Request.Path));

                resQuery.r_is_delete = true;
                _dbContext.t_employe.Update(resQuery);
                await _dbContext.SaveChangesAsync();

                return StatusCode(204, "Employé supprimé avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        #endregion

        #region ========================= MODELES =========================

        [Authorize]
        [HttpGet("modeles")]
        public async Task<IActionResult> GetModeles()
        {
            const string _desc_route = "Liste des modèles";

            try
            {
                var respQuery = await _dbContext.t_modele
                    .Where(e => e.r_is_delete != true)
                    .OrderBy(e => e.r_description)
                    .ToListAsync();

                var modelesDto = respQuery.Select(m => new ModeleDto
                {
                    id = m.r_id,
                    description = m.r_description,
                    subject = m.r_subject,
                    body = m.r_body,
                    plateforme = m.r_plateforme,
                    type = m.r_type
                }).ToList();

                return Ok(new { data = modelesDto, meta = new { total = modelesDto.Count } });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpPost("modeles")]
        public async Task<IActionResult> CreateModele([FromBody] ModeleDto _body)
        {
            const string _desc_route = "Créer un modèle";

            try
            {
                List<InvalidParam> invalidParams = new();

                if (string.IsNullOrEmpty(_body.description))
                    invalidParams.Add(new InvalidParam { name = "description", reason = "La description est requise" });

                if (!_body.plateforme.HasValue)
                    invalidParams.Add(new InvalidParam { name = "plateforme", reason = "La plateforme est requise" });

                if (!_body.type.HasValue)
                    invalidParams.Add(new InvalidParam { name = "type", reason = "Le type est requis" });

                if (invalidParams.Count > 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Les données ne sont pas conformes", instance: HttpContext.Request.Path, invalidParams: invalidParams));

                t_modele entity = new t_modele
                {
                    r_description = _body.description,
                    r_subject = _body.subject,
                    r_body = _body.body,
                    r_plateforme = _body.plateforme,
                    r_type = _body.type,
                };

                _dbContext.t_modele.Add(entity);
                await _dbContext.SaveChangesAsync();

                return Ok(new ModeleDto
                {
                    id = entity.r_id,
                    description = entity.r_description,
                    subject = entity.r_subject,
                    body = entity.r_body,
                    plateforme = entity.r_plateforme,
                    type = entity.r_type,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpPut("modeles/{id}")]
        public async Task<IActionResult> UpdateModele(int id, [FromBody] ModeleDto _body)
        {
            const string _desc_route = "Modifier un modèle";

            try
            {
                if (id <= 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant du modèle est manquant", instance: HttpContext.Request.Path));

                var resQuery = await _dbContext.t_modele
                    .Where(e => e.r_id == id && e.r_is_delete != true)
                    .FirstOrDefaultAsync();

                if (resQuery == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Le modèle n'existe pas", instance: HttpContext.Request.Path));

                if (!string.IsNullOrEmpty(_body.description)) resQuery.r_description = _body.description;
                if (!string.IsNullOrEmpty(_body.subject)) resQuery.r_subject = _body.subject;
                if (!string.IsNullOrEmpty(_body.body)) resQuery.r_body = _body.body;
                if (_body.plateforme.HasValue) resQuery.r_plateforme = _body.plateforme;
                if (_body.type.HasValue) resQuery.r_type = _body.type;

                _dbContext.t_modele.Update(resQuery);
                await _dbContext.SaveChangesAsync();

                return Ok(new ModeleDto
                {
                    id = resQuery.r_id,
                    description = resQuery.r_description,
                    subject = resQuery.r_subject,
                    body = resQuery.r_body,
                    plateforme = resQuery.r_plateforme,
                    type = resQuery.r_type,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpDelete("modeles/{id}")]
        public async Task<IActionResult> DeleteModele(int id)
        {
            const string _desc_route = "Supprimer un modèle";

            try
            {
                if (id <= 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant du modèle est manquant", instance: HttpContext.Request.Path));

                var resQuery = await _dbContext.t_modele
                    .Where(e => e.r_id == id && e.r_is_delete != true)
                    .FirstOrDefaultAsync();

                if (resQuery == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Le modèle n'existe pas", instance: HttpContext.Request.Path));

                resQuery.r_is_delete = true;
                _dbContext.t_modele.Update(resQuery);
                await _dbContext.SaveChangesAsync();

                return StatusCode(204, "Modèle supprimé avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        #endregion

        #region ========================= PARAMETRES SYSTEME =========================

        [Authorize]
        [HttpGet("parametres")]
        public async Task<IActionResult> GetParametresSysteme()
        {
            const string _desc_route = "Liste des paramètres système";

            try
            {
                var respQuery = await _dbContext.t_parametre_systeme
                    .Where(e => e.r_is_delete != true)
                    .OrderBy(e => e.cle)
                    .ToListAsync();

                var parametresDto = respQuery.Select(p => new ParametreSystemeDto
                {
                    id = p.r_id,
                    cle = p.cle,
                    valeur = p.valeur,
                    tag = p.tag
                }).ToList();

                return Ok(new { data = parametresDto, meta = new { total = parametresDto.Count } });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpPost("parametres")]
        public async Task<IActionResult> CreateParametreSysteme([FromBody] ParametreSystemeDto _body)
        {
            const string _desc_route = "Créer un paramètre système";

            try
            {
                List<InvalidParam> invalidParams = new();

                if (string.IsNullOrEmpty(_body.cle))
                    invalidParams.Add(new InvalidParam { name = "cle", reason = "La clé est requise" });

                if (string.IsNullOrEmpty(_body.valeur))
                    invalidParams.Add(new InvalidParam { name = "valeur", reason = "La valeur est requise" });

                if (invalidParams.Count > 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Les données ne sont pas conformes", instance: HttpContext.Request.Path, invalidParams: invalidParams));

                var exist = await _dbContext.t_parametre_systeme
                    .Where(e => e.cle == _body.cle && e.tag == _body.tag && e.r_is_delete != true)
                    .FirstOrDefaultAsync();

                if (exist != null)
                    return StatusCode(403, GeneraleRetour.BuildForbid(detail: "Un paramètre avec cette clé et ce tag existe déjà", instance: HttpContext.Request.Path));

                t_parametre_systeme entity = new t_parametre_systeme
                {
                    cle = _body.cle,
                    valeur = _body.valeur,
                    tag = _body.tag,
                };

                _dbContext.t_parametre_systeme.Add(entity);
                await _dbContext.SaveChangesAsync();

                return Ok(new ParametreSystemeDto
                {
                    id = entity.r_id,
                    cle = entity.cle,
                    valeur = entity.valeur,
                    tag = entity.tag,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpPut("parametres/{id}")]
        public async Task<IActionResult> UpdateParametreSysteme(int id, [FromBody] ParametreSystemeDto _body)
        {
            const string _desc_route = "Modifier un paramètre système";

            try
            {
                if (id <= 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant du paramètre est manquant", instance: HttpContext.Request.Path));

                var resQuery = await _dbContext.t_parametre_systeme
                    .Where(e => e.r_id == id && e.r_is_delete != true)
                    .FirstOrDefaultAsync();

                if (resQuery == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Le paramètre n'existe pas", instance: HttpContext.Request.Path));

                var resQueryExist = await _dbContext.t_parametre_systeme
                    .Where(e => e.cle == _body.cle && e.tag == _body.tag && e.r_is_delete != true && e.r_id != id)
                    .FirstOrDefaultAsync();

                if (resQueryExist != null)
                    return StatusCode(403, GeneraleRetour.BuildForbid(detail: "Un paramètre avec cette clé et ce tag existe déjà", instance: HttpContext.Request.Path));

                if (!string.IsNullOrEmpty(_body.cle)) resQuery.cle = _body.cle;
                if (!string.IsNullOrEmpty(_body.valeur)) resQuery.valeur = _body.valeur;
                if (!string.IsNullOrEmpty(_body.tag)) resQuery.tag = _body.tag;

                _dbContext.t_parametre_systeme.Update(resQuery);
                await _dbContext.SaveChangesAsync();

                return Ok(new ParametreSystemeDto
                {
                    id = resQuery.r_id,
                    cle = resQuery.cle,
                    valeur = resQuery.valeur,
                    tag = resQuery.tag,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpDelete("parametres/{id}")]
        public async Task<IActionResult> DeleteParametreSysteme(int id)
        {
            const string _desc_route = "Supprimer un paramètre système";

            try
            {
                if (id <= 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant du paramètre est manquant", instance: HttpContext.Request.Path));

                var resQuery = await _dbContext.t_parametre_systeme
                    .Where(e => e.r_id == id && e.r_is_delete != true)
                    .FirstOrDefaultAsync();

                if (resQuery == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Le paramètre n'existe pas", instance: HttpContext.Request.Path));

                resQuery.r_is_delete = true;
                _dbContext.t_parametre_systeme.Update(resQuery);
                await _dbContext.SaveChangesAsync();

                return StatusCode(204, "Paramètre supprimé avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        #endregion

        [Authorize]
        [HttpPost("fonctions")]
        public async Task<IActionResult> CreateFunction([FromBody] FonctionDto _body)
        {

            string _desc_route = "Créer une fonction";

            try
            {


                List<InvalidParam> invalidParams = new List<InvalidParam>();

                if (string.IsNullOrEmpty(_body.libelle))
                    invalidParams.Add(new InvalidParam { name = "libelle", reason = "Le nom est requis" });

                if (string.IsNullOrEmpty(_body.code))
                    invalidParams.Add(new InvalidParam { name = "code", reason = "Le code est requis" });


                if (invalidParams.Count > 0)
                {
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Les données ne sont pas conformes", instance: HttpContext.Request.Path, invalidParams: invalidParams));
                }

                var resp_query = await _dbContext.t_fonction
                    .Where(e => e.r_code == _body.code && e.r_is_delete != true)
                    .FirstOrDefaultAsync();

                /// Recherche de l'alias dans la base
                if (resp_query != null)
                    return StatusCode(403, GeneraleRetour.BuildForbid(detail: "Le code de la fonction existe déjà dans la liste des fonctions", instance: HttpContext.Request.Path));


                t_fonction f = new t_fonction
                {
                    r_libelle = _body.libelle,
                    r_code = _body.code,
                };


                _dbContext.t_fonction.Add(f);
                _dbContext.SaveChanges();

                FonctionDto fDto = new FonctionDto

                {
                    id = f.r_id,
                    libelle = f.r_libelle,
                    code = f.r_code,
                };

                return Ok(fDto);

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}]  ===============================>{ex.Message}");
                var problem = GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path);
                return StatusCode(500, problem);
            }
        }

        [Authorize]
        [HttpDelete("fonctions/{id}")]
        public async Task<IActionResult> SupprimerUneFonction(int id)
        {

            string _desc_route = "Supprimer une fonction";

            try
            {

                if (id <= 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant de la fonction à supprimer est manquant", instance: HttpContext.Request.Path));

                var resQuery = await _dbContext.t_fonction
                    .Where(e => e.r_id == id && e.r_is_delete != true)
                    .FirstOrDefaultAsync();

                if (resQuery == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "La fonction n'existe pas dans la liste des fonctions", instance: HttpContext.Request.Path));


                resQuery.r_is_delete = true;

                _dbContext.t_fonction.Update(resQuery);
                _dbContext.SaveChanges();

                return StatusCode(204, "Fonction supprimée avec succès");

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                var problem = GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path);
                return StatusCode(500, problem);
            }
        }

        [Authorize]
        [HttpPut("fonctions/{id}")]
        public async Task<IActionResult> UpdateFonction(int id, [FromBody] FonctionDto _body)
        {

            string _desc_route = "Modifier une fonction";

            try
            {

                if (id <= 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant de la fonction à supprimer est manquant", instance: HttpContext.Request.Path));

                var resQuery = await _dbContext.t_fonction
                    .Where(e => e.r_id == id && e.r_is_delete != true)
                    .FirstOrDefaultAsync();

                if (resQuery == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "La fonction n'existe pas dans la liste des fonctions", instance: HttpContext.Request.Path));

                var resQueryExist = await _dbContext.t_fonction
                  .Where(e => e.r_code == _body.code && e.r_is_delete != true && e.r_id != id)
                  .FirstOrDefaultAsync();

                /// Recherche de l'alias sur un autre contact dans la base
                if (resQueryExist != null)
                    return StatusCode(403, GeneraleRetour.BuildForbid(detail: "L'alias existe déjà dans la liste des contacts", instance: HttpContext.Request.Path));


                if (!string.IsNullOrEmpty(_body.libelle)) resQuery.r_libelle = _body.libelle;
                if (!string.IsNullOrEmpty(_body.code)) resQuery.r_code = _body.code;

                _dbContext.t_fonction.Update(resQuery);
                _dbContext.SaveChanges();

                FonctionDto fDto = new FonctionDto
                {
                    id = resQuery.r_id,
                    libelle = resQuery.r_libelle,
                    code = resQuery.r_code,
                };

                return StatusCode(200, fDto);

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                var problem = GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path);
                return StatusCode(500, problem);
            }
        }

        [Authorize]
        [HttpGet("fonctions")]
        public async Task<IActionResult> GetFonctions()
        {

            string _desc_route = "Liste des fonctions";

            try
            {
                var respQuery = await _dbContext.t_fonction
                    .Where(e => e.r_is_delete != true)
                    .OrderBy(e => e.r_libelle)
                    .ToListAsync();


                var fonctionsDto = respQuery.Select(f => new FonctionDto
                {
                    id = f.r_id,
                    libelle = f.r_libelle,
                    code = f.r_code
                }).ToList();

                return Ok(new { data = fonctionsDto, meta = new MetaDto { total = fonctionsDto.Count() } });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");

                var problem = GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path);
                return StatusCode(500, problem);
            }
        }
    }
}
