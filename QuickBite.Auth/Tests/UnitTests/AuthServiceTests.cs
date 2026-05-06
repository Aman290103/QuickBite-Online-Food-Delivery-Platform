using NUnit.Framework;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using QuickBite.Auth.DTOs;
using QuickBite.Auth.Entities;
using QuickBite.Auth.Interfaces;
using QuickBite.Auth.Services;
using System.Security.Claims;

namespace QuickBite.Auth.Tests.UnitTests
{
    [TestFixture]
    public class AuthServiceTests
    {
        private Mock<IUserRepository> _userRepoMock;
        private Mock<UserManager<User>> _userManagerMock;
        private Mock<IConfiguration> _configMock;
        private AuthService _authService;

        [SetUp]
        public void Setup()
        {
            _userRepoMock = new Mock<IUserRepository>();
            
            // Set up UserManager Mock
            var store = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
            
            _configMock = new Mock<IConfiguration>();
            
            _authService = new AuthService(
                _userManagerMock.Object,
                _userRepoMock.Object,
                _configMock.Object
            );
        }

        [Test]
        public async Task RegisterAsync_ShouldThrow_WhenEmailExists()
        {
            // Arrange
            var registerDto = new RegisterDto("John Doe", "test@test.com", "Password123!", "+91 9999988888", UserRole.CUSTOMER);
            _userRepoMock.Setup(repo => repo.ExistsByEmailAsync(registerDto.Email)).ReturnsAsync(true);

            // Act
            Func<Task> act = async () => await _authService.RegisterAsync(registerDto);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Email already registered.");
        }

        [Test]
        public async Task RegisterAsync_ShouldCreateUser_WhenValid()
        {
            // Arrange
            var registerDto = new RegisterDto("John Doe", "test@test.com", "Password123!", "+91 9999988888", UserRole.CUSTOMER);
            _userRepoMock.Setup(repo => repo.ExistsByEmailAsync(registerDto.Email)).ReturnsAsync(false);
            
            _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                            .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.RegisterAsync(registerDto);

            // Assert
            result.Email.Should().Be(registerDto.Email);
            result.FullName.Should().Be(registerDto.FullName);
            _userManagerMock.Verify(m => m.CreateAsync(It.IsAny<User>(), registerDto.Password), Times.Once);
        }

        [Test]
        public async Task LoginAsync_ShouldThrowUnauthorized_WhenUserNotFound()
        {
            // Arrange
            var loginDto = new LoginDto("invalid@test.com", "wrongpass");
            _userManagerMock.Setup(m => m.FindByEmailAsync(loginDto.Email)).ReturnsAsync((User)null);

            // Act
            Func<Task> act = async () => await _authService.LoginAsync(loginDto);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Invalid credentials.");
        }

        [Test]
        public async Task LoginAsync_ShouldReturnTokens_WhenValid()
        {
            // Arrange
            var loginDto = new LoginDto("john@test.com", "Admin@123");
            var user = new User { Id = Guid.NewGuid(), Email = loginDto.Email, FullName = "John", Role = UserRole.CUSTOMER, IsActive = true };
            
            _userManagerMock.Setup(m => m.FindByEmailAsync(loginDto.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.CheckPasswordAsync(user, loginDto.Password)).ReturnsAsync(true);
            
            _configMock.Setup(c => c["Jwt:Key"]).Returns("SuperSecretKeyForQuickBiteAuthService_DoNotUseInProduction_MustBeLongEnough");
            _configMock.Setup(c => c["Jwt:Issuer"]).Returns("QuickBite.Auth");
            _configMock.Setup(c => c["Jwt:Audience"]).Returns("QuickBite.Users");

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            result.AccessToken.Should().NotBeNullOrEmpty();
            result.RefreshToken.Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task DeactivateAccountAsync_ShouldUpdateUserStatus()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, IsActive = true };
            _userRepoMock.Setup(repo => repo.FindByUserIdAsync(userId)).ReturnsAsync(user);

            // Act
            await _authService.DeactivateAccountAsync(userId);

            // Assert
            user.IsActive.Should().BeFalse();
            _userRepoMock.Verify(repo => repo.UpdateAsync(user), Times.Once);
        }
    }
}
