using System.IO.Compression;
using ask.ContextDb;
using ask.Model;
using ask.Services;
using ask.Tools;
using Microsoft.EntityFrameworkCore;
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


    public ZipAttestationService(
        IOracleService oracleService,
        IWebHostEnvironment env, ILogger<ZipAttestationService> logger, ServiceAsaci serviceAsaci, askContext dbContext, IHttpClientFactory httpClientFactory) 
    {
        _oracleService = oracleService;
        _env = env;
        _ServiceAsaci = serviceAsaci;
        _logger = logger;
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
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
        string sql = $@"SELECT  LIEN_PDF, LIEN_IMG,LIEN__QR, NUMATTDI
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
                // mettre à jour en base
                try
                {
                    var recStop = await _dbContext.t_job.FirstOrDefaultAsync(x => x.r_job_id == job.r_job_id);
                    if (recStop != null)
                    {
                        recStop.r_status = STATUT_JOB.CANCELLED;
                        recStop.r_completed_at = DateTime.UtcNow;
                        _dbContext.Update(recStop);
                        await _dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur maj base lors de l'annulation du job");
                }

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
                        path = Path.Combine(
                            _env.WebRootPath,
                            path.TrimStart('/')
                        );
                    }


                    bytes = await File.ReadAllBytesAsync(path, token);
                }



                successes.Add(
                    ($"Attestation_{num}.png", bytes)
                );



            
                // mettre à jour en base
                try
                {
                    var recCurrentOK = await _dbContext.t_job.FirstOrDefaultAsync(x => x.r_job_id == job.r_job_id);
                    if (recCurrentOK != null)
                    {
                      recCurrentOK.r_success += 1;
                      _dbContext.Update(recCurrentOK);
                      await _dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur maj base lors de l'annulation du job");
                }


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
                try
                {
                    var recErr = await _dbContext.t_job.FirstOrDefaultAsync(x => x.r_job_id == job.r_job_id);
                    if (recErr != null)
                    {
                        recErr.r_errors += 1;
                        _dbContext.Update(recErr);

                            await _dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex2)
                {
                    _logger.LogError(ex2, "Erreur lors de la mise à jour du compteur errors");
                }

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
            try
            {
                var recStop = await _dbContext.t_job.FirstOrDefaultAsync(x => x.r_job_id == job.r_job_id);
                if (recStop != null)
                {
                    recStop.r_status = STATUT_JOB.CANCELLED;
                    recStop.r_completed_at = DateTime.UtcNow;
                    _dbContext.Update(recStop);

                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur maj base lors de l'annulation du job");
            }

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


    public async Task GenerateZipCEDEAO(
       t_job job)
    {
        try
        {

        var successes = new List<(string FileName, byte[] Bytes)>();
        var errors = new List<string>();

        await Send(job, "start", new
        {
            total = job.r_total
        });


        var numList = job.r_attestations.ToList();


        int current = 0;
        var token = job.CancellationTokenSource?.Token ?? CancellationToken.None;



        foreach (var num in numList)
        {
            if (token.IsCancellationRequested)
            {

                    try
                    {
                        var recStop = await _dbContext.t_job.FirstOrDefaultAsync(x => x.r_job_id == job.r_job_id);
                        if (recStop != null)
                        {
                            recStop.r_status = STATUT_JOB.CANCELLED;
                            recStop.r_completed_at = DateTime.UtcNow;
                            _dbContext.Update(recStop);
                            await _dbContext.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur maj base lors de l'annulation du job");
                    }

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

                    await Send(job,"error", new { numero = num, message = "Numéro vide" });

                    // maj base errors
                    try
                    {
                        var recErr= await _dbContext.t_job.FirstOrDefaultAsync(x => x.r_job_id == job.r_job_id);
                        if (recErr != null)
                        {
                            recErr.r_errors = +1;
                            _dbContext.Update(recErr);
                            await _dbContext.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur maj base de la mise a jour");
                    }

                    continue;

                }

                try
                {
                    var result = await _ServiceAsaci.printCedeao(num);

                    if (result == null)
                    {
                        errors.Add($"{num}: erreur service Cedeao");
                        await Send(job,"error", new { numero = num, message = "Erreur service Cedeao" });
                        
                        // maj base errors
                        try
                        {
                            var recErr = await _dbContext.t_job.FirstOrDefaultAsync(x => x.r_job_id == job.r_job_id);
                            if (recErr != null)
                            {
                                recErr.r_errors = +1;
                                _dbContext.Update(recErr);
                                await _dbContext.SaveChangesAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erreur maj base de la mise a jour");
                        }

                        continue;
                    }
                {
                    errors.Add($"{num}: {result.detail}");

                    await Send(job,"error", new
                    {
                        numero = num,
                        message = result.detail
                    });

                    continue;
                }

                var res_data = JsonConvert.DeserializeObject<dynamic>(result.data);

                string base64Image = res_data.base64?.ToString();


                if (string.IsNullOrWhiteSpace(base64Image))
                {
                    errors.Add($"{num}: Base64 manquant");

                    await Send(job,"error", new
                    {
                        numero = num,
                        message = "Image Base64 manquante"
                    });

                    continue;
                }


                byte[] imageBytes = Tools.ConvertBase64ToImageBytes(base64Image);

                string fileNom = "CEDEAO_" + num + ".png";
                successes.Add((fileNom, imageBytes));


                    // maj base succes
                    try
                    {
                        var recErr = await _dbContext.t_job.FirstOrDefaultAsync(x => x.r_job_id == job.r_job_id);
                        if (recErr != null)
                        {
                            recErr.r_success = +1;
                            _dbContext.Update(recErr);
                            await _dbContext.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur maj base de la mise a jour");
                    }


                    await Send(job,"success", new
                {
                    numero = num,
                    message = "Attestation récupérée"
                });


            }
            catch (Exception ex)
            {
                errors.Add($"{num}: {ex.Message}");




                await Send(job,"error", new
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

            job.r_file_name = fileName;
            job.r_file_path = filePath;
            job.r_completed_at = DateTime.UtcNow;
            job.r_status = STATUT_JOB.COMPLETED;
            await _dbContext.SaveChangesAsync();

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
            try
            {
                job.r_status = STATUT_JOB.CANCELLED;
                job.r_completed_at = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur maj base lors de l'annulation du job CEDEAO");
            }

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


    private async Task Send(t_job job,string type,object data)
    {
        await job.Events.Writer.WriteAsync(new{type,data});
    }

}