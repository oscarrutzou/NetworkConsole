using Microsoft.AspNetCore.Mvc;
using RESTServer.ControllerClasses;

namespace RESTServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CharacterController : ControllerBase
    {

        private static Dictionary<Point, Character> _characters;

        static CharacterController()
        {
            _characters = new Dictionary<Point, Character>();
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(_characters.Values.ToArray());
        }

        [HttpGet("{point}")]
        public IActionResult Get(Point point)
        {
            if (_characters.ContainsKey(point))
                return Ok(_characters[point]);
            return NotFound();
        }

        [HttpPost]
        public IActionResult Post(int ownerId, Point pointPos, CharacterType characterType)
        {
            if (_characters.ContainsKey(pointPos))
            {
                return BadRequest(); // If the position is used, we throw a bad request, since we cant add it there.
            }
            _characters.Add(pointPos, NewCharacter(ownerId, pointPos, characterType));
            return Ok();
        }

        private Character NewCharacter(int ownerId, Point pointPos, CharacterType characterType)
        {
            return new Character()
            {
                Health = 5,
                Damage = 2,
                CharacterType = characterType,
                Name = "Bob",
                OwnerID = ownerId,
                Point = pointPos,
            };
        }
    }
}

//private readonly ILogger<WeatherForecastController> _logger;

//public WeatherForecastController(ILogger<WeatherForecastController> logger)
//{
//    _logger = logger;
//}