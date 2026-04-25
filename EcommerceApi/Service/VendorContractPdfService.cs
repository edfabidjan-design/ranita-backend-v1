using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EcommerceApi.Service;

public class VendorContractPdfService
{
    public byte[] GenerateContractPdf(string vendorName, string email, string phone, decimal commission)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        var logoPath = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            "assets",
                            "img",
                            "logo-192.png"
                        );

                        if (File.Exists(logoPath))
                        {
                            row.ConstantItem(60).Image(logoPath);
                        }

                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("CONTRAT VENDEUR - RANITA")
                                .Bold()
                                .FontSize(16);

                            c.Item().Text($"Date : {DateTime.Now:dd/MM/yyyy}");
                        });
                    });

                    col.Item().PaddingTop(8).LineHorizontal(1);
                });

                page.Content().Column(col =>
                {
                    col.Spacing(5);

                    col.Item().Text("Informations du vendeur").Bold().FontSize(12);

                    col.Item().Text($"Nom de la boutique : {vendorName}");
                    col.Item().Text($"Email : {email}");
                    col.Item().Text($"Téléphone : {phone}");

                    col.Item().PaddingTop(5).Text("Article 1 - Objet").Bold();
                    col.Item().Text("Le présent contrat définit les conditions d'utilisation de la plateforme Ranita par le vendeur pour proposer ses produits à la vente.");

                    col.Item().PaddingTop(5).Text("Article 2 - Engagement du vendeur").Bold();
                    col.Item().Text("Le vendeur s'engage à fournir des produits conformes, maintenir un stock à jour, respecter les délais de livraison et garantir la qualité des produits.");

                    col.Item().PaddingTop(5).Text("Article 3 - Interdictions").Bold();
                    col.Item().Text("Il est interdit de vendre des produits frauduleux, contrefaits, interdits ou de publier des informations trompeuses.");

                    col.Item().PaddingTop(5).Text("Article 4 - Commission Ranita").Bold();
                    col.Item().Text($"En contrepartie de l’utilisation de la plateforme Ranita, une commission de {commission}% est prélevée sur chaque vente réalisée par le vendeur via la plateforme.");
                    col.Item().Text("Cette commission est déduite automatiquement avant tout reversement au vendeur.");
                    col.Item().Text("Ranita se réserve le droit de modifier cette commission, sous réserve d’en informer le vendeur au préalable.");

                    col.Item().PaddingTop(5).Text("Article 5 - Paiement vendeur").Bold();
                    col.Item().Text("Les paiements sont effectués selon les règles internes de la plateforme après validation des commandes.");

                    col.Item().PaddingTop(5).Text("Article 6 - Sanctions").Bold();
                    col.Item().Text("En cas de non-respect des présentes conditions, Ranita pourra suspendre, retirer ou supprimer la boutique vendeur.");

                    col.Item().PaddingTop(5).Text("Article 7 - Acceptation").Bold();
                    col.Item().Text("Le vendeur déclare avoir lu et accepté les conditions du présent contrat.");

                    col.Item().PaddingTop(8).Column(sig =>
                    {
                        sig.Spacing(3);
                        sig.Item().Text("Signature").Bold();
                        sig.Item().Text($"Nom : {vendorName}");
                        sig.Item().Text("Date : ________________________");
                        sig.Item().Text("Signature : ___________________");
                    });
                });

                page.Footer()
                    .AlignCenter()
                    .Text("Ranita Marketplace");
            });
        });

        return doc.GeneratePdf();
    }
}