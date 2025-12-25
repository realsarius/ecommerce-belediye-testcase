using EcommerceAPI.Core.Utilities.Results;
using Microsoft.AspNetCore.Mvc;

// IResult belirsizliğini önlemek için alias
using CoreIResult = EcommerceAPI.Core.Utilities.Results.IResult;

namespace EcommerceAPI.API.Controllers;

/// <summary>
/// Tüm API controller'ları için temel sınıf.
/// 
/// Bu sınıf, Result pattern ile REST standartlarını uyumlu hale getirir:
/// - Success = true -> HTTP 200 OK
/// - Success = false -> HTTP 400 Bad Request
/// - Data = null && Success = false -> HTTP 404 Not Found (opsiyonel)
/// 
/// Bu yaklaşım "Convention over Configuration" prensibini uygular.
/// </summary>
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// IDataResult tipindeki sonuçları uygun HTTP status code'larına çevirir.
    /// </summary>
    protected IActionResult HandleResult<T>(IDataResult<T> result)
    {
        if (!result.Success)
        {
            // Mesajda "bulunamadı" varsa 404, yoksa 400
            if (result.Message?.Contains("bulunamadı", StringComparison.OrdinalIgnoreCase) == true ||
                result.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            {
                return NotFound(new { success = false, message = result.Message });
            }
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// IResult tipindeki sonuçları uygun HTTP status code'larına çevirir.
    /// </summary>
    protected IActionResult HandleResult(CoreIResult result)
    {
        if (!result.Success)
        {
            // Mesajda "bulunamadı" varsa 404, yoksa 400
            if (result.Message?.Contains("bulunamadı", StringComparison.OrdinalIgnoreCase) == true ||
                result.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            {
                return NotFound(new { success = false, message = result.Message });
            }
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Yeni kayıt oluşturma işlemleri için 201 Created döner.
    /// </summary>
    protected IActionResult HandleCreatedResult<T>(IDataResult<T> result, string actionName, object routeValues)
    {
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return CreatedAtAction(actionName, routeValues, result);
    }

    /// <summary>
    /// Silme işlemleri için 204 No Content döner.
    /// </summary>
    protected IActionResult HandleDeleteResult(CoreIResult result)
    {
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return NoContent();
    }
}
