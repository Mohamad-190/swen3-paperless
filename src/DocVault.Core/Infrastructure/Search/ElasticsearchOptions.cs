namespace DocVault.Core.Infrastructure.Search;

public class ElasticsearchOptions
{
    public const string SectionName = "Elasticsearch";

    public string Url { get; set; } = "http://localhost:9200";
    public string IndexName { get; set; } = "documents";
}
