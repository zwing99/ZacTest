using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ZacTest.src
{
    [Route("api/test")]
    [ApiController]
    public class SimpleController(IUserRepository userRepository) : ControllerBase
    {
        private readonly IUserRepository _userRepository = userRepository;

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var users = await _userRepository.GetAllUsersAsync();
            return await Task.FromResult<IActionResult>(Ok(users));
        }
    }
}
