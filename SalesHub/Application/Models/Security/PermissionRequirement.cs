namespace Application.Models.Security;

public enum PermissionOperator
{
    Any, All
}

public class PermissionRequirement
{
    public IEnumerable<string> Permissions {get;set;} = Enumerable.Empty<string>();
    public PermissionOperator Operator {get;set;} = PermissionOperator.All;
}
