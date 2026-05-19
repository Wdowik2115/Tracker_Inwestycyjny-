using System;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Investe.Application.Services;
using Investe.Application.DTOs;
using Investe.Infrastructure.Persistence.UnitOfWork;
using Investe.Infrastructure.Persistence.Repositories;
using Investe.Domain.Entities;

namespace Serwer.Tests.Application.Services
{
    public class UserServiceTests
    {
        private (Mock<IUnitOfWork>, Mock<IUserRepository>, UserService) BuildSut()
        {
            var uow = new Mock<IUnitOfWork>();
            var userRepo = new Mock<IUserRepository>();
            uow.Setup(u => u.Users).Returns(userRepo.Object);
            var svc = new UserService(uow.Object);
            return (uow, userRepo, svc);
        }

        [Fact]
        public async Task GetProfileAsync_ExistingUser_ReturnsProfile()
        {
            var userId = Guid.NewGuid();
            var (uow, userRepo, svc) = BuildSut();
            var user = new User { Id = userId, Email = "test@test.com", FirstName = "John", LastName = "Doe" };
            userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            var result = await svc.GetProfileAsync(userId);

            Assert.Equal("John", result.FirstName);
            Assert.Equal("test@test.com", result.Email);
        }

        [Fact]
        public async Task UpdateProfileAsync_UpdatesFields()
        {
            var userId = Guid.NewGuid();
            var (uow, userRepo, svc) = BuildSut();
            var user = new User { Id = userId };
            userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            var dto = new UpdateProfileDto { FirstName = "Jane", LastName = "Smith", PreferredCurrency = "EUR" };
            await svc.UpdateProfileAsync(userId, dto);

            Assert.Equal("Jane", user.FirstName);
            Assert.Equal("Smith", user.LastName);
            Assert.Equal("EUR", user.PreferredCurrency);
            uow.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_IncorrectOldPassword_ThrowsArgumentException()
        {
            var userId = Guid.NewGuid();
            var (uow, userRepo, svc) = BuildSut();
            var user = new User { Id = userId, PasswordHash = BCrypt.Net.BCrypt.HashPassword("old_pass") };
            userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            var dto = new ChangePasswordDto { OldPassword = "wrong_pass", NewPassword = "new_pass" };

            await Assert.ThrowsAsync<ArgumentException>(() => svc.ChangePasswordAsync(userId, dto));
        }

        [Fact]
        public async Task ChangePasswordAsync_CorrectOldPassword_UpdatesHash()
        {
            var userId = Guid.NewGuid();
            var (uow, userRepo, svc) = BuildSut();
            var user = new User { Id = userId, PasswordHash = BCrypt.Net.BCrypt.HashPassword("old_pass") };
            userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            var dto = new ChangePasswordDto { OldPassword = "old_pass", NewPassword = "NewStrongPassword123!" };
            await svc.ChangePasswordAsync(userId, dto);

            Assert.True(BCrypt.Net.BCrypt.Verify("NewStrongPassword123!", user.PasswordHash));
            uow.Verify(u => u.CompleteAsync(), Times.Once);
        }
    }
}
