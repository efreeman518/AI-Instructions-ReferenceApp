using EF.Data;
using EF.Data.Contracts;
using EF.Domain;
using EF.Domain.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories.Updaters;

internal static class TaskItemUpdater
{
    /// <summary>
    /// Updates an existing entity with values from a DTO, including related child collections.
    /// </summary>
    public static DomainResult<TaskItem> UpdateFromDto(this TaskFlowDbContextTrxn db, TaskItem entity, TaskItemDto dto,
        RelatedDeleteBehavior relatedDeleteBehavior = RelatedDeleteBehavior.None)
    {
        return entity.Update(
            title: dto.Title,
            description: dto.Description,
            priority: dto.Priority,
            features: dto.Features,
            estimatedEffort: dto.EstimatedEffort,
            actualEffort: dto.ActualEffort,
            categoryId: dto.CategoryId)
        .Bind(updatedEntity => DomainResult.Combine(
            CollectionUtility.SyncCollectionWithResult<Comment, CommentDto, Guid>(
                updatedEntity.Comments,
                dto.Comments ?? [],
                e => e.Id,
                i => i.Id,
                incomingDto =>
                {
                    var result = Comment.Create(updatedEntity.TenantId, updatedEntity.Id, incomingDto.Body);
                    if (result.IsSuccess) updatedEntity.Comments.Add(result.Value!);
                    return result;
                },
                (existing, incomingDto) => existing.Update(incomingDto.Body),
                toRemove =>
                {
                    if (relatedDeleteBehavior == RelatedDeleteBehavior.None) return DomainResult.Success();
                    db.Delete(toRemove);
                    updatedEntity.Comments.Remove(toRemove);
                    return DomainResult.Success();
                }
            ),
            CollectionUtility.SyncCollectionWithResult<ChecklistItem, ChecklistItemDto, Guid>(
                updatedEntity.ChecklistItems,
                dto.ChecklistItems ?? [],
                e => e.Id,
                i => i.Id,
                incomingDto =>
                {
                    var result = ChecklistItem.Create(updatedEntity.TenantId, updatedEntity.Id, incomingDto.Title, incomingDto.SortOrder);
                    if (result.IsSuccess) updatedEntity.ChecklistItems.Add(result.Value!);
                    return result;
                },
                (existing, incomingDto) => existing.Update(incomingDto.Title, incomingDto.IsCompleted, incomingDto.SortOrder),
                toRemove =>
                {
                    if (relatedDeleteBehavior == RelatedDeleteBehavior.None) return DomainResult.Success();
                    db.Delete(toRemove);
                    updatedEntity.ChecklistItems.Remove(toRemove);
                    return DomainResult.Success();
                }
            ),
            CollectionUtility.SyncCollectionWithResult<TaskItemTag, TagDto, Guid>(
                updatedEntity.TaskItemTags,
                dto.Tags ?? [],
                e => e.TagId,
                i => i.Id,
                incomingDto =>
                {
                    var result = TaskItemTag.Create(updatedEntity.TenantId, updatedEntity.Id, incomingDto.Id!.Value);
                    if (result.IsSuccess) updatedEntity.TaskItemTags.Add(result.Value!);
                    return result;
                },
                removeFunc: toRemove =>
                {
                    if (relatedDeleteBehavior == RelatedDeleteBehavior.None) return DomainResult.Success();
                    db.Delete(toRemove);
                    updatedEntity.TaskItemTags.Remove(toRemove);
                    return DomainResult.Success();
                }
            ))
            .Map(updatedEntity)
        );
    }
}
