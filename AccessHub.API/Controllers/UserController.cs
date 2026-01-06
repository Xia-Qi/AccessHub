using AccessHub.Application.Commands.Users;
using AccessHub.Application.Queries.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;

namespace AccessHub.API.Controllers
{
    [Route("/api/{Controller}")]
    public class UserController : ControllerBase
    {
        private readonly IMediator _mediator;
        public UserController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpGet]
        [Authorize(Policy = "UserRead")]
        public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] bool? isActive = null)
        {
            var result = await _mediator.Send(new GetUsersQuery { Page = page, PageSize = pageSize, Search = search, IsActive = isActive });
            return Ok(result);
        }
        [HttpGet("{username}")]
        [Authorize(Policy = "UserRead")]
        public async Task<IActionResult> Get(string username)
        {
            var result = await _mediator.Send(new GetUserDetailsQuery(username));
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "UserWrite")]
        public async Task<IActionResult> Create([FromBody] CreateUserCommand command)
        {
            var userId = await _mediator.Send(command);
            return Ok(new { id = userId.Value });
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "UserWrite")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserCommand command)
        {
            command.UserId = id;
            await _mediator.Send(command);
            return Ok();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "UserDelete")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _mediator.Send(new DeleteUserCommand { UserId = id });
            return Ok();
        }

        [HttpPut("{id}/roles")]
        [Authorize(Policy = "UserWrite")]
        public async Task<IActionResult> AssignRoles(Guid id, [FromBody] AssignUserRolesCommand command)
        {
            command.UserId = id;
            await _mediator.Send(command);
            return Ok();
        }
    }
}
