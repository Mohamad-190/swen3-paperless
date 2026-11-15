namespace DocVault.Core.Infrastructure.Storage;

public class S3StorageOptions
{
    public const string SectionName = "S3Storage";

    public string ServiceUrl { get; set; } = "http://localhost:9000";
    public string AccessKey { get; set; } = "rustfsadmin";
    public string SecretKey { get; set; } = "rustfsadmin";
    public string BucketName { get; set; } = "documents";
}
