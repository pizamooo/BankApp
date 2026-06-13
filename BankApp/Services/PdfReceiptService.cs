using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using BankApp.Models;

namespace BankApp.Services
{
    public class PdfReceiptService
    {
        public byte[] GenerateTransferReceipt(TransferHistoryItem transfer, string clientName)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(25, Unit.Millimetre);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Content().Element(x => ComposeReceipt(x, transfer, clientName));
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerateTopUpReceipt(TopUpHistoryItem topUp, string clientName)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(25, Unit.Millimetre);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Content().Element(x => ComposeTopUpReceipt(x, topUp, clientName));
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeReceipt(IContainer container, TransferHistoryItem t, string clientName)
        {
            container.Column(column =>
            {
                column.Spacing(12);

                column.Item().AlignCenter().Text("BankApp").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                column.Item().AlignCenter().Text("Кассовый чек").FontSize(14).Bold();

                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                column.Item().Text($"Клиент: {clientName}").FontSize(12);
                column.Item().Text($"Дата: {t.DateText}").FontSize(12);

                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                column.Item().Text(t.Title).FontSize(13).Bold();

                column.Item().Text($"Описание: {t.Description ?? "Перевод"}").FontSize(12);

                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                // Сумма крупно
                column.Item().AlignCenter().Text(t.AmountText)
                    .FontSize(26).Bold()
                    .FontColor(t.IsIncoming ? Colors.Green.Medium : Colors.Red.Medium);

                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                column.Item().AlignCenter().Text("Операция подтверждена").FontSize(10);
                column.Item().AlignCenter().Text($"Чек № {t.Id}").FontSize(10);
                column.Item().AlignCenter().Text(DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
            });
        }

        private void ComposeTopUpReceipt(IContainer container, TopUpHistoryItem t, string clientName)
        {
            container.Column(column =>
            {
                column.Spacing(12);

                // Заголовок
                column.Item().AlignCenter().Text("BankApp").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                column.Item().AlignCenter().Text("Квитанция о пополнении").FontSize(14).Bold();

                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                // Информация о клиенте
                column.Item().Text($"Клиент: {clientName}").FontSize(12);
                column.Item().Text($"Дата: {t.Date}").FontSize(12);

                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                column.Item().Text("Пополнение счёта").FontSize(13).Bold();
                column.Item().Text($"Карта: {t.Card}").FontSize(12);

                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                // Сумма крупно
                column.Item().AlignCenter().Text(t.Amount)
                    .FontSize(26).Bold()
                    .FontColor(Colors.Green.Medium);

                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                // Исправленная строка
                string receiptNumber = t.Id > 0 ? t.Id.ToString() : "—";
                column.Item().AlignCenter().Text($"Чек № {receiptNumber}").FontSize(10);

                column.Item().AlignCenter().Text(DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
            });
        }
    }
}