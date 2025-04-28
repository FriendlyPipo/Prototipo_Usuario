using Microsoft.AspNetCore.Mvc;
using Users.Application.DTO.Request;
using Users.Application.DTO.Respond;
using Users.Application.Queries;
using Users.Application.Commands;
using MediatR;

namespace UserMs.Api.Controllers
{
    [ApiController]
    [Route("users")]
    public class UserController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UserController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("CreateUser")]
        public async Task<IActionResult> CreateUser(CreateUserDTO createUserDTO)
        {
            try
            {
                var command = new CreateUserCommand(createUserDTO);
                var userId = await _mediator.Send(command);
                return Ok(userId);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}