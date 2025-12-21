using EcommerceAPI.Business.Services.Abstract;
using EcommerceAPI.Core.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
    {
        var categories = await _categoryService.GetAllCategoriesAsync(includeInactive: false);
        return Ok(categories);
    }

    [HttpGet("{id}", Name = "GetCategoryById")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> GetCategory(int id)
    {
        var category = await _categoryService.GetCategoryByIdAsync(id);
        
        if (category == null)
            return NotFound(new { message = "Kategori bulunamadı" });
        
        return Ok(category);
    }
}
