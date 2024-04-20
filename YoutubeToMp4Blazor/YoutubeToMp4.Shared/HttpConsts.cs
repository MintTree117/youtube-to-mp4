namespace YoutubeToMp4.Shared;

public static class HttpConsts
{
    public const string DevelopmentAddress = "https://localhost:7985/";
    public const string ProductionAddress = "https://coral-app-anp75.ondigitalocean.app/";
    
    public const string HttpAuthHeader = "Authorization";
    public const string HttpAuthKey = "AccessKey";
    
    public const string UserAuth = "/api/stream";
    public const string AdminAuth = "/api/admin";
    
    public const string GetStreamInfo = "/api/stream/info";
    public const string GetStreamDownload = "/api/stream/download";
    
    public const string GetEnvVars = "/api/admin/post/get-vars";
    public const string InitFromDb = "/api/admin/post/fetch-db";
    public const string InitFromJson = "/api/admin/put/json";
    public const string UpdateDbRecords = "/api/admin/put/update-db";
    public const string PrintKeyRecords = "/api/admin/get/key-records";
}