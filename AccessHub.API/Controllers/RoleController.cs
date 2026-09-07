using AccessHub.Application.Commands.Roles;
using AccessHub.Application.Queries.Roles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessHub.API.Controllers
{
    [Route("/api/role")]
    [ApiController]
    // 双层授权:Controller 级 scope(用户管理模块)+ Action 级 permission(角色操作动作)
    [Authorize(Policy = "ahb.usermgmt")]
    public class RoleController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RoleController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [Authorize(Policy = "perm.role.read")]
        public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
        {
            var result = await _mediator.Send(new GetRolesQuery { Page = page, PageSize = pageSize, Search = search });
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "perm.role.write")]
        public async Task<IActionResult> Create([FromBody] CreateRoleCommand command)
        {
            var roleId = await _mediator.Send(command);
            return Ok(new { id = roleId.Value });
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "perm.role.write")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleCommand command)
        {
            command.RoleId = id;
            await _mediator.Send(command);
            return Ok();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "perm.role.delete")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _mediator.Send(new DeleteRoleCommand { RoleId = id });
            return Ok();
        }

        [HttpPut("{id}/permissions")]
        [Authorize(Policy = "perm.role.write")]
        public async Task<IActionResult> AssignPermissions(Guid id, [FromBody] AssignRolePermissionsCommand command)
        {
            command.RoleId = id;
            await _mediator.Send(command);
            return Ok();
        }
    }
}
