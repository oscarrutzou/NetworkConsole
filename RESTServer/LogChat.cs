using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration.UserSecrets;

using System.Diagnostics;
using TCP;

namespace RESTServer
{
    
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
        public IActionResult Post([FromBody] PostRestData post)
        {
            _log += ($"{post.Name}: {post.Message} \n");
            return Ok(_log);
        }

        //string apiUrl = "https://localhost:7019/LogChat/";

        //HttpClient httpClient = new HttpClient();
        //HttpJsonPostData postData = new HttpJsonPostData()
        //{_log+message+"/n"

        //};
    }
    
}
