using AccessHub.Application.Commands.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccessHub.API.Controllers
{
    /// <summary>
    /// Defines the <see cref="AuthController" />
    /// </summary>
    [Route("auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        /// <summary>
        /// Defines the _mediator
        /// </summary>
        private readonly IMediator _mediator;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class.
        /// </summary>
        /// <param name="mediator">The mediator<see cref="IMediator"/></param>
        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// The Login
        /// </summary>
        /// <param name="input">The input<see cref="AuthCommand"/></param>
        /// <returns>The <see cref="Task{IActionResult}"/></returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login(AuthCommand input)
        {
            var user = await _mediator.Send(input);
            if (user == null)
                return Unauthorized();

            // 简单返回用户信息，实际场景应重定向到 /connect/authorize 或 生成授权 code
            return Ok(new { user.Id, user.Name });
        }
    }
}
