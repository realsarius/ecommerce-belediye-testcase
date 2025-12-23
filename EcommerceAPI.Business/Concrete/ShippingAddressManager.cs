using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.Business.Concrete;

public class ShippingAddressManager : IShippingAddressService
{
    private readonly IShippingAddressDal _shippingAddressDal;
    private readonly IUnitOfWork _unitOfWork;

    public ShippingAddressManager(IShippingAddressDal shippingAddressDal, IUnitOfWork unitOfWork)
    {
        _shippingAddressDal = shippingAddressDal;
        _unitOfWork = unitOfWork;
    }

    public async Task<IDataResult<List<ShippingAddressDto>>> GetUserAddressesAsync(int userId)
    {
        var addresses = await _shippingAddressDal.GetListAsync(a => a.UserId == userId);
        
        var addressDtos = addresses.Select(a => new ShippingAddressDto
        {
            Id = a.Id,
            Title = a.Title,
            FullName = a.FullName,
            Phone = a.Phone,
            City = a.City,
            District = a.District,
            AddressLine = a.AddressLine,
            PostalCode = a.PostalCode,
            IsDefault = a.IsDefault
        }).ToList();

        return new SuccessDataResult<List<ShippingAddressDto>>(addressDtos);
    }

    public async Task<IDataResult<ShippingAddressDto>> AddAddressAsync(int userId, CreateShippingAddressRequest request)
    {
        var address = new ShippingAddress
        {
            UserId = userId,
            Title = request.Title,
            FullName = request.FullName,
            Phone = request.Phone,
            City = request.City,
            District = request.District,
            AddressLine = request.AddressLine,
            PostalCode = request.PostalCode,
            IsDefault = request.IsDefault
        };

        if (address.IsDefault)
        {
            var existingDefault = await _shippingAddressDal.GetAsync(a => a.UserId == userId && a.IsDefault);
            if (existingDefault != null)
            {
                existingDefault.IsDefault = false;
                _shippingAddressDal.Update(existingDefault);
            }
        }

        await _shippingAddressDal.AddAsync(address);
        await _unitOfWork.SaveChangesAsync();

        var addressDto = new ShippingAddressDto
        {
            Id = address.Id,
            Title = address.Title,
            FullName = address.FullName,
            Phone = address.Phone,
            City = address.City,
            District = address.District,
            AddressLine = address.AddressLine,
            PostalCode = address.PostalCode,
            IsDefault = address.IsDefault
        };

        return new SuccessDataResult<ShippingAddressDto>(addressDto, "Adres başarıyla eklendi");
    }
}

