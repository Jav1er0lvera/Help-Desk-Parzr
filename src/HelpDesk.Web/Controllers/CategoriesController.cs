using HelpDesk.Web.Services;
using HelpDesk.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Web.Controllers;

[Authorize]
public class CategoriesController : Controller
{
    private readonly CategoriesService _categoriesService;

    public CategoriesController(CategoriesService categoriesService)
    {
        _categoriesService = categoriesService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _categoriesService.GetAllCategories();

        if (!result.Success && !result.Warning)
            ViewBag.Error = result.Message;

        // Warn (lista vacía) o Fail: pasamos un VM vacío; la vista muestra "Aún no hay categorías."
        return View(result.Data ?? new CategoryListViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CategoryFormModel form)
    {
        var result = await _categoriesService.CreateCategory(form);
        return result.Success ? Ok() : StatusCode(500, result.Message);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, [FromBody] CategoryFormModel form)
    {
        var result = await _categoriesService.UpdateCategory(id, form);
        return result.Success ? Ok() : StatusCode(500, result.Message);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _categoriesService.DeleteCategory(id);
        return result.Success ? Ok() : StatusCode(500, result.Message);
    }
}
