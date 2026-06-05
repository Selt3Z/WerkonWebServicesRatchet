using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace WerkonWebServicesRatchet.Infrastructure.Pdf;

public sealed class VisitWorkOrderPdfGenerator
{
    public byte[] Generate(VisitWorkOrderData data)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(style => style.FontSize(10).FontFamily("Arial"));

                page.Content().Column(column =>
                {
                    column.Spacing(6);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Spacing(4);
                            left.Item().Text("ЗАКАЗ-НАРЯД").Bold().FontSize(18);
                            left.Item().Text($"№ {data.VisitId.ToString()[..8].ToUpper()}");
                            left.Item().Text($"Дата: {data.VisitedAtLocal:dd.MM.yyyy HH:mm}");
                        });

                        if (data.Organization.LogoBytes is { Length: > 0 })
                        {
                            row.ConstantItem(120).AlignRight().Height(60).Image(data.Organization.LogoBytes).FitArea();
                        }
                    });

                    column.Item().PaddingTop(8).Text("Исполнитель").Bold().FontSize(11);
                    AppendOrganizationBlock(column, data.Organization);

                    column.Item().PaddingTop(8).LineHorizontal(1);

                    column.Item().PaddingTop(4).Text("Заказчик").Bold().FontSize(11);
                    column.Item().Text($"ФИО / организация: {data.ClientFullName}");
                    column.Item().Text($"Телефон: {data.ClientPhoneNumber}");

                    column.Item().PaddingTop(8).Text("Автомобиль").Bold().FontSize(11);
                    column.Item().Text($"Марка / модель: {data.VehicleBrand} {data.VehicleModel}");
                    column.Item().Text($"Госномер: {data.LicensePlate}");

                    if (!string.IsNullOrWhiteSpace(data.Vin))
                    {
                        column.Item().Text($"VIN: {data.Vin}");
                    }

                    if (data.MileageAtVisit.HasValue)
                    {
                        column.Item().Text($"Пробег: {data.MileageAtVisit.Value}");
                    }

                    column.Item().Text($"Жалоба / причина обращения: {data.CustomerComplaint}");

                    if (!string.IsNullOrWhiteSpace(data.AssignedMechanicDisplayName))
                    {
                        column.Item().Text($"Ответственный мастер: {data.AssignedMechanicDisplayName}");
                    }

                    if (!string.IsNullOrWhiteSpace(data.MechanicComment))
                    {
                        column.Item().Text($"Комментарий мастера: {data.MechanicComment}");
                    }

                    column.Item().PaddingTop(12).Text("Выполненные работы").Bold().FontSize(12);

                    if (data.Items.Count == 0)
                    {
                        column.Item().Text("Работы не указаны.");
                    }
                    else
                    {
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Наименование").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Кол-во").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Цена").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Сумма").Bold();
                            });

                            foreach (var item in data.Items)
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.Name);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.Quantity.ToString("0.##"));
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.UnitPrice.ToString("0.00"));
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(item.TotalPrice.ToString("0.00"));
                            }
                        });
                    }

                    column.Item().PaddingTop(12).AlignRight().Text($"ИТОГО: {data.TotalAmount:0.00}").Bold().FontSize(13);

                    if (HasBankDetails(data.Organization))
                    {
                        column.Item().PaddingTop(12).Text("Платёжные реквизиты").Bold().FontSize(11);
                        AppendBankDetails(column, data.Organization);
                    }
                });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span(data.Organization.OrganizationName);
                        text.Span(" · ");
                        text.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                    });
            });
        }).GeneratePdf();
    }

    private static void AppendOrganizationBlock(ColumnDescriptor column, OrganizationDocumentInfo organization)
    {
        if (!string.IsNullOrWhiteSpace(organization.OrganizationName))
        {
            column.Item().Text(organization.OrganizationName);
        }

        if (!string.IsNullOrWhiteSpace(organization.Address))
        {
            column.Item().Text($"Адрес: {organization.Address}");
        }

        var taxLine = BuildTaxLine(organization);
        if (!string.IsNullOrWhiteSpace(taxLine))
        {
            column.Item().Text(taxLine);
        }

        if (!string.IsNullOrWhiteSpace(organization.Phone))
        {
            column.Item().Text($"Телефон: {organization.Phone}");
        }

        if (!string.IsNullOrWhiteSpace(organization.Email))
        {
            column.Item().Text($"Email: {organization.Email}");
        }
    }

    private static void AppendBankDetails(ColumnDescriptor column, OrganizationDocumentInfo organization)
    {
        if (!string.IsNullOrWhiteSpace(organization.BankAccount))
        {
            column.Item().Text($"Расчётный счёт: {organization.BankAccount}");
        }

        if (!string.IsNullOrWhiteSpace(organization.Bik))
        {
            column.Item().Text($"БИК: {organization.Bik}");
        }
    }

    private static string BuildTaxLine(OrganizationDocumentInfo organization)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(organization.Inn))
        {
            parts.Add($"ИНН: {organization.Inn}");
        }

        if (!string.IsNullOrWhiteSpace(organization.Kpp))
        {
            parts.Add($"КПП: {organization.Kpp}");
        }

        if (!string.IsNullOrWhiteSpace(organization.Ogrn))
        {
            parts.Add($"ОГРН: {organization.Ogrn}");
        }

        return parts.Count == 0 ? string.Empty : string.Join("   ", parts);
    }

    private static bool HasBankDetails(OrganizationDocumentInfo organization) =>
        !string.IsNullOrWhiteSpace(organization.BankAccount)
        || !string.IsNullOrWhiteSpace(organization.Bik);
}
