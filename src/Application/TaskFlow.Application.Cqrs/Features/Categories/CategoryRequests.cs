using EF.Common.Contracts;
using EF.CQRS.Abstractions;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Cqrs.Features.Categories;

public sealed record SearchCategoriesQuery(SearchRequest<CategorySearchFilter> Request)
    : IQuery<PagedResponse<CategoryDto>>;

public sealed record GetCategoryByIdQuery(Guid Id)
    : IQuery<Result<DefaultResponse<CategoryDto>>>;

public sealed record CreateCategoryCommand(DefaultRequest<CategoryDto> Request)
    : ICommand<Result<DefaultResponse<CategoryDto>>>;

public sealed record UpdateCategoryCommand(DefaultRequest<CategoryDto> Request)
    : ICommand<Result<DefaultResponse<CategoryDto>>>;

public sealed record DeleteCategoryCommand(Guid Id)
    : ICommand<Result>;
