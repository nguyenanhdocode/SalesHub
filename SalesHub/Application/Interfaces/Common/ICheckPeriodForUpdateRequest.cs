namespace Application.Interfaces.Common;

public interface ICheckPeriodForUpdateRequest
{
    string TableName {get;}
    string PkName {get;}
    object PkValue {get;}
}
