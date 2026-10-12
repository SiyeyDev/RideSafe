using UnityEngine;

namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// Utilidades de componentes para los comandos de editor del módulo 1.
    /// </summary>
    public static class Module01EditorComponents
    {
        /// <summary>
        /// Devuelve el componente del objeto, y lo añade si no está.
        /// <para>
        /// Existe para no volver a escribir <c>GetComponent&lt;T&gt;() ?? AddComponent&lt;T&gt;()</c>,
        /// que <b>parece</b> correcto y no lo es: en el Editor, <c>GetComponent</c> de un
        /// componente ausente no devuelve null sino un objeto de diagnóstico que solo es null
        /// para el <c>==</c> sobrecargado de Unity. Para C#, y por tanto para <c>??</c>, ese
        /// objeto es perfectamente válido, así que <c>AddComponent</c> nunca se llega a llamar
        /// y la primera línea que toque el componente revienta con <c>MissingComponentException</c>.
        /// </para>
        /// <para>
        /// La trampa solo se nota cuando el componente falta de verdad, que es justo el caso
        /// que la expresión pretendía cubrir: por eso aguantó sin dar la cara.
        /// </para>
        /// </summary>
        public static T Ensure<T>(GameObject go) where T : Component
        {
            T existing = go.GetComponent<T>();
            return existing == null ? go.AddComponent<T>() : existing;
        }
    }
}
