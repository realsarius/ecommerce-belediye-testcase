using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Core.Aspects.Autofac.Logging;
using EcommerceAPI.Core.Aspects.Autofac.Caching;
using EcommerceAPI.Core.Aspects.Autofac.Validation;
using EcommerceAPI.Business.Constants;
using EcommerceAPI.Business.Validators;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Entities.Utilities;

namespace EcommerceAPI.Business.Concrete;

public class CreditCardManager : ICreditCardService
{
    private readonly ICreditCardDal _creditCardDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEncryptionService _encryptionService;
    private readonly IPaymentFeaturePolicy _paymentFeaturePolicy;

    public CreditCardManager(
        ICreditCardDal creditCardDal, 
        IUnitOfWork unitOfWork,
        IEncryptionService encryptionService,
        IPaymentFeaturePolicy paymentFeaturePolicy)
    {
        _creditCardDal = creditCardDal;
        _unitOfWork = unitOfWork;
        _encryptionService = encryptionService;
        _paymentFeaturePolicy = paymentFeaturePolicy;
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
            Brand = c.Brand,
            Last4Digits = c.Last4Digits,
            ExpireMonth = _encryptionService.Decrypt(c.ExpireMonthEncrypted),
            ExpireYear = _encryptionService.Decrypt(c.ExpireYearEncrypted),
            IsTokenized = !string.IsNullOrWhiteSpace(c.IyzicoCardToken) ||
                          !string.IsNullOrWhiteSpace(c.StripePaymentMethodId) ||
                          !string.IsNullOrWhiteSpace(c.PayTrToken),
            TokenProvider = c.TokenProvider,
            IsDefault = c.IsDefault
        }).ToList();

        return new SuccessDataResult<List<CreditCardDto>>(cardDtos);
    }

    // Logging omitted for AddCardAsync due to sensitive data (card data) in request.
    [CacheRemoveAspect("GetUserCardsAsync")]
    [ValidationAspect(typeof(AddCreditCardRequestValidator))]
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
            Brand = CardBrandDetector.Detect(request.CardNumber),
            CardNumberEncrypted = _encryptionService.Encrypt(request.CardNumber),
            Last4Digits = last4Digits,
            ExpireYearEncrypted = _encryptionService.Encrypt(request.ExpireYear),
            ExpireMonthEncrypted = _encryptionService.Encrypt(request.ExpireMonth),
            IsDefault = shouldBeDefault
        };

        await _creditCardDal.AddAsync(creditCard);
        await _unitOfWork.SaveChangesAsync();

        var cardDto = new CreditCardDto
        {
            Id = creditCard.Id,
            CardAlias = creditCard.CardAlias,
            CardHolderName = creditCard.CardHolderName,
            Brand = creditCard.Brand,
            Last4Digits = creditCard.Last4Digits,
            ExpireMonth = request.ExpireMonth,
            ExpireYear = request.ExpireYear,
            IsTokenized = false,
            TokenProvider = null,
            IsDefault = creditCard.IsDefault
        };

        return new SuccessDataResult<CreditCardDto>(cardDto, Messages.CardAdded);
    }

    [CacheRemoveAspect("GetUserCardsAsync")]
    public async Task<IDataResult<CreditCardDto>> SaveTokenizedCardAsync(int userId, SaveTokenizedCreditCardRequest request)
    {
        if (request.TokenProvider != PaymentProviderType.Iyzico ||
            string.IsNullOrWhiteSpace(request.IyzicoCardToken) ||
            string.IsNullOrWhiteSpace(request.IyzicoUserKey))
        {
            return new ErrorDataResult<CreditCardDto>("Tokenized kart bilgisi gecersiz.");
        }

        var existingCards = await _creditCardDal.GetListAsync(c => c.UserId == userId);
        var existingCard = existingCards.FirstOrDefault(card =>
            card.TokenProvider == PaymentProviderType.Iyzico &&
            card.IyzicoCardToken == request.IyzicoCardToken);

        var shouldBeDefault = request.IsDefault || !existingCards.Any();

        if (shouldBeDefault)
        {
            foreach (var existingDefaultCard in existingCards.Where(c => c.IsDefault && c.Id != existingCard?.Id))
            {
                existingDefaultCard.IsDefault = false;
                _creditCardDal.Update(existingDefaultCard);
            }
        }

        var encryptedPlaceholder = _encryptionService.Encrypt($"tokenized:{request.TokenProvider}:{request.Last4Digits}");

        if (existingCard == null)
        {
            existingCard = new CreditCard
            {
                UserId = userId,
                CardAlias = string.IsNullOrWhiteSpace(request.CardAlias) ? "Kayitli Kartim" : request.CardAlias.Trim(),
                CardHolderName = request.CardHolderName.Trim(),
                Brand = request.Brand,
                CardNumberEncrypted = encryptedPlaceholder,
                Last4Digits = request.Last4Digits,
                ExpireYearEncrypted = _encryptionService.Encrypt(request.ExpireYear),
                ExpireMonthEncrypted = _encryptionService.Encrypt(request.ExpireMonth),
                IyzicoCardToken = request.IyzicoCardToken,
                IyzicoUserKey = request.IyzicoUserKey,
                TokenProvider = request.TokenProvider,
                IsDefault = shouldBeDefault
            };

            await _creditCardDal.AddAsync(existingCard);
        }
        else
        {
            existingCard.CardAlias = string.IsNullOrWhiteSpace(request.CardAlias) ? existingCard.CardAlias : request.CardAlias.Trim();
            existingCard.CardHolderName = request.CardHolderName.Trim();
            existingCard.Brand = request.Brand;
            existingCard.Last4Digits = request.Last4Digits;
            existingCard.ExpireYearEncrypted = _encryptionService.Encrypt(request.ExpireYear);
            existingCard.ExpireMonthEncrypted = _encryptionService.Encrypt(request.ExpireMonth);
            existingCard.IyzicoCardToken = request.IyzicoCardToken;
            existingCard.IyzicoUserKey = request.IyzicoUserKey;
            existingCard.TokenProvider = request.TokenProvider;
            existingCard.CardNumberEncrypted = encryptedPlaceholder;
            existingCard.IsDefault = shouldBeDefault;

            _creditCardDal.Update(existingCard);
        }

        await _unitOfWork.SaveChangesAsync();

        return new SuccessDataResult<CreditCardDto>(new CreditCardDto
        {
            Id = existingCard.Id,
            CardAlias = existingCard.CardAlias,
            CardHolderName = existingCard.CardHolderName,
            Brand = existingCard.Brand,
            Last4Digits = existingCard.Last4Digits,
            ExpireMonth = request.ExpireMonth,
            ExpireYear = request.ExpireYear,
            IsTokenized = true,
            TokenProvider = request.TokenProvider,
            IsDefault = existingCard.IsDefault
        });
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
    public async Task<IDataResult<StoredCardPaymentDto>> GetStoredCardForPaymentAsync(int userId, int cardId)
    {
        var card = await _creditCardDal.GetAsync(c => c.Id == cardId && c.UserId == userId);
        
        if (card == null)
        {
            return new ErrorDataResult<StoredCardPaymentDto>(Messages.CardNotFound);
        }

        var isTokenized = card.TokenProvider.HasValue &&
                          (!string.IsNullOrWhiteSpace(card.IyzicoCardToken) ||
                           !string.IsNullOrWhiteSpace(card.StripePaymentMethodId) ||
                           !string.IsNullOrWhiteSpace(card.PayTrToken));

        if (!isTokenized)
        {
            return new ErrorDataResult<StoredCardPaymentDto>(
                "Bu kayitli kart eski sifreli formatta. Guvenlik nedeniyle yeniden kart bilgisi girmeniz gerekiyor.");
        }

        var storedCard = new StoredCardPaymentDto
        {
            Id = card.Id,
            CardHolderName = card.CardHolderName,
            Brand = card.Brand,
            Last4Digits = card.Last4Digits,
            ExpireMonth = _encryptionService.Decrypt(card.ExpireMonthEncrypted),
            ExpireYear = _encryptionService.Decrypt(card.ExpireYearEncrypted),
            IsTokenized = isTokenized,
            TokenProvider = card.TokenProvider,
            IyzicoCardToken = card.IyzicoCardToken,
            IyzicoUserKey = card.IyzicoUserKey,
            CardNumber = null
        };

        return new SuccessDataResult<StoredCardPaymentDto>(storedCard);
    }
}
