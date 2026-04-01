using InteroperabiliteProject;
using InteroperabiliteProject.ContextDb;
using InteroperabiliteProject.Model;
using InteroperabiliteProject.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public static class MessageService
{
    public static async Task SaveMessageAsync(DbContext context, ILogger logger, typeMessage type, string body , bool bmsgIdEstmsgDemande = false)
    {
        try
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context), "[SaveMessageAsync] Contexte DB est null");

            if (string.IsNullOrWhiteSpace(body))
            {
                logger.LogWarning("[SaveMessageAsync] Body vide ou null, aucun message sauvegardé.");
                return;
            }

            string msgid = "";
            string endToEndId = "";
            string msgidDemande = "";
            string identifiantRevendication = "";
            string alias = "";
            string idcreationAlias = "";

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            msgidDemande = root.TryGetProperty("msgIdDemande", out var el1) ? el1.GetString() : null;
            msgid = root.TryGetProperty("msgId", out var el2) ? el2.GetString() : null;
            endToEndId = root.TryGetProperty("endToEndId", out var el3) ? el3.GetString() : null;
            identifiantRevendication = root.TryGetProperty("identifiantRevendication", out var el4) ? el4.GetString() : null;
            idcreationAlias = root.TryGetProperty("idCreationAlias", out var el5) ? el5.GetString() : null;
            alias = root.TryGetProperty("alias", out var el6) ? el6.GetString() : null;

            msgidDemande = Tools.FirstNotNullOrEmpty(
                msgidDemande,
                root.TryGetProperty("reference", out var referenceElement) ? referenceElement.GetString() : null
            );

            if (bmsgIdEstmsgDemande)
            {
                msgidDemande = msgid;
                msgid = null;
            }

            var msg = new t_message
            {
                idrevendication = identifiantRevendication,
                idcreationalias = idcreationAlias,
                msgid = msgid,
                msgiddemande = msgidDemande,
                alias = alias,
                endToEndId = endToEndId,
                sens = sensMessage.ENVOI_PI,
                body_message = body,
                type_message = type,
                date_message = DateTime.UtcNow
            };

            context.Set<t_message>().Add(msg);
            await context.SaveChangesAsync();

            logger.LogInformation($"[SaveMessageAsync] Message sauvegardé avec succès (msgId: {msgid}, type: {type})", msgid, type);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[SaveMessageAsync] Erreur lors de la sauvegarde du message");
            throw;
        }
    }


    public static async Task SaveMessageAsyncAvecDbFactory(IDbContextFactory<InteropContext> contextFactory, ILogger logger, typeMessage type, string body, bool bmsgIdEstmsgDemande = false)
    {
        try
        {
           

            string msgid = "";
            string endToEndId = "";
            string msgidDemande = "";
            string identifiantRevendication = "";
            string alias = "";
            string idcreationAlias = "";

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            msgidDemande = root.TryGetProperty("msgIdDemande", out var el1) ? el1.GetString() : null;
            msgid = root.TryGetProperty("msgId", out var el2) ? el2.GetString() : null;
            endToEndId = root.TryGetProperty("endToEndId", out var el3) ? el3.GetString() : null;
            identifiantRevendication = root.TryGetProperty("identifiantRevendication", out var el4) ? el4.GetString() : null;
            idcreationAlias = root.TryGetProperty("idCreationAlias", out var el5) ? el5.GetString() : null;
            alias = root.TryGetProperty("alias", out var el6) ? el6.GetString() : null;

            msgidDemande = Tools.FirstNotNullOrEmpty(
                msgidDemande,
                root.TryGetProperty("reference", out var referenceElement) ? referenceElement.GetString() : null
            );

            if (bmsgIdEstmsgDemande)
            {
                msgidDemande = msgid;
                msgid = null;
            }

            var msg = new t_message
            {
                idrevendication = identifiantRevendication,
                idcreationalias = idcreationAlias,
                msgid = msgid,
                msgiddemande = msgidDemande,
                alias = alias,
                endToEndId = endToEndId,
                sens = sensMessage.ENVOI_PI,
                body_message = body,
                type_message = type,
                date_message = DateTime.UtcNow
            };


            using var context = contextFactory.CreateDbContext() ;
            context.Set<t_message>().Add(msg);
            await context.SaveChangesAsync();

            logger.LogInformation($"[SaveMessageAsync] Message sauvegardé avec succès (msgId: {msgid}, type: {type})", msgid, type);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[SaveMessageAsync] Erreur lors de la sauvegarde du message");
            throw;
        }
    }


}
