using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace TDMS.Verify;

public static partial class Checks
{
    /// <summary>Crew and user names: separate fields, no initials, one-legal-name rule.</summary>
    [Check]
    public static string NameStandard_SeparateFields_NoInitials()
    {
        if (NameHelper.CheckStandard("Dela Cruz", "Juan Carlos", "Santos") != null) return "a full name was refused";
        if (NameHelper.CheckStandard("Dela Cruz", "Juan", "") != null) return "a name with no middle name was refused";
        if (NameHelper.CheckStandard("Madonna", ".", "") != null) return "a single legal name (period in First Name) was refused";
        if (NameHelper.CheckStandard("", "Juan", "") == null) return "a blank Last Name was accepted";
        if (NameHelper.CheckStandard("Dela Cruz", "", "") == null) return "a blank First Name was accepted";
        if (NameHelper.CheckStandard("Dela Cruz", "J.", "") == null) return "an initial as First Name was accepted";
        if (NameHelper.CheckStandard("Dela Cruz", "Juan", "S.") == null) return "an initial as Middle Name was accepted";
        return "";
    }
}
