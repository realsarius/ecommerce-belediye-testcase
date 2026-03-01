using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace EcommerceAPI.UnitTests;

public class CampaignManagerTests
{
    private readonly Mock<ICampaignDal> _campaignDalMock;
    private readonly Mock<IProductDal> _productDalMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly CampaignManager _manager;

    public CampaignManagerTests()
    {
        _campaignDalMock = new Mock<ICampaignDal>();
        _productDalMock = new Mock<IProductDal>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _auditServiceMock = new Mock<IAuditService>();

        _manager = new CampaignManager(
            _campaignDalMock.Object,
            _productDalMock.Object,
            _unitOfWorkMock.Object,
            _auditServiceMock.Object,
            Mock.Of<ILogger<CampaignManager>>());
    }

    [Fact]
    public async Task CreateAsync_WithValidProducts_ShouldCreateScheduledCampaign()
    {
        var startsAt = DateTime.UtcNow.AddHours(2);
        var endsAt = DateTime.UtcNow.AddDays(1);

        _productDalMock
            .Setup(x => x.GetByIdsWithInventoryAsync(It.IsAny<List<int>>()))
            .ReturnsAsync([
                new Product
                {
                    Id = 90,
                    Name = "Kulaklık",
                    Description = "d",
                    Price = 1200,
                    SKU = "SKU-90",
                    CategoryId = 1,
                    IsActive = true
                }
            ]);

        _campaignDalMock
            .Setup(x => x.AddAsync(It.IsAny<Campaign>()))
            .Callback<Campaign>(campaign => campaign.Id = 501)
            .ReturnsAsync((Campaign campaign) => campaign);

        var result = await _manager.CreateAsync(new CreateCampaignRequest
        {
            Name = "Hafta Sonu Fırsatı",
            StartsAt = startsAt,
            EndsAt = endsAt,
            Products =
            [
                new CreateCampaignProductRequest
                {
                    ProductId = 90,
                    CampaignPrice = 999,
                    IsFeatured = true
                }
            ]
        });

        result.Success.Should().BeTrue();
        result.Data.Status.Should().Be(CampaignStatus.Scheduled);
        result.Data.Products.Should().ContainSingle(x => x.ProductId == 90 && x.CampaignPrice == 999);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ProcessCampaignLifecycleAsync_ShouldUpdateChangedStatuses()
    {
        var campaigns = new List<Campaign>
        {
            new()
            {
                Id = 1,
                Name = "Scheduled to Active",
                IsEnabled = true,
                StartsAt = DateTime.UtcNow.AddHours(-1),
                EndsAt = DateTime.UtcNow.AddHours(4),
                Status = CampaignStatus.Scheduled
            },
            new()
            {
                Id = 2,
                Name = "Active to Ended",
                IsEnabled = true,
                StartsAt = DateTime.UtcNow.AddDays(-2),
                EndsAt = DateTime.UtcNow.AddHours(-2),
                Status = CampaignStatus.Active
            }
        };

        _campaignDalMock
            .Setup(x => x.GetListAsync(null))
            .ReturnsAsync(campaigns);

        var result = await _manager.ProcessCampaignLifecycleAsync();

        result.Success.Should().BeTrue();
        campaigns[0].Status.Should().Be(CampaignStatus.Active);
        campaigns[1].Status.Should().Be(CampaignStatus.Ended);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenProductsOverlapWithExistingCampaign_ShouldReturnError()
    {
        _campaignDalMock
            .Setup(x => x.HasOverlappingProductCampaignsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync(true);

        var result = await _manager.CreateAsync(new CreateCampaignRequest
        {
            Name = "Flash Sale",
            StartsAt = DateTime.UtcNow.AddHours(1),
            EndsAt = DateTime.UtcNow.AddHours(5),
            Products =
            [
                new CreateCampaignProductRequest
                {
                    ProductId = 90,
                    CampaignPrice = 999,
                    IsFeatured = true
                }
            ]
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("çakışan");
        _campaignDalMock.Verify(x => x.AddAsync(It.IsAny<Campaign>()), Times.Never);
    }
}
