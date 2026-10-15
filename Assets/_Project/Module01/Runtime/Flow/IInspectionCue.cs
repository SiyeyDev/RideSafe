namespace RideSafe.Module01
{
    /// <summary>
    /// Cómo un punto de inspección le dice al aprendiz «mira esto».
    /// <para>
    /// Hay dos maneras y conviven: <see cref="InspectionMarker"/>, el signo de exclamación
    /// flotante, y <see cref="InspectionHighlight"/>, el contorno sobre la pieza real de la
    /// malla. Cuál se usa depende de qué piezas traiga despiezadas el vehículo —la e-bike,
    /// <c>manubrio</c> y <c>rueda_trasera</c>; el patinete, además, su <c>freno</c>—, y eso
    /// es cosa del escenario, no del recorrido. Ningún vehículo las tiene todas: lo que no
    /// existe como pieza se queda con el signo.
    /// </para>
    /// <para>
    /// <see cref="ZoneRunner"/> habla por esta interfaz justamente para no enterarse: recorre
    /// todos los avisos que cuelguen del punto y les manda lo mismo.
    /// </para>
    /// </summary>
    public interface IInspectionCue
    {
        /// <summary>¿Es el punto que toca revisar ahora?</summary>
        void SetHighlighted(bool value);

        /// <summary>¿Ya lo resolvió el aprendiz?</summary>
        void SetResolved(bool value);

        /// <summary>Lo tapa algo —el popup del inspector— y no debe verse, pase lo que pase.</summary>
        void SetObscured(bool value);
    }
}
