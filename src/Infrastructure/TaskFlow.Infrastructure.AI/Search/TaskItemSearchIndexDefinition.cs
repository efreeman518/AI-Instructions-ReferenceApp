using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace TaskFlow.Infrastructure.AI.Search;

public static class TaskItemSearchIndexDefinition
{
    public const string IndexName = "taskitems-index";

    public static SearchIndex Create()
    {
        var index = new SearchIndex(IndexName)
        {
            Fields =
            [
                new SimpleField("Id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
                new SimpleField("TenantId", SearchFieldDataType.String) { IsFilterable = true },
                new SearchableField("Title") { IsFilterable = true, IsSortable = true },
                new SearchableField("Description"),
                new SimpleField("Status", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                new SimpleField("Priority", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                new SearchableField("CategoryName") { IsFilterable = true },
                new SimpleField("DueDate", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
                new SimpleField("CompletedDate", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
                new SimpleField("LastUpdated", SearchFieldDataType.DateTimeOffset) { IsSortable = true },
                new SearchField("ContentVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = 1536,
                    VectorSearchProfileName = "vector-profile"
                }
            ],
            VectorSearch = new VectorSearch
            {
                Profiles = { new VectorSearchProfile("vector-profile", "hnsw-config") },
                Algorithms = { new HnswAlgorithmConfiguration("hnsw-config") }
            },
            SemanticSearch = new SemanticSearch
            {
                Configurations =
                {
                    new SemanticConfiguration("default", new SemanticPrioritizedFields
                    {
                        TitleField = new SemanticField("Title"),
                        ContentFields = { new SemanticField("Description") },
                        KeywordsFields = { new SemanticField("CategoryName"), new SemanticField("Status") }
                    })
                },
                DefaultConfigurationName = "default"
            }
        };

        return index;
    }
}
