using AccessHub.Application.Commands.Permissions;
using AccessHub.Application.Queries.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessHub.API.Controllers
{
    [Route("/api/permission")]
    [ApiController]
    [Authorize(Policy = "UserRead")] // 暂时使用UserRead权限，后续可以创建Permission相关的权限
    public class PermissionController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PermissionController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
        {
            var result = await _mediator.Send(new GetPermissionsQuery { Page = page, PageSize = pageSize, Search = search });
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "UserWrite")]
        public async Task<IActionResult> Create([FromBody] CreatePermissionCommand command)
        {
            var permissionId = await _mediator.Send(command);
            return Ok(new { id = permissionId.Value });
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "UserWrite")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePermissionCommand command)
        {
            command.PermissionId = id;
            await _mediator.Send(command);
            return Ok();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "UserDelete")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _mediator.Send(new DeletePermissionCommand { PermissionId = id });
            return Ok();
        }
    }
}