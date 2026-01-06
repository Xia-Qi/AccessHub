using AccessHub.Application.Commands.Roles;
using AccessHub.Application.Queries.Roles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessHub.API.Controllers
{
    [Route("/api/role")]
    [ApiController]
    [Authorize(Policy = "UserRead")] // 暂时使用UserRead权限，后续可以创建Role相关的权限
    public class RoleController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RoleController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
        {
            var result = await _mediator.Send(new GetRolesQuery { Page = page, PageSize = pageSize, Search = search });
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "UserWrite")]
        public async Task<IActionResult> Create([FromBody] CreateRoleCommand command)
        {
            var roleId = await _mediator.Send(command);
            return Ok(new { id = roleId.Value });
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "UserWrite")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleCommand command)
        {
            command.RoleId = id;
            await _mediator.Send(command);
            return Ok();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "UserDelete")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _mediator.Send(new DeleteRoleCommand { RoleId = id });
            return Ok();
        }

        [HttpPut("{id}/permissions")]
        [Authorize(Policy = "UserWrite")]
        public async Task<IActionResult> AssignPermissions(Guid id, [FromBody] AssignRolePermissionsCommand command)
        {
            command.RoleId = id;
            await _mediator.Send(command);
            return Ok();
        }
    }
}