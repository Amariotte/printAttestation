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

    }
}
