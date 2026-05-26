using EF.Common.Contracts;
using TaskFlow.Application.Cqrs.Registration;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Cqrs.Features.Categories;

internal static class CategoryCqrsRegistrations
{
    public static IReadOnlyList<CqrsHandlerRegistration> Registrations { get; } =
    [
        new(typeof(SearchCategoriesQuery), typeof(PagedResponse<CategoryDto>), typeof(SearchCategoriesHandler)),
        new(typeof(GetCategoryByIdQuery), typeof(Result<DefaultResponse<CategoryDto>>), typeof(GetCategoryByIdHandler)),
        new(typeof(CreateCategoryCommand), typeof(Result<DefaultResponse<CategoryDto>>), typeof(CreateCategoryHandler)),
        new(typeof(UpdateCategoryCommand), typeof(Result<DefaultResponse<CategoryDto>>), typeof(UpdateCategoryHandler)),
        new(typeof(DeleteCategoryCommand), typeof(Result), typeof(DeleteCategoryHandler)),
    ];
}
