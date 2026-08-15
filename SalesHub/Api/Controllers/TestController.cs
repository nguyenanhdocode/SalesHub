using Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MyApp.Namespace
{
    [Route("api/test")]
    [ApiController]
    public class TestController : ControllerBase
    {
        readonly DocumentNoService _docService;
        public TestController(DocumentNoService docService)
        {
            _docService = docService;
        }

        [HttpGet]
        [Route("document-no")]
        public async Task<string> GetDocNo()
        {
            return await _docService.GetNextDocumentNo("GI", 2026, 1);
        }
    }
}
