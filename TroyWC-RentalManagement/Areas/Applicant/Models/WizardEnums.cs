namespace TroyWC_RentalManagement.Areas.Applicant.Models;

/// <summary>Sections of the rental application, in order.</summary>
public enum ApplicationStep
{
    Applicant,
    Residences,
    Review,
}

/// <summary>Posted by the wizard's submit buttons (name="Command").</summary>
public enum WizardCommand
{
    /// <summary>Validate the section; save and move on only when it is valid.</summary>
    Continue,

    /// <summary>Save the section as entered, valid or not.</summary>
    Save,

    /// <summary>Add or replace the residence in the modal. Kept in the form only, not saved.</summary>
    SaveResidence,

    /// <summary>Drop the residence at CommandIndex from the form, not saved.</summary>
    RemoveResidence,
}
