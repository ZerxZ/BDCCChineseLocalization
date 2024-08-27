namespace BDCCChineseLocalization;
using Flurl;
using Flurl.Http;

public class ParatranzClient
{
    private string _apiKey;
    public const string BaseUrl = "https://paratranz.cn/api/";
    public ParatranzClient(string apiKey)
    {
        ApiKey = apiKey.Trim();
    }
    public string ApiKey
    {
        get => _apiKey;
        set => _apiKey = value;
    }
    public async Task<IFlurlResponse> BuildArtifactAsync(int project,CancellationToken cancellationToken)
    {

        return await BaseUrl
            .AppendPathSegments("projects", project, "artifacts")
            .WithHeader("Authorization",ApiKey).PostJsonAsync(null, cancellationToken:cancellationToken);
    }
    public async Task<Stream> DownloadArtifactAsync(int project, CancellationToken cancellationToken)
    {
        return await   BaseUrl
                .AppendPathSegments("projects", project, "artifacts", "download")
            .WithHeader("Authorization",ApiKey)
            .GetStreamAsync(cancellationToken: cancellationToken);
    }
}