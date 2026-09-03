using System.IO.Compression;
using print_attestation.ContextDb;
using print_attestation.Dtos.General;
using print_attestation.Model;
using print_attestation.Services;
using print_attestation.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OracleApi.Services;
using Org.BouncyCastle.Ocsp;

public class ZipAttestationService
{

    private readonly IOracleService _oracleService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ZipAttestationService> _logger;
    private readonly ServiceAsaci _ServiceAsaci;
    private readonly askContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ParamAppSettings _param_app_settings;


    public ZipAttestationService(
        IOracleService oracleService,
        IWebHostEnvironment env, ILogger<ZipAttestationService> logger, ServiceAsaci serviceAsaci, IOptions<ParamAppSettings> param_app_settings, askContext dbContext, IHttpClientFactory httpClientFactory)
    {
        _oracleService = oracleService;
        _env = env;
        _ServiceAsaci = serviceAsaci;
        _logger = logger;
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _param_app_settings = param_app_settings.Value;
    }
    public async Task GenerateZipATD(t_job job)
    {
        try
        {

            var successes = new List<(string FileName, byte[] Bytes)>();
            var errors = new List<string>();

            await Send(job, "start", new
            {
                total = job.r_total
            });

            // Mettre à jour en base : démarrage
            try
            {
                var rec = await _dbContext.t_job.FirstOrDefaultAsync(x => x.r_job_id == job.r_job_id);
                if (rec != null)
                {
                    rec.r_status = STATUT_JOB.RUNNING;
                    rec.r_total = job.r_total;
                    _dbContext.Update(rec);
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Impossible de mettre à jour le job en base (start)");
            }


            var numList = job.r_attestations.ToList();

            // Génération des paramètres Oracle
            var parameters = new Dictionary<string, object>();
            var placeholders = new List<string>();
            for (int i = 0; i < numList.Count; i++)
            {
                string paramName = $":num{i}";

                placeholders.Add(paramName);

                parameters.Add(paramName, numList[i]);
            }


            // Une seule requête Oracle
            string sql = $@"SELECT  LIEN_PDF, LIEN_IMG,LIEN__QR, NUMATTDI,NUMEIMMA
                                    FROM attestation_risque
                                         WHERE NUMATTDI IN ({string.Join(",", placeholders)})";


            var rows = await _oracleService.ExecuteQueryAsync(sql, parameters);


            // Indexation par numéro pour accès rapide
            var attestations = rows.ToDictionary(
                x => x["NUMATTDI"]?.ToString(),
                x => x
            );

            int current = 0;

            var token = job.CancellationTokenSource?.Token ?? CancellationToken.None;

            foreach (var num in numList)
            {
                if (token.IsCancellationRequested)
                {

                    await updateCompleted(job.r_job_id, STATUT_JOB.CANCELLED);
                    await Send(job, "stopped", new { message = "Job arrêté" });
                    try { job.Events.Writer.Complete(); } catch { }
                    return;
                }
                current++;

                _logger.LogError(current + "===========>" + num);

                await Send(job, "progress", new
                {
                    current,
                    total = job.r_total,
                    numero = num
                });


                try
                {

                    if (!attestations.TryGetValue(num, out var row))
                    {
                        errors.Add($"{num}: introuvable");

                        await Send(job, "error",
                        new
                        {
                            numero = num,
                            message = "Introuvable"
                        });

                        continue;
                    }

                    string? path = row["LIEN_PDF"]?.ToString() ?? row["LIEN_IMG"]?.ToString() ?? row["LIEN__QR"]?.ToString();


                    if (string.IsNullOrEmpty(path))
                    {
                        errors.Add($"{num}: fichier absent");

                        await Send(job, "error",
                        new
                        {
                            numero = num,
                            message = "Fichier absent"
                        });

                        continue;
                    }



                    byte[] bytes;


                    if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        var client = _httpClientFactory.CreateClient();
                        var resp = await client.GetAsync(path, HttpCompletionOption.ResponseContentRead, token);
                        resp.EnsureSuccessStatusCode();
                        bytes = await resp.Content.ReadAsByteArrayAsync(token);
                    }
                    else
                    {

                        if (!Path.IsPathRooted(path))
                        {
                            path = Path.Combine( _env.WebRootPath,path.TrimStart('/'));
                        }

                        bytes = await File.ReadAllBytesAsync(path, token);
                    }

                    string[] _caracteres = _param_app_settings.immatriculation.charactersToReplace ?? [];
                    string _nameFile = num + "_" + Tools.ReplaceCaracteres(row["NUMEIMMA"]?.ToString(), _caracteres) + ".png";

                    successes.Add((_nameFile, bytes));

                    // mettre à jour en base
                    await updateLigneDetailsJob(job.r_job_id, num, TYPE_JOB.atd, true, null);
                    await Send(job, "success",
                new
                {
                    numero = num
                });

                }
                catch (Exception ex)
                {
                    errors.Add($"{num}:{ex.Message}");
                    // mise à jour en base : errors++
                    await updateLigneDetailsJob(job.r_job_id, num, TYPE_JOB.atd, false, ex.Message);
                    await Send(job, "error", new { numero = num, message = ex.Message });
                }
            }



            // ======================
            // CREATION ZIP (écriture directe disque)
            // ======================

            var fileName = $"Attestations_{DateTime.UtcNow:yyyyMMddHHmmss}.zip";
            var filePath = Path.Combine(Path.GetTempPath(), fileName);

            try
            {
                await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
                using (var archive = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: false))
                {
                    foreach (var item in successes)
                    {
                        token.ThrowIfCancellationRequested();
                        var entry = archive.CreateEntry(item.FileName, CompressionLevel.Optimal);
                        await using var stream = entry.Open();
                        await stream.WriteAsync(item.Bytes, 0, item.Bytes.Length, token);
                    }

                    if (errors.Any())
                    {
                        var entry = archive.CreateEntry("errors.txt");
                        await using var writer = new StreamWriter(entry.Open());
                        foreach (var e in errors)
                        {
                            await writer.WriteLineAsync(e);
                        }
                    }
                }


                try
                {
                    var recOK = await _dbContext.t_job.FirstOrDefaultAsync(x => x.r_job_id == job.r_job_id);
                    if (recOK != null)
                    {
                        recOK.r_file_name = fileName;
                        recOK.r_file_path = filePath;
                        recOK.r_completed_at = DateTime.UtcNow;
                        recOK.r_status = STATUT_JOB.COMPLETED;
                        _dbContext.Update(recOK);

                        await _dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur maj base lors de la mise à jour du job");
                }


                await Send(job, "complete",
                new
                {
                    success = true,
                    file = fileName,
                    total = successes.Count,
                    errors = errors.Count
                });
            }
            catch (OperationCanceledException)
            {
                await updateCompleted(job.r_job_id, STATUT_JOB.CANCELLED);
                await Send(job, "stopped", new { message = "Job arrêté" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du ZIP");
                await Send(job, "error", new { message = "Erreur lors de la création du ZIP" });
            }
        }
        finally
        {
            try { job.Events.Writer.Complete(); } catch { }
        }

    }


    public async Task GenerateZipCEDEAO(t_job job)
    {
        try
        {
            var successes = new List<(string FileName, byte[] Bytes)>();
            var errors = new List<string>();

            await Send(job, "start", new { total = job.r_total });
            var numList = job.r_attestations.ToList();

            int current = 0;
            var token = job.CancellationTokenSource?.Token ?? CancellationToken.None;



            foreach (var num in numList)
            {
                if (token.IsCancellationRequested)
                {
                    await updateCompleted(job.r_job_id, STATUT_JOB.CANCELLED);
                    await Send(job, "stopped", new { message = "Job arrêté" });
                    return;
                }

                current++;
                _logger.LogError(current + "===========>" + num);


                await Send(job, "progress", new
                {
                    current,
                    total = job.r_total,
                    numero = num
                });


                if (string.IsNullOrWhiteSpace(num))
                {
                    errors.Add($"{num}: numéro vide");

                    await Send(job, "error", new { numero = num, message = "Numéro vide" });
                    await updateLigneDetailsJob(job.r_job_id, num, TYPE_JOB.cedeao, false, "Numéro vide");
                    continue;
                }

                try
                {
                    var result = await _ServiceAsaci.printCedeao(num);

                    if (result == null)
                    {
                        errors.Add($"{num}: erreur service Cedeao");

                        await Send(job, "error", new { numero = num, message = "Erreur service Cedeao" });
                        await updateLigneDetailsJob(job.r_job_id, num, TYPE_JOB.cedeao, false, "Erreur service Cedeao");

                        continue;
                    }

                    if (!Tools.RetourIsSucces(result.status))
                    {
                        errors.Add($"{num}: {result.detail}");
                        await Send(job, "error", new{numero = num,message = result.detail});
                        await updateLigneDetailsJob(job.r_job_id, num, TYPE_JOB.cedeao, false, result.detail);
                        continue;
                    }

                    var res_data = JsonConvert.DeserializeObject<dynamic>(result.data);

                    string base64Image = res_data.base64?.ToString();


                    if (string.IsNullOrWhiteSpace(base64Image))
                    {
                        errors.Add($"{num}: Base64 manquant");
                        await updateLigneDetailsJob(job.r_job_id, num, TYPE_JOB.cedeao, false, "Image Base64 manquante");

                        await Send(job, "error", new
                        {
                            numero = num,
                            message = "Image Base64 manquante"
                        });

                        continue;
                    }


                    byte[] imageBytes = Tools.ConvertBase64ToImageBytes(base64Image);

                    string fileNom = "CEDEAO_" + num + ".png";
                    successes.Add((fileNom, imageBytes));

                    await updateLigneDetailsJob(job.r_job_id, num, TYPE_JOB.cedeao, true, null);
                    await Send(job, "success", new
                    {
                        numero = num,
                        message = "Attestation récupérée"
                    });


                }
                catch (Exception ex)
                {
                    errors.Add($"{num}: {ex.Message}");

                    await updateLigneDetailsJob(job.r_job_id, num, TYPE_JOB.cedeao, false, ex.Message);

                    await Send(job, "error", new
                    {
                        numero = num,
                        message = ex.Message
                    });

                }
            }


            // ======================
            // CREATION ZIP (écriture directe disque)
            // ======================

            var fileName = $"Attestations_CEDEAO_{DateTime.UtcNow:yyyyMMddHHmmss}.zip";
            var filePath = Path.Combine(Path.GetTempPath(), fileName);

            try
            {
                await using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                using (var archive = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: false))
                {
                    foreach (var item in successes)
                    {
                        token.ThrowIfCancellationRequested();
                        var entry = archive.CreateEntry(item.FileName, CompressionLevel.Optimal);
                        await using var stream = entry.Open();
                        await stream.WriteAsync(item.Bytes, 0, item.Bytes.Length, token);
                    }

                    if (errors.Any())
                    {
                        var entry = archive.CreateEntry("errors.txt");
                        await using var writer = new StreamWriter(entry.Open());
                        foreach (var e in errors)
                        {
                            await writer.WriteLineAsync(e);
                        }
                    }
                }

                var rec = await _dbContext.t_job.FirstOrDefaultAsync(x => x.r_job_id == job.r_job_id);
                if (rec != null)
                {
                    rec.r_file_name = fileName;
                    rec.r_file_path = filePath;
                    rec.r_completed_at = DateTime.UtcNow;
                    rec.r_status = STATUT_JOB.COMPLETED;
                    _dbContext.Update(rec);
                    await _dbContext.SaveChangesAsync();
                }

                await Send(job, "complete",
                new
                {
                    success = true,
                    file = fileName,
                    total = successes.Count,
                    errors = errors.Count
                });
            }
            catch (OperationCanceledException)
            {
                await updateCompleted(job.r_job_id, STATUT_JOB.CANCELLED);
                await Send(job, "stopped", new { message = "Job arrêté" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du ZIP CEDEAO");
                await Send(job, "error", new { message = "Erreur lors de la création du ZIP" });
            }



        }
        finally
        {
            try { job.Events.Writer.Complete(); } catch { }
        }

    }


    public async Task GenerateZipATDAndCEDEAO(t_job job)
    {
        try
        {
            var successes = new List<(string FileName, byte[] Bytes)>();
            var errors = new List<string>();

            string[] caracteres = _param_app_settings .immatriculation.charactersToReplace ?? [];

            await Send(job, "start", new
            {
                total = job.r_total
            });

            var numList = job.r_attestations.ToList();

            // ============================================================
            // 1. RÉCUPÉRATION DES ATTESTATIONS ATD DEPUIS ORACLE
            // ============================================================

            var parameters = new Dictionary<string, object>();
            var placeholders = new List<string>();

            for (int i = 0; i < numList.Count; i++)
            {
                string paramName = $":num{i}";

                placeholders.Add(paramName);
                parameters.Add(paramName, numList[i]);
            }

            string sql = $@"SELECT LIEN_PDF,LIEN_IMG,LIEN__QR,NUMATTDI,NUMEIMMA FROM attestation_risque WHERE NUMATTDI IN ({string.Join(",", placeholders)})";
            var rows = await _oracleService.ExecuteQueryAsync(sql, parameters);

            var attestations = rows.ToDictionary(
                x => x["NUMATTDI"]?.ToString(),
                x => x
            );

            int current = 0;

            var token = job.CancellationTokenSource?.Token
                        ?? CancellationToken.None;


            // ============================================================
            // 2. TRAITEMENT DE CHAQUE NUMÉRO
            // ============================================================

            foreach (var num in numList)
            {
                if (token.IsCancellationRequested)
                {
                    await updateCompleted(
                        job.r_job_id, STATUT_JOB.CANCELLED);

                    await Send(job, "stopped", new{ message = "Job arrêté"});
                    return;
                }

                current++;

                await Send(job, "progress", new{ current, total = job.r_total,numero = num});

                try
                {
                    // ====================================================
                    // ATTESTATION ATD
                    // ====================================================

                    if (!attestations.TryGetValue(num, out var row))
                    {
                        errors.Add($"{num}: attestation introuvable");
                        await Send(job, "error", new{numero = num, message = "Attestation introuvable"});
                        await updateLigneDetailsJob(job.r_job_id,num, TYPE_JOB.atd, false,"Attestation introuvable");
                        continue;
                    }


                    string? path = row["LIEN_PDF"]?.ToString() ?? row["LIEN_IMG"]?.ToString() ?? row["LIEN__QR"]?.ToString();


                    if (string.IsNullOrEmpty(path))
                    {
                        errors.Add($"{num}: fichier attestation absent");

                        await Send(job, "error", new{numero = num, message = "Fichier attestation absent"});
                        await updateLigneDetailsJob(job.r_job_id, num, TYPE_JOB.atd, false, "Fichier attestation absent");
                        continue;
                    }


                    byte[] attestationBytes;

                    if (path.StartsWith(
                        "http",StringComparison.OrdinalIgnoreCase))
                    {
                        var client = _httpClientFactory.CreateClient();
                        var response = await client.GetAsync(path,HttpCompletionOption.ResponseContentRead, token
                        );

                        response.EnsureSuccessStatusCode();

                        attestationBytes = await response.Content.ReadAsByteArrayAsync(token);
                    }
                    else
                    {
                        if (!Path.IsPathRooted(path))
                        {
                            path = Path.Combine( _env.WebRootPath,path.TrimStart('/') );
                        }

                        attestationBytes = await File.ReadAllBytesAsync(path, token);
                    }


                    // ====================================================
                    // NOM DE L'ATTESTATION
                    // ====================================================

                    string immatriculation = Tools.ReplaceCaracteres( row["NUMEIMMA"]?.ToString(),caracteres);

                    string attestationFileName = num + "_" + immatriculation + ".png";

                    successes.Add((attestationFileName, attestationBytes));

                    await updateLigneDetailsJob(
                       job.r_job_id,
                       num,
                       TYPE_JOB.atd,
                       true,
                       null
                   );



                    // ====================================================
                    // CEDEAO
                    // ====================================================

                    var result = await _ServiceAsaci.printCedeao(num);


                    if (result == null)
                    {
                        errors.Add($"{num}: erreur service CEDEAO" );
                        await Send(job, "error", new
                        { numero = num,message = "Erreur service CEDEAO"});
                        continue;
                    }


                    if (!Tools.RetourIsSucces(result.status))
                    {
                        errors.Add($"{num}: {result.detail}");
                        await Send(job, "error", new{numero = num, message = result.detail});
                        continue;
                    }

                    var resData = JsonConvert.DeserializeObject<dynamic>( result.data);

                    string base64Image = resData.base64?.ToString();


                    if (string.IsNullOrWhiteSpace(base64Image))
                    {
                        errors.Add( $"{num}: Base64 CEDEAO manquant");
                        await Send(job, "error", new{ numero = num,message = "Image CEDEAO manquante"});
                        continue;
                    }

                    byte[] cedeaoBytes =Tools.ConvertBase64ToImageBytes( base64Image );
                    string cedeaoFileName = "CEDEAO_" + num + ".png";


                    successes.Add((cedeaoFileName, cedeaoBytes) );

                    // ====================================================
                    // SUCCÈS
                    // ====================================================

                    await updateLigneDetailsJob(
                        job.r_job_id,
                        num,
                        TYPE_JOB.cedeao,
                        true,
                        null
                    );

                    await Send(job, "success", new
                    {
                        numero = num,
                        message = "Attestation et CEDEAO récupérées"
                    });
                }
                catch (Exception ex)
                {
                    errors.Add(
                        $"{num}: {ex.Message}"
                    );

                    await updateLigneDetailsJob(
                        job.r_job_id,
                        num,
                        TYPE_JOB.cedeao,
                        false,
                        ex.Message
                    );

                    await Send(job, "error", new
                    {
                        numero = num,
                        message = ex.Message
                    });
                }
            }


            // ============================================================
            // 3. CRÉATION DU ZIP
            // ============================================================

            var fileName =
                $"Attestations_ATD_CEDEAO_{DateTime.UtcNow:yyyyMMddHHmmss}.zip";

            var filePath = Path.Combine(Path.GetTempPath(),fileName);


            try
            {
                await using var fs =
                    new FileStream(filePath, FileMode.Create,FileAccess.Write, FileShare.None, 81920,true);

                using (var archive =
                    new ZipArchive(
                        fs,
                        ZipArchiveMode.Create,
                        leaveOpen: false
                    ))
                {
                    foreach (var item in successes)
                    {
                        token.ThrowIfCancellationRequested();

                        var entry = archive.CreateEntry(item.FileName,CompressionLevel.Optimal);

                        await using var stream = entry.Open();

                        await stream.WriteAsync(item.Bytes, 0, item.Bytes.Length,token);
                    }


                    // ====================================================
                    // FICHIER DES ERREURS
                    // ====================================================

                    if (errors.Any())
                    {
                        var entry =
                            archive.CreateEntry("errors.txt");

                        await using var writer =
                            new StreamWriter(entry.Open());

                        foreach (var error in errors)
                        {
                            await writer.WriteLineAsync(error);
                        }
                    }
                }


                // ========================================================
                // 4. MISE À JOUR DU JOB
                // ========================================================

                var rec =
                    await _dbContext.t_job
                        .FirstOrDefaultAsync(
                            x => x.r_job_id == job.r_job_id
                        );

                if (rec != null)
                {
                    rec.r_file_name = fileName;
                    rec.r_file_path = filePath;
                    rec.r_completed_at = DateTime.UtcNow;
                    rec.r_status = STATUT_JOB.COMPLETED;

                    _dbContext.Update(rec);

                    await _dbContext.SaveChangesAsync();
                }


                // ========================================================
                // 5. FIN
                // ========================================================

                await Send(job, "complete", new
                {
                    success = true,
                    file = fileName,
                    total = successes.Count,
                    errors = errors.Count
                });
            }
            catch (OperationCanceledException)
            {
                await updateCompleted(
                    job.r_job_id,
                    STATUT_JOB.CANCELLED
                );

                await Send(job, "stopped", new
                {
                    message = "Job arrêté"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erreur lors de la création du ZIP ATD + CEDEAO"
                );

                await Send(job, "error", new
                {
                    message = "Erreur lors de la création du ZIP"
                });
            }
        }
        finally
        {
            try
            {
                job.Events.Writer.Complete();
            }
            catch
            {
            }
        }
    }

    private async Task Send(t_job job, string type, object data)
    {
        await job.Events.Writer.WriteAsync(new { type, data });
    }


    private async Task updateLigneDetailsJob(string JobId, string num, TYPE_JOB type, bool bSuccess, string? descError)
    {

        try
        {

            var rec = await _dbContext.t_job.FirstOrDefaultAsync(x => x.r_job_id == JobId);
            if (rec != null)
            {
                var recDetails = new t_job_details
                {
                    r_attestation = num,
                    r_job_id_fk = rec.r_id,
                    r_type = type.ToString(), // Remplacez TYPE_JOB.ATD par la valeur appropriée
                    r_desc_error = descError,
                    r_success = bSuccess
                };

                _dbContext.Add(recDetails);


                if (bSuccess == true)
                    rec.r_success += 1;
                else
                    rec.r_errors += 1;

                _dbContext.Update(rec);
                await _dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur maj base de la mise a jour");
        }


    }

    private async Task updateCompleted(string JobId, STATUT_JOB statuts)
    {

        try
        {

            var rec = await _dbContext.t_job.FirstOrDefaultAsync(x => x.r_job_id == JobId);
            if (rec != null)
            {
                rec.r_status = statuts;
                rec.r_completed_at = DateTime.UtcNow;
                _dbContext.Update(rec);
                await _dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur maj base de la mise a jour");
        }


    }


}