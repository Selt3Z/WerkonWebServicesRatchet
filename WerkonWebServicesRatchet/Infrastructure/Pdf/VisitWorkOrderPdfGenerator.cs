using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace WerkonWebServicesRatchet.Infrastructure.Pdf;

public sealed class VisitWorkOrderPdfGenerator
{
    private static readonly Color BrandBlue = Color.FromHex("#2765A8");
    private static readonly Color SoftGray = Color.FromHex("#F3F5F8");
    private static readonly Color BorderGray = Color.FromHex("#C9D0D8");
    private static readonly Color LabelGray = Color.FromHex("#5B6673");

    public byte[] Generate(VisitWorkOrderData data)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var documentNumber = data.VisitNumber.ToString();

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(style => style.FontSize(8.5f).FontFamily("Arial").FontColor(Colors.Black));

                page.Header().Element(header => ComposeHeader(header, data, documentNumber));
                page.Content().Element(content => ComposeContent(content, data));
                page.Footer().Element(footer => ComposeFooter(footer, data));
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, VisitWorkOrderData data, string documentNumber)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    if (!string.IsNullOrWhiteSpace(data.Organization.OrganizationName))
                    {
                        left.Item().Text(data.Organization.OrganizationName).Bold().FontSize(11).FontColor(BrandBlue);
                    }

                    if (!string.IsNullOrWhiteSpace(data.Organization.Address))
                    {
                        left.Item().PaddingTop(2).Text(data.Organization.Address).FontSize(7.5f).FontColor(LabelGray);
                    }

                    var contacts = BuildContactLine(data.Organization);
                    if (!string.IsNullOrWhiteSpace(contacts))
                    {
                        left.Item().Text(contacts).FontSize(7.5f).FontColor(LabelGray);
                    }

                    var taxLine = BuildTaxLine(data.Organization);
                    if (!string.IsNullOrWhiteSpace(taxLine))
                    {
                        left.Item().PaddingTop(2).Text(taxLine).FontSize(7).FontColor(LabelGray);
                    }
                });

                if (data.Organization.LogoBytes is { Length: > 0 })
                {
                    row.ConstantItem(96).AlignRight().AlignMiddle().Height(46).Image(data.Organization.LogoBytes).FitArea();
                }
            });

            column.Item().PaddingTop(8).LineHorizontal(1.2f).LineColor(BrandBlue);

            column.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem().AlignMiddle().Text("ЗАКАЗ-НАРЯД").Bold().FontSize(13).FontColor(BrandBlue);
                row.ConstantItem(180).AlignRight().Column(meta =>
                {
                    meta.Item().Text($"№ {documentNumber}").Bold().FontSize(9);
                    meta.Item().Text($"Дата: {data.VisitedAtLocal:dd.MM.yyyy HH:mm}").FontSize(8);
                });
            });
        });
    }

    private static void ComposeContent(IContainer container, VisitWorkOrderData data)
    {
        container.PaddingTop(10).Column(column =>
        {
            column.Spacing(8);

            column.Item().Row(row =>
            {
                row.RelativeItem().Element(c => ComposeInfoCard(c, "Заказчик",
                [
                    ("ФИО / организация", data.ClientFullName),
                    ("Телефон", data.ClientPhoneNumber)
                ]));

                row.ConstantItem(10);

                row.RelativeItem().Element(c => ComposeInfoCard(c, "Автомобиль",
                [
                    ("Марка / модель", $"{data.VehicleBrand} {data.VehicleModel}".Trim()),
                    ("Госномер", data.LicensePlate),
                    ("VIN", data.Vin),
                    ("Пробег", data.MileageAtVisit?.ToString())
                ]));
            });

            column.Item().Element(c => ComposeInfoCard(c, "Обращение",
            [
                ("Жалоба / причина", data.CustomerComplaint),
                ("Ответственный мастер", data.AssignedMechanicDisplayName),
                ("Комментарий мастера", data.MechanicComment)
            ]));

            column.Item().Text("Выполненные работы").Bold().FontSize(9.5f).FontColor(BrandBlue);

            column.Item().Element(c => ComposeWorksTable(c, data));

            column.Item().AlignRight().Background(SoftGray).Border(1).BorderColor(BorderGray).Padding(6).Width(200)
                .Row(row =>
                {
                    row.RelativeItem().Text("ИТОГО").Bold().FontSize(9);
                    row.ConstantItem(80).AlignRight().Text($"{data.TotalAmount:0.00}").Bold().FontSize(10).FontColor(BrandBlue);
                });

            if (HasBankDetails(data.Organization))
            {
                column.Item().Element(c => ComposeInfoCard(c, "Платёжные реквизиты",
                [
                    ("Расчётный счёт", data.Organization.BankAccount),
                    ("БИК", data.Organization.Bik)
                ]));
            }

            column.Item().PaddingTop(14).Row(row =>
            {
                row.RelativeItem().Element(c => ComposeSignatureBlock(c, "Исполнитель"));
                row.ConstantItem(20);
                row.RelativeItem().Element(c => ComposeSignatureBlock(c, "Заказчик"));
            });
        });
    }

    private static void ComposeWorksTable(IContainer container, VisitWorkOrderData data)
    {
        container.DefaultTextStyle(style => style.FontSize(8)).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(24);
                columns.RelativeColumn(4.2f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(1.4f);
                columns.RelativeColumn(1.4f);
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).AlignCenter().Text("№").Bold().FontSize(7.5f);
                header.Cell().Element(HeaderCell).Text("Наименование").Bold().FontSize(7.5f);
                header.Cell().Element(HeaderCell).AlignCenter().Text("Кол-во").Bold().FontSize(7.5f);
                header.Cell().Element(HeaderCell).AlignRight().Text("Цена").Bold().FontSize(7.5f);
                header.Cell().Element(HeaderCell).AlignRight().Text("Сумма").Bold().FontSize(7.5f);
            });

            if (data.Items.Count == 0)
            {
                table.Cell().ColumnSpan(5).Element(BodyCell).Padding(6).Text("Работы не указаны.").FontSize(8).FontColor(LabelGray);
                return;
            }

            var index = 1;
            foreach (var item in data.Items)
            {
                table.Cell().Element(BodyCell).AlignCenter().Text(index.ToString()).FontSize(8);
                table.Cell().Element(BodyCell).Column(name =>
                {
                    name.Item().Text(item.Name).FontSize(8);
                    if (!string.IsNullOrWhiteSpace(item.Comment))
                    {
                        name.Item().Text(item.Comment).FontSize(7).FontColor(LabelGray);
                    }
                });
                table.Cell().Element(BodyCell).AlignCenter().Text(item.Quantity.ToString("0.##")).FontSize(8);
                table.Cell().Element(BodyCell).AlignRight().Text(item.UnitPrice.ToString("0.00")).FontSize(8);
                table.Cell().Element(BodyCell).AlignRight().Text(item.TotalPrice.ToString("0.00")).FontSize(8);
                index++;
            }
        });
    }

    private static void ComposeInfoCard(IContainer container, string title, (string Label, string? Value)[] fields)
    {
        var visible = fields
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .ToArray();

        if (visible.Length == 0)
        {
            container.Border(1).BorderColor(BorderGray).Background(Colors.White).Padding(7).Column(column =>
            {
                column.Item().Text(title).Bold().FontSize(8.5f).FontColor(BrandBlue);
                column.Item().PaddingTop(3).Text("—").FontSize(8).FontColor(LabelGray);
            });
            return;
        }

        container.Border(1).BorderColor(BorderGray).Background(Colors.White).Padding(7).Column(column =>
        {
            column.Item().Text(title).Bold().FontSize(8.5f).FontColor(BrandBlue);
            column.Item().PaddingTop(4).Column(body =>
            {
                body.Spacing(3);
                foreach (var field in visible)
                {
                    body.Item().Row(row =>
                    {
                        row.ConstantItem(108).Text(field.Label).FontSize(7).FontColor(LabelGray);
                        row.RelativeItem().Text(field.Value!).FontSize(8);
                    });
                }
            });
        });
    }

    private static void ComposeSignatureBlock(IContainer container, string title)
    {
        container.Column(column =>
        {
            column.Item().Text(title).Bold().FontSize(8).FontColor(LabelGray);
            column.Item().PaddingTop(18).Row(row =>
            {
                row.RelativeItem().BorderBottom(1).BorderColor(BorderGray).Height(1);
                row.ConstantItem(8);
                row.ConstantItem(80).BorderBottom(1).BorderColor(BorderGray).Height(1);
            });
            column.Item().PaddingTop(2).Row(row =>
            {
                row.RelativeItem().Text("подпись").FontSize(6).FontColor(LabelGray);
                row.ConstantItem(8);
                row.ConstantItem(80).AlignCenter().Text("расшифровка").FontSize(6).FontColor(LabelGray);
            });
        });
    }

    private static void ComposeFooter(IContainer container, VisitWorkOrderData data)
    {
        container.PaddingTop(6).Column(column =>
        {
            column.Item().LineHorizontal(0.7f).LineColor(BorderGray);
            column.Item().PaddingTop(5).AlignCenter().Text(text =>
            {
                var orgName = string.IsNullOrWhiteSpace(data.Organization.OrganizationName)
                    ? "WWS Ratchet"
                    : data.Organization.OrganizationName;

                text.Span(orgName).FontSize(7).FontColor(LabelGray);
                text.Span("  ·  ").FontSize(7).FontColor(LabelGray);
                text.Span($"сформировано {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(7).FontColor(LabelGray);
                text.Span("  ·  ").FontSize(7).FontColor(LabelGray);
                text.CurrentPageNumber().FontSize(7).FontColor(LabelGray);
                text.Span(" / ").FontSize(7).FontColor(LabelGray);
                text.TotalPages().FontSize(7).FontColor(LabelGray);
            });
        });
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.Background(SoftGray).Border(1).BorderColor(BorderGray).PaddingVertical(3).PaddingHorizontal(4);

    private static IContainer BodyCell(IContainer container) =>
        container.Border(1).BorderColor(BorderGray).PaddingVertical(3).PaddingHorizontal(4).AlignMiddle();

    private static string BuildContactLine(OrganizationDocumentInfo organization)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(organization.Phone))
        {
            parts.Add($"тел. {organization.Phone}");
        }

        if (!string.IsNullOrWhiteSpace(organization.Email))
        {
            parts.Add(organization.Email);
        }

        return string.Join("  ·  ", parts);
    }

    private static string BuildTaxLine(OrganizationDocumentInfo organization)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(organization.Inn))
        {
            parts.Add($"ИНН {organization.Inn}");
        }

        if (!string.IsNullOrWhiteSpace(organization.Kpp))
        {
            parts.Add($"КПП {organization.Kpp}");
        }

        if (!string.IsNullOrWhiteSpace(organization.Ogrn))
        {
            parts.Add($"ОГРН {organization.Ogrn}");
        }

        return string.Join("   ", parts);
    }

    private static bool HasBankDetails(OrganizationDocumentInfo organization) =>
        !string.IsNullOrWhiteSpace(organization.BankAccount)
        || !string.IsNullOrWhiteSpace(organization.Bik);
}
