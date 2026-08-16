using HelpDesk.SDK.Categories;
using HelpDesk.SDK.Platforms;
using HelpDesk.Web.Common;
using HelpDesk.Web.Mapping;
using HelpDesk.Web.ViewModels.Category;

namespace HelpDesk.Web.Services;

// Nota: el service usa CategoryRequestDto solo internamente (vía form.ToRequestDto());
// el controller nunca lo ve. El controller solo maneja CategoryFormModel (tipo del Web).

/// <summary>
/// Capa de servicio de Categorías. Consume el SDK (Refit), centraliza try/catch + logging
/// vía <see cref="BaseService.ExecuteAsync{T}"/>, mapea DTO → ViewModel y devuelve
/// <see cref="ServiceResult{T}"/>. El controlador queda delgado; la vista solo pinta.
/// </summary>
public class CategoriesService : BaseService
{
    private readonly ICategoriesApi _categoriesApi;   // el SDK, NO HttpClient
    private readonly IPlatformsApi _platformsApi;     // solo lectura, para resolver PlatformName

    protected override string ServiceName => nameof(CategoriesService);

    public CategoriesService(
        ICategoriesApi categoriesApi,
        IPlatformsApi platformsApi,
        ILogger<CategoriesService> logger) : base(logger)
    {
        _categoriesApi = categoriesApi;
        _platformsApi = platformsApi;
    }

    public Task<ServiceResult<CategoryListViewModel>> GetAllCategories() =>
        ExecuteAsync(async () =>
        {
            // Cargamos plataformas SIEMPRE: el modal de crear/editar las necesita aunque
            // todavía no exista ninguna categoría. El estado "vacío" lo pinta la vista por
            // Model.Categories.Count == 0, no por el mensaje del ServiceResult.
            var categories = await _categoriesApi.GetAllAsync() ?? [];
            var platforms = await _platformsApi.GetAllAsync() ?? [];

            // CategoryDto solo trae PlatformId; el mapper resuelve el nombre con las plataformas.
            var vm = new CategoryListViewModel
            {
                Categories = categories.MapToViewModel(platforms),
                Platforms = platforms,
            };
            return ServiceResult<CategoryListViewModel>.Ok(vm);
        },
        apiErrorMessage: ErrorMessages.ApiCategoriesListError,
        webErrorMessage: ErrorMessages.WebCategoriesListError);

    public Task<ServiceResult<bool>> CreateCategory(CategoryFormModel form) =>
        ExecuteAsync(async () =>
        {
            await _categoriesApi.CreateAsync(form.ToRequestDto());   // el SDK vive aquí, no en el controller
            return ServiceResult<bool>.Ok(true);
        },
        apiErrorMessage: ErrorMessages.ApiCategoriesCreateError,
        webErrorMessage: ErrorMessages.WebCategoriesCreateError);

    public Task<ServiceResult<bool>> UpdateCategory(Guid id, CategoryFormModel form) =>
        ExecuteAsync(async () =>
        {
            await _categoriesApi.UpdateAsync(id, form.ToRequestDto());
            return ServiceResult<bool>.Ok(true);
        },
        apiErrorMessage: ErrorMessages.ApiCategoriesUpdateError,
        webErrorMessage: ErrorMessages.WebCategoriesUpdateError);

    public Task<ServiceResult<bool>> DeleteCategory(Guid id) =>
        ExecuteAsync(async () =>
        {
            await _categoriesApi.DeleteAsync(id);
            return ServiceResult<bool>.Ok(true);
        },
        apiErrorMessage: ErrorMessages.ApiCategoriesDeleteError,
        webErrorMessage: ErrorMessages.WebCategoriesDeleteError);
}
