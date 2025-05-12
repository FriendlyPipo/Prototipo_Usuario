using Microsoft.AspNetCore.Mvc;
using Users.Application.DTO.Request;
using Users.Application.DTO.Respond;
using Users.Application.Queries;
using Users.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace Users.Api.Controllers
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
        [Authorize] 
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

        [HttpDelete("DeleteUser")]
        [Authorize] 
        public async Task<IActionResult> DeleteUser(DeleteUserDTO deleteUserDTO)
        {
            try
            {   
                var command = new DeleteUserCommand(deleteUserDTO);
                var userId = await _mediator.Send(command);
                return Ok(userId);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPut("UpdateUser")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(UpdateUserDTO updateUserDTO)
        {
            try
            {
                var command = new UpdateUserCommand(updateUserDTO);
                var userId = await _mediator.Send(command);
                return Ok(userId);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("GetUserById")]
        [Authorize] 
        public async Task<IActionResult> GetUserById(Guid userId)
        {
            try
            {
                var query = new GetUserByIdQuery(userId);
                var user = await _mediator.Send(query);
                return Ok(user);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        } 

        [HttpGet("GetAllUsers")]
        [Authorize]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var query = new GetAllUsesrQuery();
                var users = await _mediator.Send(query);
                return Ok(users);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}   