using EF.Data;
using EF.Data.Contracts;
using EF.Domain;
using EF.Domain.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories.Updaters;

/// <summary>Provides task item updater behavior for the Infrastructure Updaters layer.</summary>
internal static class TaskItemUpdater
{
    /// <summary>
    /// Applies the parent TaskItem DTO and synchronizes comments, checklist items, and tag links
    /// into the loaded aggregate graph. Create-mode UIs can send child records with client-only ids
    /// or empty TaskItemId values; the updater creates real children under the parent aggregate.
    /// When RelatedDeleteBehavior is RelationshipAndEntity, missing children are hard-deleted so
    /// a single parent save can represent client-side removals.
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
            // Children are mutated through the aggregate root's own methods (AddComment /
            // RemoveComment etc.), never by touching the navigation collections directly.
            // The updater then sets the EF change-tracker state explicitly: db.Add(child) on
            // create, db.Delete(child) on remove. db.Add is REQUIRED on the update path - a
            // navigation-add on an already-tracked, persisted parent can be inferred as Modified
            // (the key is a client-set Guid with no ValueGeneratedNever), which makes SaveChanges
            // emit an UPDATE against a non-existent row and throw DbUpdateConcurrencyException.
            // createFunc runs only for genuinely new children, so db.Add never double-inserts.
            CollectionUtility.SyncCollectionWithResult<Comment, CommentDto, Guid>(
                updatedEntity.Comments,
                dto.Comments ?? [],
                e => e.Id,
                i => i.Id,
                incomingDto =>
                {
                    var added = updatedEntity.AddComment(incomingDto.Body);
                    if (added.IsSuccess) db.Add(added.Value!);
                    return added;
                },
                (existing, incomingDto) => existing.Update(incomingDto.Body),
                toRemove =>
                {
                    if (relatedDeleteBehavior == RelatedDeleteBehavior.None) return DomainResult.Success();
                    updatedEntity.RemoveComment(toRemove);
                    db.Delete(toRemove);
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
                    var added = updatedEntity.AddChecklistItem(incomingDto.Title, incomingDto.SortOrder);
                    if (added.IsSuccess)
                    {
                        // Apply IsCompleted immediately - AddChecklistItem/Create don't take it,
                        // so a buffered "checked" item from the client would lose that state
                        // without this follow-up Update on the newly created child.
                        if (incomingDto.IsCompleted) added.Value!.Update(isCompleted: true);
                        db.Add(added.Value!);
                    }
                    return added;
                },
                (existing, incomingDto) => existing.Update(incomingDto.Title, incomingDto.IsCompleted, incomingDto.SortOrder),
                toRemove =>
                {
                    if (relatedDeleteBehavior == RelatedDeleteBehavior.None) return DomainResult.Success();
                    updatedEntity.RemoveChecklistItem(toRemove);
                    db.Delete(toRemove);
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
                    var added = updatedEntity.AssociateTag(incomingDto.Id!.Value);
                    if (added.IsSuccess) db.Add(added.Value!);
                    return added;
                },
                removeFunc: toRemove =>
                {
                    if (relatedDeleteBehavior == RelatedDeleteBehavior.None) return DomainResult.Success();
                    updatedEntity.RemoveTag(toRemove);
                    db.Delete(toRemove);
                    return DomainResult.Success();
                }
            ))
            .Map(updatedEntity)
        );
    }
}
