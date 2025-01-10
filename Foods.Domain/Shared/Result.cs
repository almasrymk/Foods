namespace Foods.Domain.Shared
{
    using System.Net;

    public partial record Result(HttpStatusCode StatusCode, List<string>? Errors);
    public partial record Result<TResponse>(HttpStatusCode StatusCode, TResponse? Response, List<string>? Errors);
}