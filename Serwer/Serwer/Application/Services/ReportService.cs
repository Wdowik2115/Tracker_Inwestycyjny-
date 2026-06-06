using Investe.Application.DTOs;
using Investe.Application.Interfaces.Services;
using Investe.Domain.Entities;
using Investe.Infrastructure.Persistence.UnitOfWork;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Investe.Application.Services
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICoinPriceService _priceService;
        private readonly ILogger<ReportService> _logger;

        public ReportService(
            IUnitOfWork unitOfWork,
            ICoinPriceService priceService,
            ILogger<ReportService> logger)
        {
            _unitOfWork = unitOfWork;
            _priceService = priceService;
            _logger = logger;
        }

        public async Task<ReportDto> GenerateAccountReportAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException("User not found.");

            var wallets = (await _unitOfWork.Wallets.GetWalletsByUserIdAsync(userId)).ToList();

            var walletData = new List<WalletReportData>();
            foreach (var wallet in wallets)
            {
                var assets = (await _unitOfWork.Assets.GetAssetsByWalletIdAsync(wallet.Id)).ToList();
                var symbols = assets.Select(a => a.Symbol).Distinct();
                var prices = await _priceService.GetCurrentPricesAsync(symbols);
                var transactions = (await _unitOfWork.Transactions.GetTransactionsByWalletIdAsync(wallet.Id))
                    .OrderByDescending(t => t.ExecutedAt).ToList();

                walletData.Add(BuildWalletData(wallet, assets, prices, transactions));
            }

            var title = $"Account Report – {DateTime.UtcNow:MMMM yyyy}";
            var pdfBytes = GenerateAccountPdf(user, walletData, title);
            var report = await SaveReportAsync(userId, null, ReportType.Account, title, pdfBytes);

            return ToDto(report, null);
        }

        public async Task<ReportDto> GenerateWalletReportAsync(Guid userId, Guid walletId)
        {
            var wallet = await _unitOfWork.Wallets.GetByIdAsync(walletId)
                ?? throw new KeyNotFoundException("Wallet not found.");

            if (wallet.UserId != userId)
                throw new UnauthorizedAccessException("Wallet does not belong to this user.");

            var assets = (await _unitOfWork.Assets.GetAssetsByWalletIdAsync(walletId)).ToList();
            var symbols = assets.Select(a => a.Symbol).Distinct();
            var prices = await _priceService.GetCurrentPricesAsync(symbols);
            var transactions = (await _unitOfWork.Transactions.GetTransactionsByWalletIdAsync(walletId))
                .OrderByDescending(t => t.ExecutedAt).ToList();

            var data = BuildWalletData(wallet, assets, prices, transactions);
            var title = $"{wallet.Name} Report – {DateTime.UtcNow:MMMM yyyy}";
            var pdfBytes = GenerateWalletPdf(data, title);
            var report = await SaveReportAsync(userId, walletId, ReportType.Wallet, title, pdfBytes);

            return ToDto(report, wallet.Name);
        }

        public async Task<IEnumerable<ReportDto>> GetReportsAsync(Guid userId)
        {
            var reports = await _unitOfWork.Reports.GetReportsByUserIdAsync(userId);
            return reports.Select(r => ToDto(r, r.Wallet?.Name));
        }

        public async Task<(byte[] Content, string FileName)> GetReportFileAsync(Guid userId, Guid reportId)
        {
            var report = await _unitOfWork.Reports.GetReportByIdAndUserIdAsync(reportId, userId)
                ?? throw new KeyNotFoundException("Report not found.");

            return (report.Content, report.FileName);
        }

        public async Task DeleteReportAsync(Guid userId, Guid reportId)
        {
            var report = await _unitOfWork.Reports.GetReportByIdAndUserIdAsync(reportId, userId)
                ?? throw new KeyNotFoundException("Report not found.");

            await _unitOfWork.Reports.DeleteAsync(report);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Report {ReportId} deleted by user {UserId}", reportId, userId);
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private static WalletReportData BuildWalletData(
            Wallet wallet,
            List<Asset> assets,
            Dictionary<string, decimal> prices,
            List<Transaction> transactions)
        {
            var positions = assets.Select(a =>
            {
                var price = prices.GetValueOrDefault(a.Symbol, 0m);
                var value = a.Quantity * price;
                var cost = a.Quantity * a.AverageBuyPrice;
                var pnl = value - cost;
                var pnlPct = cost > 0 ? pnl / cost * 100m : 0m;
                return new PositionData(a.Symbol, a.Name, a.Quantity, a.AverageBuyPrice, price, value, pnl, pnlPct);
            }).ToList();

            var totalValue = positions.Sum(p => p.Value);
            var totalCost = positions.Sum(p => p.Quantity * p.AvgCost);
            var totalPnl = totalValue - totalCost;
            var totalPnlPct = totalCost > 0 ? totalPnl / totalCost * 100m : 0m;

            return new WalletReportData(wallet.Name, wallet.Description, totalValue, totalPnl, totalPnlPct, positions, transactions);
        }

        private async Task<Report> SaveReportAsync(Guid userId, Guid? walletId, ReportType type, string title, byte[] pdfBytes)
        {
            var fileName = $"{title.Replace(" ", "_").Replace("–", "-")}.pdf";

            var report = new Report
            {
                UserId = userId,
                WalletId = walletId,
                Type = type,
                Title = title,
                FileName = fileName,
                Content = pdfBytes,
                FileSizeBytes = pdfBytes.Length,
                GeneratedAt = DateTime.UtcNow
            };

            await _unitOfWork.Reports.AddAsync(report);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Report {ReportId} generated for user {UserId} ({Bytes} bytes)", report.Id, userId, pdfBytes.Length);
            return report;
        }

        private static ReportDto ToDto(Report report, string? walletName) => new()
        {
            Id = report.Id,
            Title = report.Title,
            FileName = report.FileName,
            Type = report.Type.ToString(),
            WalletName = walletName,
            FileSizeBytes = report.FileSizeBytes,
            GeneratedAt = report.GeneratedAt
        };

        // ── QuestPDF document builders ────────────────────────────────────────

        private static byte[] GenerateAccountPdf(User user, List<WalletReportData> wallets, string title)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(t => t.FontSize(10).FontColor("#1a1a2e"));

                    page.Header().Element(c => RenderHeader(c, title, $"Generated: {DateTime.UtcNow:dd MMM yyyy, HH:mm} UTC"));
                    page.Footer().Element(RenderFooter);

                    page.Content().Column(col =>
                    {
                        col.Spacing(16);

                        // User info
                        col.Item().Text($"Account: {user.Email}").FontSize(11).FontColor("#4a5568");

                        // Summary across all wallets
                        var totalValue = wallets.Sum(w => w.TotalValue);
                        var totalPnl = wallets.Sum(w => w.TotalPnl);
                        var totalPnlPct = totalValue > 0 ? totalPnl / (totalValue - totalPnl) * 100m : 0m;

                        col.Item().Element(c => RenderSummaryRow(c, totalValue, totalPnl, totalPnlPct));

                        // Per-wallet sections
                        foreach (var wallet in wallets)
                        {
                            col.Item().Element(c => RenderWalletSection(c, wallet));
                        }
                    });
                });
            }).GeneratePdf();
        }

        private static byte[] GenerateWalletPdf(WalletReportData wallet, string title)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(t => t.FontSize(10).FontColor("#1a1a2e"));

                    page.Header().Element(c => RenderHeader(c, title, $"Generated: {DateTime.UtcNow:dd MMM yyyy, HH:mm} UTC"));
                    page.Footer().Element(RenderFooter);

                    page.Content().Column(col =>
                    {
                        col.Spacing(16);

                        if (!string.IsNullOrEmpty(wallet.Description))
                            col.Item().Text(wallet.Description).FontSize(11).FontColor("#4a5568");

                        col.Item().Element(c => RenderSummaryRow(c, wallet.TotalValue, wallet.TotalPnl, wallet.TotalPnlPct));
                        col.Item().Element(c => RenderWalletSection(c, wallet));
                    });
                });
            }).GeneratePdf();
        }

        private static void RenderHeader(IContainer container, string title, string subtitle)
        {
            container.Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Investee").FontSize(20).Bold().FontColor("#2563eb");
                        c.Item().Text(title).FontSize(14).SemiBold().FontColor("#1a1a2e");
                        c.Item().Text(subtitle).FontSize(9).FontColor("#718096");
                    });
                });
                col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#e2e8f0");
            });
        }

        private static void RenderFooter(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().LineHorizontal(1).LineColor("#e2e8f0");
                col.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().Text("Investee – Investment Tracker").FontSize(8).FontColor("#a0aec0");
                    row.ConstantItem(80).AlignRight().Text(t =>
                    {
                        t.Span("Page ").FontSize(8).FontColor("#a0aec0");
                        t.CurrentPageNumber().FontSize(8).FontColor("#a0aec0");
                        t.Span(" / ").FontSize(8).FontColor("#a0aec0");
                        t.TotalPages().FontSize(8).FontColor("#a0aec0");
                    });
                });
            });
        }

        private static void RenderSummaryRow(IContainer container, decimal totalValue, decimal totalPnl, decimal totalPnlPct)
        {
            var pnlColor = totalPnl >= 0 ? "#16a34a" : "#dc2626";
            container.Background("#f8fafc").Padding(12).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("TOTAL VALUE").FontSize(8).FontColor("#718096");
                    c.Item().Text(FormatCurrency(totalValue)).FontSize(16).Bold();
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("TOTAL P&L").FontSize(8).FontColor("#718096");
                    c.Item().Text($"{(totalPnl >= 0 ? "+" : "")}{FormatCurrency(totalPnl)} ({(totalPnlPct >= 0 ? "+" : "")}{totalPnlPct:F2}%)")
                        .FontSize(13).Bold().FontColor(pnlColor);
                });
            });
        }

        private static void RenderWalletSection(IContainer container, WalletReportData wallet)
        {
            container.Column(col =>
            {
                col.Spacing(8);

                // Wallet heading
                col.Item().Background("#1e3a5f").Padding(10).Row(row =>
                {
                    row.RelativeItem().Text(wallet.Name).FontSize(12).SemiBold().FontColor("#ffffff");
                    row.ConstantItem(120).AlignRight().Text(FormatCurrency(wallet.TotalValue)).FontSize(12).Bold().FontColor("#ffffff");
                });

                // Positions table
                if (wallet.Positions.Count > 0)
                {
                    col.Item().Text("Positions").FontSize(10).SemiBold().FontColor("#4a5568");
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2); // Symbol
                            cols.RelativeColumn(2); // Name
                            cols.RelativeColumn(2); // Qty
                            cols.RelativeColumn(2); // Avg Cost
                            cols.RelativeColumn(2); // Price
                            cols.RelativeColumn(2); // Value
                            cols.RelativeColumn(3); // P&L
                        });

                        table.Header(header =>
                        {
                            foreach (var h in new[] { "SYMBOL", "NAME", "QUANTITY", "AVG COST", "PRICE", "VALUE", "P&L" })
                                header.Cell().Background("#f1f5f9").Padding(4).Text(h).FontSize(8).Bold().FontColor("#64748b");
                        });

                        foreach (var pos in wallet.Positions)
                        {
                            var pnlColor = pos.Pnl >= 0 ? "#16a34a" : "#dc2626";
                            table.Cell().Padding(4).Text(pos.Symbol).FontSize(9).Bold();
                            table.Cell().Padding(4).Text(pos.Name).FontSize(9);
                            table.Cell().Padding(4).Text(FormatQty(pos.Quantity)).FontSize(9);
                            table.Cell().Padding(4).Text(FormatCurrency(pos.AvgCost)).FontSize(9);
                            table.Cell().Padding(4).Text(FormatCurrency(pos.CurrentPrice)).FontSize(9);
                            table.Cell().Padding(4).Text(FormatCurrency(pos.Value)).FontSize(9);
                            table.Cell().Padding(4).Text($"{(pos.Pnl >= 0 ? "+" : "")}{FormatCurrency(pos.Pnl)} ({pos.PnlPct:F2}%)")
                                .FontSize(9).FontColor(pnlColor);
                        }
                    });
                }

                // Transactions table
                if (wallet.Transactions.Count > 0)
                {
                    col.Item().Text("Transactions").FontSize(10).SemiBold().FontColor("#4a5568");
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2); // Date
                            cols.RelativeColumn(1); // Type
                            cols.RelativeColumn(1); // Symbol
                            cols.RelativeColumn(2); // Qty
                            cols.RelativeColumn(2); // Price
                            cols.RelativeColumn(2); // Total
                            cols.RelativeColumn(1); // Fee
                        });

                        table.Header(header =>
                        {
                            foreach (var h in new[] { "DATE", "TYPE", "SYMBOL", "QUANTITY", "PRICE", "TOTAL", "FEE" })
                                header.Cell().Background("#f1f5f9").Padding(4).Text(h).FontSize(8).Bold().FontColor("#64748b");
                        });

                        foreach (var tx in wallet.Transactions)
                        {
                            var typeColor = tx.Type == TransactionType.Buy ? "#16a34a" : "#dc2626";
                            table.Cell().Padding(4).Text(tx.ExecutedAt.ToString("dd MMM yyyy")).FontSize(9);
                            table.Cell().Padding(4).Text(tx.Type.ToString().ToUpper()).FontSize(9).Bold().FontColor(typeColor);
                            table.Cell().Padding(4).Text(tx.Symbol).FontSize(9);
                            table.Cell().Padding(4).Text(FormatQty(tx.Quantity)).FontSize(9);
                            table.Cell().Padding(4).Text(FormatCurrency(tx.PriceAtTime)).FontSize(9);
                            table.Cell().Padding(4).Text(FormatCurrency(tx.TotalValue)).FontSize(9);
                            table.Cell().Padding(4).Text(tx.Fee > 0 ? FormatCurrency(tx.Fee) : "–").FontSize(9);
                        }
                    });
                }
            });
        }

        private static string FormatCurrency(decimal v) =>
            "$" + v.ToString("N2");

        private static string FormatQty(decimal v) =>
            v.ToString("0.########");
    }

    // ── Internal data records ─────────────────────────────────────────────────

    internal record PositionData(
        string Symbol, string Name, decimal Quantity, decimal AvgCost,
        decimal CurrentPrice, decimal Value, decimal Pnl, decimal PnlPct);

    internal record WalletReportData(
        string Name, string Description, decimal TotalValue, decimal TotalPnl, decimal TotalPnlPct,
        List<PositionData> Positions, List<Transaction> Transactions);
}
