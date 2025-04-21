
// SE NECESITA CAMBIAR
using MediatR;
using System;

namespace UserMs.app.Queries;

public record GetUserByIdQuery(Guid UserId) : IRequest<UserDto>;

public record UserDto(Guid UserId, string UserCorreo, string UserNombre, DateTime CreatedAt);
