using System;

namespace RideSafe.Module01
{
    /// <summary>
    /// La nota con la que se cierra <b>una</b> sección: su cifra y si se sigue o se repite.
    /// <para>
    /// Vive aparte de <c>Module01Experience</c> a propósito. Es una función de un
    /// <see cref="SelectionScore"/> y nada más —ni escena, ni panel, ni fase—, así que se prueba
    /// sin entrar en Play y, sobre todo, <b>sobrevive a que se reordene el final del módulo</b>:
    /// se llama desde donde haga falta el día que la pantalla de «¿estás listo para el viaje?»
    /// cambie de sitio.
    /// </para>
    /// <para>
    /// Es la nota de la sección y no el promedio del módulo: en este punto la cifra que decide
    /// es la de la parte que se acaba de cerrar, porque es la que manda repetirla.
    /// </para>
    /// </summary>
    public static class SectionScoreNote
    {
        /// <summary>El azul apagado del veredicto, el mismo que usa la nota final.</summary>
        private const string VerdictColor = "#4A6480";

        /// <summary>
        /// Compone la nota. <paramref name="translate"/> resuelve las claves de I2; si es
        /// <c>null</c> salen las claves tal cual, que delata la traducción que falte en vez de
        /// dejar el hueco en blanco.
        /// </summary>
        public static string Compose(SelectionScore score, Func<string, string> translate)
        {
            string key = score.FailsExam ? "M1/Section_MustRepeat" : "M1/Section_CanAdvance";
            string verdict = translate != null ? translate(key) : key;

            // La cifra en grande y el veredicto con palabras debajo: «62 / 100» por sí solo no
            // le dice a nadie si puede seguir.
            return "<size=300%><b>" + score.Percent + " / 100</b></size>\n"
                   + "<color=" + VerdictColor + ">" + verdict + "</color>";
        }
    }
}
