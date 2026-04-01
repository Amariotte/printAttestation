using InteroperabiliteProject.Model;
using InteroperabiliteProject.Interface;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace InteroperabiliteProject.ServicceAIP
{


    public class ServiceEtat
    {

        private readonly IdatasRepo _datarepo;

        public ServiceEtat(IdatasRepo datarepo)
        {
            _datarepo = datarepo;
        }


        public async Task<string> GenererRecuPaiementPdf(t_transfert op, bool isClientPayeur, string folderPath, string fileName, string nomDuParticipant)
      
        {
           
        string chemin = Path.Combine(folderPath, fileName);

        PdfDocument document = new PdfDocument();
        document.Info.Title = $"Reçu de paiement {op.endToEndId}";

        PdfPage page = document.AddPage();
        XGraphics gfx = XGraphics.FromPdfPage(page);

        // Définition des polices
        XFont fontTitle = new XFont("Verdana", 16, XFontStyle.Bold);
        XFont fontText = new XFont("Verdana", 12, XFontStyle.Regular);
        XFont fontTextBold = new XFont("Verdana", 12, XFontStyle.Bold);

        // Dessiner le titre encadré
        XRect rectTitle = new XRect(40, 40, page.Width - 80, 30);
        gfx.DrawRectangle(XBrushes.Brown, rectTitle);
        gfx.DrawString("Reçu du paiement", fontTitle, XBrushes.White, rectTitle, XStringFormats.Center);

        int yPosition = 90;

        // Ajout des informations du paiement
        gfx.DrawString($"Identifiant", fontText, XBrushes.Black, new XRect(40, yPosition, page.Width, 20), XStringFormats.TopLeft);
        gfx.DrawString(op.endToEndId, fontTextBold, XBrushes.Black, new XRect(40, yPosition + 20, page.Width, 20), XStringFormats.TopLeft);
        yPosition += 80;

        gfx.DrawString($"Référence", fontText, XBrushes.Black, new XRect(40, yPosition, page.Width, 20), XStringFormats.TopLeft);
       
        if (!string.IsNullOrEmpty(op.identifiantTransaction))
               gfx.DrawString(op.identifiantTransaction, fontTextBold, XBrushes.Black, new XRect(40, yPosition + 20, page.Width, 20), XStringFormats.TopLeft);
          else
            gfx.DrawString("", fontTextBold, XBrushes.Black, new XRect(40, yPosition + 20, page.Width, 20), XStringFormats.TopLeft);
        
            yPosition += 80;

        gfx.DrawString($"Montant", fontText, XBrushes.Black, new XRect(40, yPosition, page.Width, 20), XStringFormats.TopLeft);
        gfx.DrawString($"{op.montant:N0} FCFA", fontTextBold, XBrushes.Black, new XRect(40, yPosition + 20, page.Width, 20), XStringFormats.TopLeft);
        yPosition += 80;

        gfx.DrawString($"Frais", fontText, XBrushes.Black, new XRect(40, yPosition, page.Width, 20), XStringFormats.TopLeft);
        gfx.DrawString("Gratuit", fontTextBold, XBrushes.Black, new XRect(40, yPosition + 20, page.Width, 20), XStringFormats.TopLeft);
        yPosition += 80;


 

        gfx.DrawString($"{(isClientPayeur ? "Envoyé à" : "Réçu de")}", fontText, XBrushes.Black, new XRect(40, yPosition, page.Width, 20), XStringFormats.TopLeft);
        gfx.DrawString( isClientPayeur ? op.nomClientPaye : op.nomClientPayeur, fontTextBold, XBrushes.Black, new XRect(40, yPosition + 20, page.Width, 20), XStringFormats.TopLeft);
        yPosition += 80;
        
        if (isClientPayeur)
         {
                  if (!string.IsNullOrEmpty(op.aliasClientPaye))
                    {
                        gfx.DrawString($"Alias", fontTextBold, XBrushes.Black, new XRect(40, yPosition, page.Width, 20), XStringFormats.TopLeft);
                        gfx.DrawString(op.aliasClientPaye, fontText, XBrushes.Black, new XRect(40, yPosition + 20, page.Width - 80, 20), XStringFormats.TopLeft);
                        yPosition += 80;
                    }
                    else
                    {
                   
                        gfx.DrawString($"Numéro de compte", fontTextBold, XBrushes.Black, new XRect(40, yPosition, page.Width, 20), XStringFormats.TopLeft);
                        gfx.DrawString(op.compteClientPayeur, fontText, XBrushes.Black, new XRect(40, yPosition + 20, page.Width - 80, 20), XStringFormats.TopLeft);
                        yPosition += 80;

                        gfx.DrawString($"Institution financière", fontTextBold, XBrushes.Black, new XRect(40, yPosition, page.Width, 20), XStringFormats.TopLeft);
                        gfx.DrawString(nomDuParticipant, fontText, XBrushes.Black, new XRect(40, yPosition + 20, page.Width - 80, 20), XStringFormats.TopLeft);
                        yPosition += 80;
                    }

                }
        else
              {
                 if(!string.IsNullOrEmpty(op.aliasClientPayeur))
                {
                    gfx.DrawString($"Alias", fontTextBold, XBrushes.Black, new XRect(40, yPosition, page.Width, 20), XStringFormats.TopLeft);
                    gfx.DrawString(op.aliasClientPayeur, fontText, XBrushes.Black, new XRect(40, yPosition + 20, page.Width - 80, 20), XStringFormats.TopLeft);
                    yPosition += 80;
                }
                 else
                {
                    if (!string.IsNullOrEmpty(op.compteClientPayeur))
                    {
                        gfx.DrawString($"Compte", fontTextBold, XBrushes.Black, new XRect(40, yPosition, page.Width, 20), XStringFormats.TopLeft);
                    gfx.DrawString(op.compteClientPayeur, fontText, XBrushes.Black, new XRect(40, yPosition + 20, page.Width - 80, 20), XStringFormats.TopLeft);
                    yPosition += 80;
                }
                }
                   
                }
       


        gfx.DrawString($"Date réception", fontText, XBrushes.Black, new XRect(40, yPosition, page.Width, 20), XStringFormats.TopLeft);
        gfx.DrawString((op.dateHeureIrrevocabilite ?? op.dateHeureAcceptation ?? op.r_createdon)?.ToString("dd/MM/yyyy à HH:mm"), fontTextBold, XBrushes.Black, new XRect(40, yPosition + 20, page.Width, 20), XStringFormats.TopLeft);

        // Enregistrer le document
        document.Save(chemin);

        return chemin;
    }
  


}
}
