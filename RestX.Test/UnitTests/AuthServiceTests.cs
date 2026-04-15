using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using RestX.BLL.DataTranferObjects.Authentication;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Auth;
using RestX.BLL.Interfaces.Customers;
using RestX.BLL.Services.Auth;
using RestX.Models.Customers;
using RestX.Models.HR;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using System.Linq.Expressions;

namespace RestX.Tests.UnitTests
{
    [TestFixture]
    public class AuthServiceTests
    {
        private Mock<IRepository> _repoMock;
        private Mock<UserManager<ApplicationUser>> _userManagerMock;
        private Mock<SignInManager<ApplicationUser>> _signInManagerMock;
        private Mock<RoleManager<IdentityRole<Guid>>> _roleManagerMock;
        private Mock<ITokenService> _tokenServiceMock;
        private Mock<IAuthLinkService> _authLinkServiceMock;
        private Mock<IEmailService> _emailServiceMock;
        private Mock<IRedisService> _redisServiceMock;
        private Mock<IMapper> _mapperMock;
        private Mock<ILogger<AuthService>> _loggerMock;
        private Mock<ICustomerService> _customerServiceMock;
        private List<ActiveTenant> _tenants;
        private AuthService _authService;

        private readonly ApplicationUser _validUser = new()
        {
            Id = Guid.NewGuid(),
            Email = "staff@restx.food",
            UserName = "staff@restx.food",
            FullName = "Test Staff",
            RefreshToken = "valid-refresh-token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7),
            PhoneNumber = "0901234567"
        };

        [SetUp]
        public void Setup()
        {
            _repoMock = new Mock<IRepository>();

            var userStore = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
                _userManagerMock.Object, contextAccessor.Object, claimsFactory.Object, null!, null!, null!, null!);

            var roleStore = new Mock<IRoleStore<IdentityRole<Guid>>>();
            _roleManagerMock = new Mock<RoleManager<IdentityRole<Guid>>>(
                roleStore.Object, null!, null!, null!, null!);

            _tokenServiceMock = new Mock<ITokenService>();
            _authLinkServiceMock = new Mock<IAuthLinkService>();
            _emailServiceMock = new Mock<IEmailService>();
            _redisServiceMock = new Mock<IRedisService>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<AuthService>>();
            _customerServiceMock = new Mock<ICustomerService>();

            _tenants = new List<ActiveTenant>
            {
                new ActiveTenant { Hostname = "test.restx.food" }
            };

            _tokenServiceMock.Setup(t => t.GenerateAccessToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>(), It.IsAny<string>()))
                .Returns("jwt-access-token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken())
                .Returns("new-refresh-token");
            _tokenServiceMock.Setup(t => t.GetAccessTokenExpiry())
                .Returns(DateTime.UtcNow.AddHours(1));
            _tokenServiceMock.Setup(t => t.GetRefreshTokenExpiry())
                .Returns(DateTime.UtcNow.AddDays(7));

            _mapperMock.Setup(m => m.Map<UserInfo>(It.IsAny<ApplicationUser>()))
                .Returns((ApplicationUser u) => new UserInfo
                {
                    Id = u.Id,
                    Email = u.Email ?? string.Empty,
                    FullName = u.FullName
                });

            _userManagerMock.Setup(u => u.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { "Staff" });
            _userManagerMock.Setup(u => u.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            _authService = new AuthService(
                _repoMock.Object,
                _userManagerMock.Object,
                _signInManagerMock.Object,
                _roleManagerMock.Object,
                _tokenServiceMock.Object,
                _authLinkServiceMock.Object,
                _emailServiceMock.Object,
                _redisServiceMock.Object,
                _mapperMock.Object,
                _loggerMock.Object,
                _customerServiceMock.Object,
                _tenants);
        }

        #region UTCID01 - Login with valid email and valid password

        [Test]
        public async Task LoginAsync_WithValidCredentials_ReturnsSuccessWithToken()
        {
            // Arrange
            var request = new LoginRequest { Email = "staff@restx.food", Password = "ValidPass123!" };
            _userManagerMock.Setup(u => u.FindByEmailAsync(request.Email))
                .ReturnsAsync(_validUser);
            _signInManagerMock.Setup(s => s.CheckPasswordSignInAsync(_validUser, request.Password, true))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.EqualTo("Login successful"));
            Assert.That(result.Data, Is.InstanceOf<LoginResponse>());
            var loginData = result.Data as LoginResponse;
            Assert.That(loginData!.AccessToken, Is.Not.Empty);
            Assert.That(loginData.RefreshToken, Is.Not.Empty);
        }

        #endregion

        #region UTCID02 - Login with valid email but invalid password

        [Test]
        public async Task LoginAsync_WithValidEmailInvalidPassword_ReturnsFailure()
        {
            // Arrange
            var request = new LoginRequest { Email = "staff@restx.food", Password = "WrongPassword!" };
            _userManagerMock.Setup(u => u.FindByEmailAsync(request.Email))
                .ReturnsAsync(_validUser);
            _signInManagerMock.Setup(s => s.CheckPasswordSignInAsync(_validUser, request.Password, true))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Invalid email or password"));
            Assert.That(result.Data, Is.Null);
        }

        #endregion

        #region UTCID03 - Login with invalid email (user not found)

        [Test]
        public async Task LoginAsync_WithNonExistentEmail_ReturnsFailure()
        {
            // Arrange
            var request = new LoginRequest { Email = "nonexistent@restx.food", Password = "ValidPass123!" };
            _userManagerMock.Setup(u => u.FindByEmailAsync(request.Email))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Invalid email or password"));
            Assert.That(result.Data, Is.Null);
            _signInManagerMock.Verify(
                s => s.CheckPasswordSignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<bool>()),
                Times.Never);
        }

        #endregion

        #region UTCID04 - Login with empty email

        [Test]
        public async Task LoginAsync_WithEmptyEmail_ReturnsFailure()
        {
            // Arrange
            var request = new LoginRequest { Email = "", Password = "ValidPass123!" };
            _userManagerMock.Setup(u => u.FindByEmailAsync(string.Empty))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Invalid email or password"));
        }

        #endregion

        #region UTCID05 - Login with empty password

        [Test]
        public async Task LoginAsync_WithEmptyPassword_ReturnsFailure()
        {
            // Arrange
            var request = new LoginRequest { Email = "staff@restx.food", Password = "" };
            _userManagerMock.Setup(u => u.FindByEmailAsync(request.Email))
                .ReturnsAsync(_validUser);
            _signInManagerMock.Setup(s => s.CheckPasswordSignInAsync(_validUser, string.Empty, true))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Invalid email or password"));
        }

        #endregion

        #region UTCID06 - Logout with valid user, token invalidated

        [Test]
        public async Task LogoutAsync_WithValidUser_ReturnsSuccessAndInvalidatesToken()
        {
            // Arrange
            var userId = _validUser.Id;
            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(_validUser);

            // Act
            var result = await _authService.LogoutAsync(userId);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.EqualTo("Logout successful"));
            _userManagerMock.Verify(u => u.UpdateAsync(It.Is<ApplicationUser>(
                user => user.RefreshToken == string.Empty && user.RefreshTokenExpiryTime == null
            )), Times.Once);
        }

        #endregion

        #region UTCID07 - Logout with non-existent user returns failure

        [Test]
        public async Task LogoutAsync_WithNonExistentUser_ReturnsFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString()))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _authService.LogoutAsync(userId);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("User not found"));
            _userManagerMock.Verify(u => u.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        #endregion

        #region UTCID08 - Customer phone login with valid registered phone

        [Test]
        public async Task CustomerPhoneLoginAsync_WithValidPhone_ReturnsSuccessWithToken()
        {
            // Arrange
            var request = new CustomerPhoneLoginRequest { PhoneNumber = "0901234567" };
            var customerId = Guid.NewGuid();
            var customer = new Customer
            {
                Id = customerId,
                ApplicationUserId = _validUser.Id,
                IsActive = true
            };

            var users = new List<ApplicationUser> { _validUser }.AsQueryable();
            _userManagerMock.Setup(u => u.Users).Returns(users);

            _repoMock.Setup(r => r.GetFirstAsync<Customer>(
                    It.IsAny<Expression<Func<Customer, bool>>>(), null, null))
                .ReturnsAsync(customer);

            _userManagerMock.Setup(u => u.GetRolesAsync(_validUser))
                .ReturnsAsync(new List<string> { "Customer" });

            // Act
            var result = await _authService.CustomerPhoneLoginAsync(request);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.EqualTo("Login successful"));
            Assert.That(result.Data, Is.InstanceOf<LoginResponse>());
            var loginData = result.Data as LoginResponse;
            Assert.That(loginData!.AccessToken, Is.Not.Empty);
        }

        #endregion
    }
}
