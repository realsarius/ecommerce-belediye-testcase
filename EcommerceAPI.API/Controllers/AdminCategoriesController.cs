using EcommerceAPI.Business.Services.Abstract;
using EcommerceAPI.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/admin/categories")]
[Authorize(Roles = "Admin")]
public class AdminCategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public AdminCategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAllCategories()
    {
        var categories = await _categoryService.GetAllCategoriesAsync(includeInactive: true);
        return Ok(categories);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        try
        {
            var category = await _categoryService.CreateCategoryAsync(request);
            return CreatedAtRoute(
                routeName: "GetCategoryById",
                routeValues: new { id = category.Id },
                value: category);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request)
    {
        try
        {
            var category = await _categoryService.UpdateCategoryAsync(id, request);
            
            if (category == null)
                return NotFound(new { message = "Kategori bulunamadı" });
            
            return Ok(category);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        try
        {
            var result = await _categoryService.DeleteCategoryAsync(id);
            
            if (!result)
                return NotFound(new { message = "Kategori bulunamadı" });
            
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
