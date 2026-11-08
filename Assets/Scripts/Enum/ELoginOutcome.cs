using System;

/// <summary>Result of the single Log In button, including guest-link conflicts.</summary>
public enum LoginOutcome
{
    SignedIn,
    GuestLinked,
    SwitchedToExisting,
    SameAccount,
    ConfirmationRequired,
    Cancelled
}
