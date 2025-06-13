using Api.Utils;
using Microsoft.Extensions.Logging;
using Api.Services;
using AutoFixture;
using AutoMapper;
using Domain.Abstractions.Repositories;
using Domain.Abstractions.Services;
using Domain.Entities;
using Domain.Models.Dtos.Booking;
using Domain.Models.Dtos.CompensationRequest;
using Domain.Models.Dtos.Image;
using Domain.Models.Dtos.Property;
using Domain.Models.Enums;
using Domain.Models.Payment;
using Domain.Models.Result;
using Infrastructure.PaymentSystem.Options;
using Microsoft.Extensions.Options;
using Moq;

namespace ServicesTests;

public class PropertyServiceTests
{
    private readonly Fixture _fixture;
    private readonly Mock<IPropertyRepository> _propertyRepoMock;
    private readonly Mock<IFileStorageService> _fileStorageMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly PropertyService _service;

    public PropertyServiceTests()
    {
        _fixture = new Fixture();
        _propertyRepoMock = new Mock<IPropertyRepository>();
        _fileStorageMock = new Mock<IFileStorageService>();
        _mapperMock = new Mock<IMapper>();
        
        _service = new PropertyService(
            _propertyRepoMock.Object,
            _fileStorageMock.Object,
            new FileWorker(),
            _mapperMock.Object,
            Mock.Of<ILogger<PropertyService>>());
    }

    [Fact]
    public async Task CreatePropertyAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        _fixture.Register<Stream>(() => new MemoryStream());
        var request =  _fixture.Build<PropertyCreateRequest>()
            .Create();
        var userId = _fixture.Create<int>();
        var property = _fixture.Create<Property>();
        
        _mapperMock.Setup(m => m.Map<Property>(request)).Returns(property);
        _fileStorageMock.Setup(f => f.UploadFileAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), new CancellationToken()))
            .ReturnsAsync(Result.Success(SuccessType.NoContent));
        _propertyRepoMock.Setup(r => r.AddAsync(property)).ReturnsAsync(property);
        _propertyRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreatePropertyAsync(request, userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(property.Id, result.Value);
        _propertyRepoMock.Verify(r => r.AddAsync(property), Times.Once);
        _propertyRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SearchPropertiesAsync_NoProperties_ReturnsNoContent()
    {
        // Arrange
        var request = _fixture.Create<PropertySearchRequest>();
        var userId = _fixture.Create<int?>();
        
        _propertyRepoMock.Setup(r => r.AnyLocationByCityAsync(request.City)).ReturnsAsync(true);
        _propertyRepoMock.Setup(r => r.SearchPropertiesAsync(
            request.City,
            request.HasPets,
            request.Adults + request.Children,
            request.StartDate.ToUniversalTime(),
            request.EndDate.ToUniversalTime()))
            .ReturnsAsync(new List<Property>());

        // Act
        var result = await _service.SearchPropertiesAsync(request, userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(SuccessType.NoContent, result.SuccessType);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetMainImageAsync_ValidImage_ReturnsImage()
    {
        // Arrange
        var imageId = _fixture.Create<int>();
        var imageInfo = _fixture.Build<PropertyImage>()
            .With(i => i.IsMain, true)
            .Create();
        var base64Content = _fixture.Create<string>();
        
        _propertyRepoMock.Setup(r => r.GetImageInfoAsync(imageId)).ReturnsAsync(imageInfo);
        _fileStorageMock.Setup(f => f.DownloadFileAsBase64Async(imageInfo.FileName, new CancellationToken()))
            .ReturnsAsync(Result<string>.Success(SuccessType.Ok, base64Content));

        // Act
        var result = await _service.GetMainImageAsync(imageId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(base64Content, result.Value.ContentBase64);
        Assert.True(result.Value.IsMain);
    }
}



public class CompensationRequestServiceTests
{
    private readonly Fixture _fixture;
    private readonly Mock<ICompensationRequestRepository> _compensationRepoMock;
    private readonly Mock<IFileStorageService> _fileStorageMock;
    private readonly CompensationRequestService _service;

    public CompensationRequestServiceTests()
    {
        _fixture = new Fixture();
        _compensationRepoMock = new Mock<ICompensationRequestRepository>();
        _fileStorageMock = new Mock<IFileStorageService>();
        
        _service = new CompensationRequestService(
            _compensationRepoMock.Object,
            Mock.Of<IMapper>(),
            _fileStorageMock.Object,
            Mock.Of<ILogger<CompensationRequestService>>());
    }

    [Fact]
    public async Task CreateCompensationRequest_ValidRequest_CreatesRequest()
    {
        // Arrange
        var request = _fixture.Create<CompensationRequestDto>();
        var userId = _fixture.Create<int>();
        var compensationRequest = _fixture.Create<CompensationRequest>();
        
        _fileStorageMock.Setup(f => f.UploadFileAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), new CancellationToken()))
            .ReturnsAsync(Result.Success(SuccessType.NoContent));
        _compensationRepoMock.Setup(r => r.AddAsync(It.IsAny<CompensationRequest>()))
            .ReturnsAsync(compensationRequest);
        _compensationRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateCompensationRequest(request, userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(compensationRequest.Id, result.Value);
        _compensationRepoMock.Verify(r => r.AddAsync(It.IsAny<CompensationRequest>()), Times.Once);
    }

    [Fact]
    public async Task ApproveCompensationRequest_ValidRequest_ApprovesRequest()
    {
        // Arrange
        var requestId = _fixture.Create<int>();
        var userId = _fixture.Create<int>();
        var amount = _fixture.Create<decimal>();
        var booking = _fixture.Build<Booking>()
            .With(b => b.TotalPrice, amount + 100)
            .Create();
        var compensationRequest = _fixture.Build<CompensationRequest>()
            .With(c => c.Booking, booking)
            .With(c => c.Tenant, _fixture.Create<User>())
            .Create();
        
        _compensationRepoMock.Setup(r => r.GetWithBookingPropertyAndTenantAsync(requestId))
            .ReturnsAsync(compensationRequest);
        _compensationRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.ApproveCompensationRequest(userId, requestId, amount);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(CompensationStatus.Approved, compensationRequest.Status);
        Assert.Equal(amount, compensationRequest.Tenant.Balance);
    }

    [Fact]
    public async Task GetCompensationRequestById_ExistingRequest_ReturnsRequest()
    {
        // Arrange
        var requestId = _fixture.Create<int>();
        var compensationRequest = _fixture.Create<CompensationRequest>();
        var response = _fixture.Create<CompensationRequestResponse>();
        
        _compensationRepoMock.Setup(r => r.GetByFilterAsync(r => r.Id == requestId))
            .ReturnsAsync(compensationRequest);

        // Act
        var result = await _service.GetCompensationRequestById(requestId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(response.Id, result.Value.Id); // Assuming mapper is properly set up
    }
}