using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration.UserSecrets;

using System.Diagnostics;
using TCP;

namespace RESTServer;


[ApiController]
[Route("[controller]")]
public class LogChat : ControllerBase
{
    
    private static string _log;
    static LogChat()
    {
        
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(_log);
    }
    
    [HttpPost]
    public IActionResult Post([FromBody] string post)
    {
        _log += ($"    - {post}\n");
        return Ok(_log);
    }
}
