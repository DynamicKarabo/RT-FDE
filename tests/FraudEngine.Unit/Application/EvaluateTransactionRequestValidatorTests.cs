using FraudEngine.Application.Validation;
using FraudEngine.Contracts;

namespace FraudEngine.Unit.Application;

public class EvaluateTransactionRequestValidatorTests
{
    private readonly EvaluateTransactionRequestValidator _sut = new();

    [Fact]
    public void Validate_Succeeds_WhenAllFieldsAreValid()
    {
        var request = ValidRequest();

        var result = _sut.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Fails_WhenTransactionIdIsEmpty()
    {
        var request = ValidRequest() with { TransactionId = Guid.Empty };

        var result = _sut.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(EvaluateTransactionRequest.TransactionId));
    }

    [Fact]
    public void Validate_Fails_WhenUserIdIsEmpty()
    {
        var request = ValidRequest() with { UserId = Guid.Empty };

        var result = _sut.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(EvaluateTransactionRequest.UserId));
    }

    [Fact]
    public void Validate_Fails_WhenAmountIsZero()
    {
        var request = ValidRequest() with { Amount = 0 };

        var result = _sut.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(EvaluateTransactionRequest.Amount));
    }

    [Fact]
    public void Validate_Fails_WhenAmountIsNegative()
    {
        var request = ValidRequest() with { Amount = -100m };

        var result = _sut.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(EvaluateTransactionRequest.Amount));
    }

    [Fact]
    public void Validate_Fails_WhenCurrencyIsEmpty()
    {
        var request = ValidRequest() with { Currency = string.Empty };

        var result = _sut.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(EvaluateTransactionRequest.Currency));
    }

    [Fact]
    public void Validate_Fails_WhenCurrencyIsNotThreeCharacters()
    {
        var request = ValidRequest() with { Currency = "US" };

        var result = _sut.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(EvaluateTransactionRequest.Currency));
    }

    [Fact]
    public void Validate_Fails_WhenTimestampIsDefault()
    {
        var request = ValidRequest() with { Timestamp = default };

        var result = _sut.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(EvaluateTransactionRequest.Timestamp));
    }

    [Fact]
    public void Validate_Fails_WhenIpAddressIsEmpty()
    {
        var request = ValidRequest() with { IpAddress = string.Empty };

        var result = _sut.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(EvaluateTransactionRequest.IpAddress));
    }

    [Fact]
    public void Validate_Fails_WhenDeviceIdIsEmpty()
    {
        var request = ValidRequest() with { DeviceId = string.Empty };

        var result = _sut.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(EvaluateTransactionRequest.DeviceId));
    }

    [Fact]
    public void Validate_Fails_WhenMerchantIdIsEmpty()
    {
        var request = ValidRequest() with { MerchantId = string.Empty };

        var result = _sut.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(EvaluateTransactionRequest.MerchantId));
    }

    private static EvaluateTransactionRequest ValidRequest() => new(
        TransactionId: Guid.NewGuid(),
        UserId: Guid.NewGuid(),
        Amount: 2500m,
        Currency: "ZAR",
        Timestamp: DateTimeOffset.UtcNow,
        IpAddress: "192.168.1.1",
        DeviceId: "device-1",
        MerchantId: "merchant-1");
}
