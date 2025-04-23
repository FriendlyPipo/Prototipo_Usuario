using Microsoft.AspNetCore.Mvc;
using Users.Application.DTO.Request;
using Users.Application.DTO.Response;
using Users.Application.Queries;
using MediatR;

namespace UserMs.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UserController(IMediator mediator)
        {
            _mediator = mediator;
        }

       [HttpPost]
       public async Task<IActionResult> CreateUser(CreateUserDTO createUserDTO)
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