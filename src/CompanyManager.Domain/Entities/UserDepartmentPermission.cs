using CompanyManager.Domain.Enums;

namespace CompanyManager.Domain.Entities;

public class UserDepartmentPermission
{
    public Guid UserId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public PermissionLevel Level { get; private set; }

    private UserDepartmentPermission() { }

    public UserDepartmentPermission(Guid userId, Guid departmentId, PermissionLevel level)
    {
        UserId = userId;
        DepartmentId = departmentId;
        Level = level;
    }

    public void UpdateLevel(PermissionLevel level) => Level = level;
}
