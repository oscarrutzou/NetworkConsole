using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration.UserSecrets;
using RESTServer.ControllerClasses;
using System.Diagnostics;

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
        
        [HttpPost("{message}")]
        public IActionResult Post(string message)
        {
            
            return Ok(_log+message+"/n");
        }

        //string apiUrl = "https://localhost:7019/LogChat/";

        //HttpClient httpClient = new HttpClient();
        //HttpJsonPostData postData = new HttpJsonPostData()
        //{
            
        //};
    }
}
