using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Core.Aspects.Autofac.Logging;
using EcommerceAPI.Core.Aspects.Autofac.Caching;
using EcommerceAPI.Business.Constants;

namespace EcommerceAPI.Business.Concrete;

public class CreditCardManager : ICreditCardService
{
    private readonly ICreditCardDal _creditCardDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEncryptionService _encryptionService;

    public CreditCardManager(
        ICreditCardDal creditCardDal, 
        IUnitOfWork unitOfWork,
        IEncryptionService encryptionService)
    {
        _creditCardDal = creditCardDal;
        _unitOfWork = unitOfWork;
        _encryptionService = encryptionService;
    }

    [LogAspect]
    [CacheAspect(duration: 30)]
    public async Task<IDataResult<List<CreditCardDto>>> GetUserCardsAsync(int userId)
    {
        var cards = await _creditCardDal.GetListAsync(c => c.UserId == userId);
        
        var cardDtos = cards.Select(c => new CreditCardDto
        {
            Id = c.Id,
            CardAlias = c.CardAlias,
            CardHolderName = c.CardHolderName,
            Last4Digits = c.Last4Digits,
            ExpireMonth = _encryptionService.Decrypt(c.ExpireMonthEncrypted),
            ExpireYear = _encryptionService.Decrypt(c.ExpireYearEncrypted),
            IsDefault = c.IsDefault
        }).ToList();

        return new SuccessDataResult<List<CreditCardDto>>(cardDtos);
    }

    // Logging omitted for AddCardAsync due to sensitive data (CardNumber, CVV) in request.
    [CacheRemoveAspect("GetUserCardsAsync")]
    public async Task<IDataResult<CreditCardDto>> AddCardAsync(int userId, AddCreditCardRequest request)
    {
        var existingCards = await _creditCardDal.GetListAsync(c => c.UserId == userId);
        bool isFirstCard = !existingCards.Any();
        bool shouldBeDefault = isFirstCard || request.IsDefault;
        
        if (shouldBeDefault && existingCards.Any())
        {
            foreach (var existingCard in existingCards.Where(c => c.IsDefault))
            {
                existingCard.IsDefault = false;
                _creditCardDal.Update(existingCard);
            }
        }

        string last4Digits = request.CardNumber.Length >= 4 
            ? request.CardNumber.Substring(request.CardNumber.Length - 4)
            : request.CardNumber;

        var creditCard = new CreditCard
        {
            UserId = userId,
            CardAlias = request.CardAlias,
            CardHolderName = request.CardHolderName,
            CardNumberEncrypted = _encryptionService.Encrypt(request.CardNumber),
            Last4Digits = last4Digits,
            ExpireYearEncrypted = _encryptionService.Encrypt(request.ExpireYear),
            ExpireMonthEncrypted = _encryptionService.Encrypt(request.ExpireMonth),
            CvvEncrypted = _encryptionService.Encrypt(request.Cvv),
            IsDefault = shouldBeDefault
        };

        await _creditCardDal.AddAsync(creditCard);
        await _unitOfWork.SaveChangesAsync();

        var cardDto = new CreditCardDto
        {
            Id = creditCard.Id,
            CardAlias = creditCard.CardAlias,
            CardHolderName = creditCard.CardHolderName,
            Last4Digits = creditCard.Last4Digits,
            ExpireMonth = request.ExpireMonth,
            ExpireYear = request.ExpireYear,
            IsDefault = creditCard.IsDefault
        };

        return new SuccessDataResult<CreditCardDto>(cardDto, Messages.CardAdded);
    }

    [LogAspect]
    [CacheRemoveAspect("GetUserCardsAsync")]
    public async Task<IResult> DeleteCardAsync(int userId, int cardId)
    {
        var card = await _creditCardDal.GetAsync(c => c.Id == cardId && c.UserId == userId);
        
        if (card == null)
        {
            return new ErrorResult(Messages.CardNotFound);
        }

        bool wasDefault = card.IsDefault;
        _creditCardDal.Delete(card);
        await _unitOfWork.SaveChangesAsync();

        if (wasDefault)
        {
            var remainingCards = await _creditCardDal.GetListAsync(c => c.UserId == userId);
            var firstCard = remainingCards.FirstOrDefault();
            if (firstCard != null)
            {
                firstCard.IsDefault = true;
                _creditCardDal.Update(firstCard);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        return new SuccessResult(Messages.CardDeleted);
    }

    [LogAspect]
    [CacheRemoveAspect("GetUserCardsAsync")]
    public async Task<IResult> SetDefaultCardAsync(int userId, int cardId)
    {
        var card = await _creditCardDal.GetAsync(c => c.Id == cardId && c.UserId == userId);
        
        if (card == null)
        {
            return new ErrorResult(Messages.CardNotFound);
        }

        var allCards = await _creditCardDal.GetListAsync(c => c.UserId == userId);
        foreach (var existingCard in allCards.Where(c => c.IsDefault))
        {
            existingCard.IsDefault = false;
            _creditCardDal.Update(existingCard);
        }

        card.IsDefault = true;
        _creditCardDal.Update(card);
        await _unitOfWork.SaveChangesAsync();

        return new SuccessResult(Messages.DefaultCardSet);
    }

    [LogAspect]
    public async Task<IDataResult<DecryptedCardDto>> GetDecryptedCardForPaymentAsync(int userId, int cardId)
    {
        var card = await _creditCardDal.GetAsync(c => c.Id == cardId && c.UserId == userId);
        
        if (card == null)
        {
            return new ErrorDataResult<DecryptedCardDto>(Messages.CardNotFound);
        }

        var decryptedCard = new DecryptedCardDto
        {
            Id = card.Id,
            CardHolderName = card.CardHolderName,
            CardNumber = _encryptionService.Decrypt(card.CardNumberEncrypted),
            ExpireMonth = _encryptionService.Decrypt(card.ExpireMonthEncrypted),
            ExpireYear = _encryptionService.Decrypt(card.ExpireYearEncrypted)
        };

        return new SuccessDataResult<DecryptedCardDto>(decryptedCard);
    }
}
