namespace RideSafe.Module01
{
    /// <summary>
    /// Clasificación de un elemento de protección personal.
    /// <para>
    /// No es un booleano a propósito: la narrativa exige que lo opcional y lo
    /// condicional no se cuenten como error, y que lo inapropiado se explique por
    /// su mecanismo de riesgo, no por estar "mal".
    /// </para>
    /// </summary>
    public enum SafetyItemCategory
    {
        /// <summary>Esperado siempre en la condición presentada.</summary>
        Core = 0,

        /// <summary>Correcto solo bajo la condición presentada, p. ej. lluvia o baja visibilidad.</summary>
        ConditionDependent = 1,

        /// <summary>Ni exigido ni dañino. Elegirlo u omitirlo nunca penaliza.</summary>
        Optional = 2,

        /// <summary>Compromete ajuste, visibilidad, atención o control.</summary>
        Inappropriate = 3
    }
}
