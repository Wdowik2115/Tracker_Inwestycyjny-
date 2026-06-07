using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Investe.Application.Services;
using Investe.Application.Interfaces.Services;
using Investe.Infrastructure.Persistence.UnitOfWork;
using Investe.Infrastructure.Persistence.Repositories;
using Investe.Domain.Entities;
using Microsoft.Extensions.Logging;
using QuestPDF.Infrastructure;

namespace Serwer.Tests.Application.Services
{
    public class ReportServiceTests
    {
        static ReportServiceTests()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        private static (
            Mock<IUnitOfWork> uow,
            Mock<IUserRepository> userRepo,
            Mock<IWalletRepository> walletRepo,
            Mock<IAssetRepository> assetRepo,
            Mock<ITransactionRepository> transRepo,
            Mock<IReportRepository> reportRepo,
            Mock<ICoinPriceService> priceService,
            ReportService sut)
            BuildSut()
        {
            var uow = new Mock<IUnitOfWork>();
            var userRepo = new Mock<IUserRepository>();
            var walletRepo = new Mock<IWalletRepository>();
            var assetRepo = new Mock<IAssetRepository>();
            var transRepo = new Mock<ITransactionRepository>();
            var reportRepo = new Mock<IReportRepository>();
            var priceService = new Mock<ICoinPriceService>();
            var logger = new Mock<ILogger<ReportService>>();

            uow.Setup(u => u.Users).Returns(userRepo.Object);
            uow.Setup(u => u.Wallets).Returns(walletRepo.Object);
            uow.Setup(u => u.Assets).Returns(assetRepo.Object);
            uow.Setup(u => u.Transactions).Returns(transRepo.Object);
            uow.Setup(u => u.Reports).Returns(reportRepo.Object);

            var sut = new ReportService(uow.Object, priceService.Object, logger.Object);
            return (uow, userRepo, walletRepo, assetRepo, transRepo, reportRepo, priceService, sut);
        }

        // ── GenerateAccountReportAsync ────────────────────────────────────────────

        [Fact]
        public async Task GenerateAccountReportAsync_UserNotFound_ThrowsKeyNotFoundException()
        {
            var (_, userRepo, _, _, _, _, _, sut) = BuildSut();
            var userId = Guid.NewGuid();

            userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => sut.GenerateAccountReportAsync(userId));
        }

        [Fact]
        public async Task GenerateAccountReportAsync_NoWallets_SavesReportAndReturnsDto()
        {
            var (uow, userRepo, walletRepo, _, _, reportRepo, _, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Email = "test@test.com" };

            userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            walletRepo.Setup(r => r.GetWalletsByUserIdAsync(userId)).ReturnsAsync(new List<Wallet>());
            reportRepo.Setup(r => r.AddAsync(It.IsAny<Report>())).ReturnsAsync((Report r) => r);

            var result = await sut.GenerateAccountReportAsync(userId);

            Assert.NotNull(result);
            Assert.Equal(ReportType.Account.ToString(), result.Type);
            Assert.Null(result.WalletName);
            reportRepo.Verify(r => r.AddAsync(It.IsAny<Report>()), Times.Once);
            uow.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task GenerateAccountReportAsync_StoresPdfBytesInEntity()
        {
            var (_, userRepo, walletRepo, _, _, reportRepo, _, sut) = BuildSut();
            var userId = Guid.NewGuid();

            userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User { Id = userId, Email = "x@x.com" });
            walletRepo.Setup(r => r.GetWalletsByUserIdAsync(userId)).ReturnsAsync(new List<Wallet>());

            Report? saved = null;
            reportRepo.Setup(r => r.AddAsync(It.IsAny<Report>()))
                .Callback<Report>(r => saved = r)
                .ReturnsAsync((Report r) => r);

            await sut.GenerateAccountReportAsync(userId);

            Assert.NotNull(saved);
            Assert.NotEmpty(saved!.Content);
            Assert.True(saved.FileSizeBytes > 0);
            Assert.Equal(saved.Content.Length, (int)saved.FileSizeBytes);
        }

        [Fact]
        public async Task GenerateAccountReportAsync_TitleContainsCurrentMonth()
        {
            var (_, userRepo, walletRepo, _, _, reportRepo, _, sut) = BuildSut();
            var userId = Guid.NewGuid();

            userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User { Id = userId, Email = "x@x.com" });
            walletRepo.Setup(r => r.GetWalletsByUserIdAsync(userId)).ReturnsAsync(new List<Wallet>());

            Report? saved = null;
            reportRepo.Setup(r => r.AddAsync(It.IsAny<Report>()))
                .Callback<Report>(r => saved = r)
                .ReturnsAsync((Report r) => r);

            await sut.GenerateAccountReportAsync(userId);

            Assert.Contains(DateTime.UtcNow.Year.ToString(), saved!.Title);
            Assert.Contains("Account Report", saved.Title);
        }

        [Fact]
        public async Task GenerateAccountReportAsync_SetsUserIdAndNullWalletId()
        {
            var (_, userRepo, walletRepo, _, _, reportRepo, _, sut) = BuildSut();
            var userId = Guid.NewGuid();

            userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User { Id = userId, Email = "x@x.com" });
            walletRepo.Setup(r => r.GetWalletsByUserIdAsync(userId)).ReturnsAsync(new List<Wallet>());

            Report? saved = null;
            reportRepo.Setup(r => r.AddAsync(It.IsAny<Report>()))
                .Callback<Report>(r => saved = r)
                .ReturnsAsync((Report r) => r);

            await sut.GenerateAccountReportAsync(userId);

            Assert.Equal(userId, saved!.UserId);
            Assert.Null(saved.WalletId);
            Assert.Equal(ReportType.Account, saved.Type);
        }

        [Fact]
        public async Task GenerateAccountReportAsync_WithWallets_FetchesPricesAndTransactions()
        {
            var (_, userRepo, walletRepo, assetRepo, transRepo, reportRepo, priceService, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();

            userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User { Id = userId, Email = "x@x.com" });
            walletRepo.Setup(r => r.GetWalletsByUserIdAsync(userId))
                .ReturnsAsync(new List<Wallet> { new() { Id = walletId, UserId = userId, Name = "BTC Wallet" } });
            assetRepo.Setup(r => r.GetAssetsByWalletIdAsync(walletId))
                .ReturnsAsync(new List<Asset> { new() { Symbol = "BTC", Quantity = 1m, AverageBuyPrice = 50_000m } });
            priceService.Setup(p => p.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["BTC"] = 60_000m });
            transRepo.Setup(r => r.GetTransactionsByWalletIdAsync(walletId))
                .ReturnsAsync(new List<Transaction>());
            reportRepo.Setup(r => r.AddAsync(It.IsAny<Report>())).ReturnsAsync((Report r) => r);

            await sut.GenerateAccountReportAsync(userId);

            priceService.Verify(p => p.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
            transRepo.Verify(r => r.GetTransactionsByWalletIdAsync(walletId), Times.Once);
        }

        // ── GenerateWalletReportAsync ─────────────────────────────────────────────

        [Fact]
        public async Task GenerateWalletReportAsync_WalletNotFound_ThrowsKeyNotFoundException()
        {
            var (_, _, walletRepo, _, _, _, _, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();

            walletRepo.Setup(r => r.GetByIdAsync(walletId)).ReturnsAsync((Wallet?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => sut.GenerateWalletReportAsync(userId, walletId));
        }

        [Fact]
        public async Task GenerateWalletReportAsync_WrongUser_ThrowsUnauthorizedAccessException()
        {
            var (_, _, walletRepo, _, _, _, _, sut) = BuildSut();
            var walletId = Guid.NewGuid();

            walletRepo.Setup(r => r.GetByIdAsync(walletId))
                .ReturnsAsync(new Wallet { Id = walletId, UserId = Guid.NewGuid() });

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.GenerateWalletReportAsync(Guid.NewGuid(), walletId));
        }

        [Fact]
        public async Task GenerateWalletReportAsync_SavesReportWithCorrectWalletId()
        {
            var (uow, _, walletRepo, assetRepo, transRepo, reportRepo, priceService, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();
            var wallet = new Wallet { Id = walletId, UserId = userId, Name = "ETH Wallet" };

            walletRepo.Setup(r => r.GetByIdAsync(walletId)).ReturnsAsync(wallet);
            assetRepo.Setup(r => r.GetAssetsByWalletIdAsync(walletId)).ReturnsAsync(new List<Asset>());
            priceService.Setup(p => p.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new Dictionary<string, decimal>());
            transRepo.Setup(r => r.GetTransactionsByWalletIdAsync(walletId)).ReturnsAsync(new List<Transaction>());

            Report? saved = null;
            reportRepo.Setup(r => r.AddAsync(It.IsAny<Report>()))
                .Callback<Report>(r => saved = r)
                .ReturnsAsync((Report r) => r);

            var result = await sut.GenerateWalletReportAsync(userId, walletId);

            Assert.Equal(walletId, saved!.WalletId);
            Assert.Equal(userId, saved.UserId);
            Assert.Equal(ReportType.Wallet, saved.Type);
            Assert.Equal("ETH Wallet", result.WalletName);
            uow.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task GenerateWalletReportAsync_TitleContainsWalletName()
        {
            var (_, _, walletRepo, assetRepo, transRepo, reportRepo, priceService, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();

            walletRepo.Setup(r => r.GetByIdAsync(walletId))
                .ReturnsAsync(new Wallet { Id = walletId, UserId = userId, Name = "SOL Wallet" });
            assetRepo.Setup(r => r.GetAssetsByWalletIdAsync(walletId)).ReturnsAsync(new List<Asset>());
            priceService.Setup(p => p.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new Dictionary<string, decimal>());
            transRepo.Setup(r => r.GetTransactionsByWalletIdAsync(walletId)).ReturnsAsync(new List<Transaction>());

            Report? saved = null;
            reportRepo.Setup(r => r.AddAsync(It.IsAny<Report>()))
                .Callback<Report>(r => saved = r)
                .ReturnsAsync((Report r) => r);

            await sut.GenerateWalletReportAsync(userId, walletId);

            Assert.Contains("SOL Wallet", saved!.Title);
        }

        [Fact]
        public async Task GenerateWalletReportAsync_StoresPdfBytesInEntity()
        {
            var (_, _, walletRepo, assetRepo, transRepo, reportRepo, priceService, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();

            walletRepo.Setup(r => r.GetByIdAsync(walletId))
                .ReturnsAsync(new Wallet { Id = walletId, UserId = userId, Name = "W" });
            assetRepo.Setup(r => r.GetAssetsByWalletIdAsync(walletId)).ReturnsAsync(new List<Asset>());
            priceService.Setup(p => p.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new Dictionary<string, decimal>());
            transRepo.Setup(r => r.GetTransactionsByWalletIdAsync(walletId)).ReturnsAsync(new List<Transaction>());

            Report? saved = null;
            reportRepo.Setup(r => r.AddAsync(It.IsAny<Report>()))
                .Callback<Report>(r => saved = r)
                .ReturnsAsync((Report r) => r);

            await sut.GenerateWalletReportAsync(userId, walletId);

            Assert.NotEmpty(saved!.Content);
            Assert.True(saved.FileSizeBytes > 0);
            Assert.Equal(saved.Content.Length, (int)saved.FileSizeBytes);
        }

        // ── GetReportsAsync ───────────────────────────────────────────────────────

        [Fact]
        public async Task GetReportsAsync_NoReports_ReturnsEmpty()
        {
            var (_, _, _, _, _, reportRepo, _, sut) = BuildSut();
            var userId = Guid.NewGuid();

            reportRepo.Setup(r => r.GetReportsByUserIdAsync(userId)).ReturnsAsync(new List<Report>());

            var result = await sut.GetReportsAsync(userId);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetReportsAsync_ReturnsMappedDtos()
        {
            var (_, _, _, _, _, reportRepo, _, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();

            var reports = new List<Report>
            {
                new()
                {
                    Id = Guid.NewGuid(), UserId = userId, Type = ReportType.Account,
                    Title = "Account Report – June 2026", FileName = "account.pdf",
                    FileSizeBytes = 1024, GeneratedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(), UserId = userId, WalletId = walletId, Type = ReportType.Wallet,
                    Title = "BTC Wallet Report – June 2026", FileName = "wallet.pdf",
                    FileSizeBytes = 2048, GeneratedAt = DateTime.UtcNow,
                    Wallet = new Wallet { Id = walletId, Name = "BTC Wallet" }
                }
            };

            reportRepo.Setup(r => r.GetReportsByUserIdAsync(userId)).ReturnsAsync(reports);

            var result = (await sut.GetReportsAsync(userId)).ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal("Account", result[0].Type);
            Assert.Null(result[0].WalletName);
            Assert.Equal("Wallet", result[1].Type);
            Assert.Equal("BTC Wallet", result[1].WalletName);
            Assert.Equal(1024, result[0].FileSizeBytes);
            Assert.Equal(2048, result[1].FileSizeBytes);
        }

        // ── GetReportFileAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task GetReportFileAsync_ReportNotFound_ThrowsKeyNotFoundException()
        {
            var (_, _, _, _, _, reportRepo, _, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var reportId = Guid.NewGuid();

            reportRepo.Setup(r => r.GetReportByIdAndUserIdAsync(reportId, userId)).ReturnsAsync((Report?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => sut.GetReportFileAsync(userId, reportId));
        }

        [Fact]
        public async Task GetReportFileAsync_ReturnsContentAndFileName()
        {
            var (_, _, _, _, _, reportRepo, _, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var reportId = Guid.NewGuid();
            var pdfBytes = new byte[] { 1, 2, 3, 4, 5 };

            reportRepo.Setup(r => r.GetReportByIdAndUserIdAsync(reportId, userId))
                .ReturnsAsync(new Report
                {
                    Id = reportId, UserId = userId,
                    Content = pdfBytes, FileName = "my_report.pdf"
                });

            var (content, fileName) = await sut.GetReportFileAsync(userId, reportId);

            Assert.Equal(pdfBytes, content);
            Assert.Equal("my_report.pdf", fileName);
        }

        // ── DeleteReportAsync ─────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteReportAsync_ReportNotFound_ThrowsKeyNotFoundException()
        {
            var (_, _, _, _, _, reportRepo, _, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var reportId = Guid.NewGuid();

            reportRepo.Setup(r => r.GetReportByIdAndUserIdAsync(reportId, userId)).ReturnsAsync((Report?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => sut.DeleteReportAsync(userId, reportId));
        }

        [Fact]
        public async Task DeleteReportAsync_CallsDeleteAndCompletes()
        {
            var (uow, _, _, _, _, reportRepo, _, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var reportId = Guid.NewGuid();
            var report = new Report { Id = reportId, UserId = userId, Content = Array.Empty<byte>() };

            reportRepo.Setup(r => r.GetReportByIdAndUserIdAsync(reportId, userId)).ReturnsAsync(report);
            reportRepo.Setup(r => r.DeleteAsync(report)).Returns(Task.CompletedTask);

            await sut.DeleteReportAsync(userId, reportId);

            reportRepo.Verify(r => r.DeleteAsync(report), Times.Once);
            uow.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteReportAsync_CannotDeleteAnotherUsersReport()
        {
            var (_, _, _, _, _, reportRepo, _, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var reportId = Guid.NewGuid();

            reportRepo.Setup(r => r.GetReportByIdAndUserIdAsync(reportId, userId)).ReturnsAsync((Report?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => sut.DeleteReportAsync(userId, reportId));

            reportRepo.Verify(r => r.DeleteAsync(It.IsAny<Report>()), Times.Never);
        }
    }
}
