using Mono.Cecil.Cil;
using System;
using UnityEngine;

public static class LanguageCodeExtensions
{
    public static ELanguageCode Next(this ELanguageCode current)
    {
        return (ELanguageCode)(((int)current + 1) % Enum.GetValues(typeof(ELanguageCode)).Length);
    }
}
