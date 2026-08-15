namespace Application.Models.Security;

public class PermissionServiceDto
{
    public string PermissionCode {get;set;} = null!;
    public string FeatureCode {get;set;} = null!;
    public string ModuleCode {get;set;} = null!;
    public Guid UserId {get;set;}
}
