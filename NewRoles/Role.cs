namespace TownOfHost.NewRoles;

public class Role
{
    public int Id { get; }
    public CustomRoles CustomRole { get; }
    public TabGroup Group { get; set; }
    public string Color { get; set; }
    public bool HasTask { get; set; }

    public Role(int id, CustomRoles role)
    {
        Id = id;
        CustomRole = role;
        Group = TabGroup.CrewmateRoles;
        Color = "#ffffff";
        HasTask = true;
    }

    public void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, Group, CustomRole);
    }
}