using Xunit;
using FluentAssertions;
using APISinout.Validators;
using APISinout.Models;
using APISinout.Tests.Fixtures;

namespace APISinout.Tests.Unit.Validators;

/// <summary>
/// Testes completos para RegisterRequestValidator
/// </summary>
public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator;

    public RegisterRequestValidatorTests()
    {
        _validator = new RegisterRequestValidator();
    }

    #region Name Validation

    [Fact]
    public async Task Validate_WithValidName_ShouldPass()
    {
        // Arrange
        var request = UserFixtures.CreateValidRegisterRequest();

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyName_ShouldFail()
    {
        // Arrange
        var request = UserFixtures.CreateValidRegisterRequest();
        request.Name = "";

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WithNameTooShort_ShouldFail()
    {
        // Arrange
        var request = UserFixtures.CreateValidRegisterRequest();
        request.Name = "Jo";

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData("João Silva")]
    [InlineData("Maria José")]
    [InlineData("José da Silva")]
    public async Task Validate_WithValidNameVariations_ShouldPass(string name)
    {
        // Arrange
        var request = UserFixtures.CreateValidRegisterRequest();
        request.Name = name;

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Email Validation

    [Fact]
    public async Task Validate_WithEmptyEmail_ShouldFail()
    {
        // Arrange
        var request = UserFixtures.CreateValidRegisterRequest();
        request.Email = "";

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("invalid@")]
    [InlineData("@invalid.com")]
    public async Task Validate_WithInvalidEmail_ShouldFail(string email)
    {
        // Arrange
        var request = UserFixtures.CreateValidRegisterRequest();
        request.Email = email;

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("user.name@example.com")]
    [InlineData("user+tag@example.co.uk")]
    public async Task Validate_WithValidEmail_ShouldPass(string email)
    {
        // Arrange
        var request = UserFixtures.CreateValidRegisterRequest();
        request.Email = email;

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Password Validation

    [Fact]
    public async Task Validate_WithEmptyPassword_ShouldFail()
    {
        // Arrange
        var request = UserFixtures.CreateValidRegisterRequest();
        request.Password = "";

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Validate_WithPasswordTooShort_ShouldFail()
    {
        // Arrange
        var request = UserFixtures.CreateValidRegisterRequest();
        request.Password = "Test@1";

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Theory]
    [InlineData("Test@123")]
    [InlineData("MyPassword1")]
    [InlineData("Secure#Pass99")]
    public async Task Validate_WithStrongPassword_ShouldPass(string password)
    {
        // Arrange
        var request = UserFixtures.CreateValidRegisterRequest();
        request.Password = password;

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Phone Validation

    [Theory]
    [InlineData("+55 11 99999-9999")]
    [InlineData("(11) 99999-9999")]
    [InlineData("11999999999")]
    public async Task Validate_WithValidPhone_ShouldPass(string phone)
    {
        // Arrange
        var request = UserFixtures.CreateValidRegisterRequest();
        request.Phone = phone;

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithNullPhone_ShouldPass()
    {
        // Arrange
        var request = UserFixtures.CreateValidRegisterRequest();
        request.Phone = null;

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion
}
