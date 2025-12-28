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
        public async Task<IActionResult> Index()
        {
            var user = await _mediator.Send(new GetUserDetailsQuery("test"));
            return Ok(user.Name);
        }
    }
}
