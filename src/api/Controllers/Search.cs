using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Search : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public Search(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET: api/Search
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SearchDocument>>> GetSearchResults(string query)
        {
            var endpoint = new Uri(_configuration["Search:Endpoint"]!);
            var indexName = _configuration["Search:IndexName"]!;
            var key = new AzureKeyCredential(_configuration["Search:ApiKey"]!);

            var client = new SearchClient(endpoint, indexName, key);
            var options = new SearchOptions { Size = 10 };
            var results = await client.SearchAsync<SearchDocument>(
                string.IsNullOrWhiteSpace(query) ? "*" : query,
                options
            );

            var docs = new List<SearchDocument>();
            await foreach (var r in results.Value.GetResultsAsync())
                docs.Add(r.Document);

            return Ok(docs);
        }
    }
}
