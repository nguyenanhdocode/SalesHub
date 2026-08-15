namespace Application.Features.Auth.Login;

public class UserDto
{
    public Guid UserId {get;set;}
    public string UserName {get;set;} = null!;
    public string Password {get;set;} = null!;
    public DateTime? LockUntil {get;set;}
    public int LoginFailedCount {get;set;}
    public bool Activated {get;set;}
}
