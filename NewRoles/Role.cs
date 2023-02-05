using AmongUs.GameOptions;

namespace TownOfHost.NewRoles;

public class Role
{
    public int Id { get; }
    public CustomRoles CustomRole { get; }
    public TabGroup Group { get; set; }
    public string Color { get; set; }
    public bool HasTask { get; set; }
    public RoleTypes BaseRole { get; set; }
    public string Name { get; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string Info { get; set; }

    public Role(int id, CustomRoles role)
    {
        Id = id;
        CustomRole = role;
        Group = TabGroup.CrewmateRoles;
        Color = "#ffffff";
        HasTask = true;
        BaseRole = RoleTypes.Crewmate;
        Name = role.ToString();
        DisplayName = Translator.GetString(Name);
        Description = Translator.GetString(Name + "LongInfo");
        Info = Translator.GetString(Name + "Info");;
    }
}