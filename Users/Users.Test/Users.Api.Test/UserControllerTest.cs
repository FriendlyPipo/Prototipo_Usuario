using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Users.Api.Controllers;
using Users.Application.Commands;
using Users.Application.DTO.Request;
using Users.Application.DTO.Respond;
using Users.Application.Queries;
using MediatR;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace Users.Test.Users.Api.Test
{
    public class UserControllerTest
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly Mock<ILogger<UserController>> _mockLogger;
        private readonly UserController _controller;

        private class ApiResponse
        {
            public string Message { get; set; }
        }

        private class ApiResponseWithId : ApiResponse
        {
            public string UserId { get; set; }
        }

        private class ForgotPasswordResponse
        {
            public string message { get; set; }
        }

        public UserControllerTest()
        {
            _mockMediator = new Mock<IMediator>();
            _mockLogger = new Mock<ILogger<UserController>>();
            _controller = new UserController(_mockMediator.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task CreateUser_Success_ReturnsOkResult()
        {
            // Arrange
            var createUserDTO = new CreateUserDTO
            {
                UserName = "User",
                UserLastName = "Test",
                UserEmail = "Tes@example.com",
                UserPhoneNumber = "12345678901",
                UserDirection = "123 Main St",
                UserRole = "Administrador",
                UserPassword = "Password123!"
            };
            var expectedUserId = Guid.NewGuid().ToString();

            _mockMediator.Setup(x => x.Send(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedUserId);

            // Act
            var result = await _controller.CreateUser(createUserDTO);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponseWithId>(JsonSerializer.Deserialize<ApiResponseWithId>(
                JsonSerializer.Serialize(okResult.Value)));
            Assert.Equal(expectedUserId, response.UserId);
            Assert.Contains("Usuario creado exitosamente", response.Message);
        }

        [Fact]
        public async Task CreateUser_Exception_ReturnsBadRequest()
        {
            // Arrange
            var createUserDTO = new CreateUserDTO
            {
                UserName = "Test",
                UserLastName = "User",
                UserEmail = "Test@example.com",
                UserPhoneNumber = "12345678901",
                UserDirection = "123 Main St",
                UserRole = "Administrador",
                UserPassword = "Password123!"
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Error al crear usuario"));

            // Act
            var result = await _controller.CreateUser(createUserDTO);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(JsonSerializer.Deserialize<ApiResponse>(
                JsonSerializer.Serialize(badRequestResult.Value)));
            Assert.Equal("Error al crear usuario", response.Message);
        }

        [Fact]
        public async Task DeleteUser_Success_ReturnsOkResult()
        {
            // Arrange
            var deleteUserDTO = new DeleteUserDTO { UserId = Guid.NewGuid() };

            _mockMediator.Setup(x => x.Send(It.IsAny<DeleteUserCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Usuario eliminado exitosamente.");

            // Act
            var result = await _controller.DeleteUser(deleteUserDTO);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(JsonSerializer.Deserialize<ApiResponse>(
                JsonSerializer.Serialize(okResult.Value)));
            Assert.Equal("Usuario eliminado exitosamente.", response.Message);
        }

        [Fact]
        public async Task DeleteUser_Exception_ReturnsBadRequest()
        {
            // Arrange
            var deleteUserDTO = new DeleteUserDTO { UserId = Guid.NewGuid() };

            _mockMediator.Setup(x => x.Send(It.IsAny<DeleteUserCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Error al eliminar usuario"));

            // Act
            var result = await _controller.DeleteUser(deleteUserDTO);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(JsonSerializer.Deserialize<ApiResponse>(
                JsonSerializer.Serialize(badRequestResult.Value)));
            Assert.Equal("Error al eliminar usuario", response.Message);
        }

        [Fact]
        public async Task UpdateUser_Success_ReturnsOkResult()
        {
            // Arrange
            var updateUserDTO = new UpdateUserDTO 
            { 
                UserId = Guid.NewGuid(),
                UserName = "User Updated",
                UserLastName = "Test Updated",
                UserEmail = "updated@example.com",
                UserPhoneNumber = "12345678901",
                UserDirection = "456 Main St",
                UserRole = "Postor"
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<UpdateUserCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Usuario actualizado exitosamente.");

            // Act
            var result = await _controller.UpdateUser(updateUserDTO);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(JsonSerializer.Deserialize<ApiResponse>(
                JsonSerializer.Serialize(okResult.Value)));
            Assert.Equal("Usuario actualizado exitosamente.", response.Message);
        }

        [Fact]
        public async Task UpdateUser_Exception_ReturnsBadRequest()
        {
            // Arrange
            var updateUserDTO = new UpdateUserDTO { UserId = Guid.NewGuid() };

            _mockMediator.Setup(x => x.Send(It.IsAny<UpdateUserCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Error al actualizar usuario"));

            // Act
            var result = await _controller.UpdateUser(updateUserDTO);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(JsonSerializer.Deserialize<ApiResponse>(
                JsonSerializer.Serialize(badRequestResult.Value)));
            Assert.Equal("Error al actualizar usuario", response.Message);
        }

        [Fact]
        public async Task GetUserById_Success_ReturnsOkResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedUser = new GetUserDTO 
            { 
                UserId = userId,
                UserName = "User",
                UserLastName = "Test",
                UserEmail = "Test@example.com"
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<GetUserByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedUser);

            // Act
            var result = await _controller.GetUserById(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUser = Assert.IsType<GetUserDTO>(okResult.Value);
            Assert.Equal(expectedUser.UserId, returnedUser.UserId);
        }

        [Fact]
        public async Task GetUserById_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _mockMediator.Setup(x => x.Send(It.IsAny<GetUserByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((GetUserDTO)null);

            // Act
            var result = await _controller.GetUserById(userId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(JsonSerializer.Deserialize<ApiResponse>(
                JsonSerializer.Serialize(notFoundResult.Value)));
            Assert.Contains($"Usuario con ID {userId} no encontrado", response.Message);
        }

        [Fact]
        public async Task GetAllUsers_Success_ReturnsOkResult()
        {
            // Arrange
            var expectedUsers = new List<GetUserDTO>
            {
                new GetUserDTO { UserId = Guid.NewGuid(), UserName = "User", UserLastName = "Test", UserEmail = "Test@example.com" },
                new GetUserDTO { UserId = Guid.NewGuid(), UserName = "User2", UserLastName = "Test2", UserEmail = "Test2@example.com" }
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<GetAllUsersQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedUsers);

            // Act
            var result = await _controller.GetAllUsers();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUsers = Assert.IsType<List<GetUserDTO>>(okResult.Value);
            Assert.Equal(expectedUsers.Count, returnedUsers.Count);
        }

        [Fact]
        public async Task GetAllUsers_Exception_ReturnsBadRequest()
        {
            // Arrange
            _mockMediator.Setup(x => x.Send(It.IsAny<GetAllUsersQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Error al obtener usuarios"));

            // Act
            var result = await _controller.GetAllUsers();

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(JsonSerializer.Deserialize<ApiResponse>(
                JsonSerializer.Serialize(badRequestResult.Value)));
            Assert.Equal("Error al obtener usuarios", response.Message);
        }

        [Fact]
        public async Task ForgotPassword_Success_ReturnsOkResult()
        {
            // Arrange
            var forgotPasswordDTO = new ForgotPasswordDTO { UserEmail = "test@example.com" };
            var expectedMessage = "Se ha enviado un correo con instrucciones para restablecer la contraseña.";

            _mockMediator.Setup(x => x.Send(It.IsAny<ForgotPasswordCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedMessage);

            // Act
            var result = await _controller.ForgotPassword(forgotPasswordDTO);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ForgotPasswordResponse>(JsonSerializer.Deserialize<ForgotPasswordResponse>(
                JsonSerializer.Serialize(okResult.Value)));
            Assert.Equal(expectedMessage, response.message);
        }

        [Fact]
        public async Task ForgotPassword_Exception_ReturnsInternalServerError()
        {
            // Arrange
            var forgotPasswordDTO = new ForgotPasswordDTO { UserEmail = "test@example.com" };

            _mockMediator.Setup(x => x.Send(It.IsAny<ForgotPasswordCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Error interno"));

            // Act
            var result = await _controller.ForgotPassword(forgotPasswordDTO);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var response = Assert.IsType<ForgotPasswordResponse>(JsonSerializer.Deserialize<ForgotPasswordResponse>(
                JsonSerializer.Serialize(statusCodeResult.Value)));
            Assert.Equal("Ocurrió un error inesperado al procesar tu solicitud.", response.message);
        }
    }
}
