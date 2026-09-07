using AccessHub.Application.Commands.Permissions;
using AccessHub.Application.Queries.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessHub.API.Controllers
{
    [Route("/api/permission")]
    [ApiController]
    // 双层授权:Controller 级 scope(用户管理模块)+ Action 级 permission
    // 权限定义属于角色管理范畴,故复用 perm.role.* 校验(超管 *.* 自动通过)。
    [Authorize(Policy = "ahb.usermgmt")]
    public class PermissionController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PermissionController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [Authorize(Policy = "perm.role.read")]
        public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
        {
            var result = await _mediator.Send(new GetPermissionsQuery { Page = page, PageSize = pageSize, Search = search });
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "perm.role.write")]
        public async Task<IActionResult> Create([FromBody] CreatePermissionCommand command)
        {
            var permissionId = await _mediator.Send(command);
            return Ok(new { id = permissionId.Value });
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "perm.role.write")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePermissionCommand command)
        {
            command.PermissionId = id;
            await _mediator.Send(command);
            return Ok();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "perm.role.delete")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _mediator.Send(new DeletePermissionCommand { PermissionId = id });
            return Ok();
        }
    }
}
