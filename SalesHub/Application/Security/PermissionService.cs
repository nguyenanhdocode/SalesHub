using Application.Database;
using Application.Models.Security;
using Dapper;
using Microsoft.AspNetCore.Routing;

namespace Infrastructure.Security;

public class PermissionService
{
    private readonly DbSession _dbSession;
    private Dictionary<Guid, IEnumerable<PermissionServiceDto>> _permissions = new();

    public PermissionService(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    public IEnumerable<PermissionServiceDto> GetByUserId(Guid userId)
    {
        if (_permissions.ContainsKey(userId))
        {
            return _permissions[userId];
        }

        return Enumerable.Empty<PermissionServiceDto>();
    }

    private const string LOAD_BY_USER_QUERY = @"
    SELECT
        permissions.code AS PermissionCode
        , features.code AS FeatureCode
        , modules.code AS ModuleCode
    FROM group_user
    INNER JOIN group_permission ON group_permission.group_id = group_user.group_id
    INNER JOIN permissions ON permissions.permission_id = group_permission.permission_id
    INNER JOIN features ON features.feature_id = permissions.feature_id
	INNER JOIN modules ON modules.module_id = features.module_id
    WHERE group_user.user_id = @UserId
    ";

    public async Task LoadByUserId(Guid userId)
    {
        var rows = await _dbSession.Connection.QueryAsync<PermissionServiceDto>(LOAD_BY_USER_QUERY, new
        {
            UserId = userId
        });

        if (_permissions.ContainsKey(userId))
        {
            _permissions.Remove(userId);
        }

        _permissions.Add(userId, rows);
    }

    private const string LOAD_ALL_QUERY = @"
    SELECT
        permissions.code AS PermissionCode
        , features.code AS FeatureCode
        , modules.code AS ModuleCode
        , group_user.user_id AS UserId
    FROM group_user
    INNER JOIN group_permission ON group_permission.group_id = group_user.group_id
    INNER JOIN permissions ON permissions.permission_id = group_permission.permission_id
    INNER JOIN features ON features.feature_id = permissions.feature_id
	INNER JOIN modules ON modules.module_id = features.module_id
    ";

    public async Task LoadAll()
    {
        var rows = await _dbSession.Connection.QueryAsync<PermissionServiceDto>(LOAD_ALL_QUERY);

        _permissions = rows.GroupBy(p => p.UserId).ToDictionary(p => p.First().UserId, p => p.AsEnumerable());
    }
}
