using Application.Features.Auth.Login;
using Application.Features.User.Create;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("/api/auth")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Route("login")]
    public async Task<IResult> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result =  await _sender.Send(command, cancellationToken);

        return Results.Ok(result);
    }
}
