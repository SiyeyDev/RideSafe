using Cachacos;
using NUnit.Framework;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// Dónde busca el juego un asset que existe en varios idiomas.
    /// <para>
    /// La regla la fija la herramienta que los genera: escribe en
    /// <c>Resources/Sounds/&lt;categoría&gt;/&lt;idioma&gt;/&lt;término sin categoría&gt;</c>. Si la ruta
    /// que compone el juego no coincide, no falla nada: simplemente no suena, que es mucho peor
    /// de notar.
    /// </para>
    /// </summary>
    public class LocalizedResourceTests
    {
        [Test]
        public void La_ruta_es_carpeta_idioma_y_clave()
        {
            Assert.AreEqual("Sounds/M1/Español/Item_HelmetOk_Risk",
                LocalizedResource.PathFor("Sounds/M1", "Español", "Item_HelmetOk_Risk"));
        }

        [Test]
        public void La_categoria_de_la_clave_no_entra_en_la_ruta()
        {
            // El fichero se llama como el termino SIN categoria: la herramienta hace
            // term.Replace("M1/", ""). Si dejaramos el prefijo buscariamos
            // 'Sounds/M1/Español/M1/Item_...' y no hay nada ahi.
            Assert.AreEqual("Sounds/M1/English/Point_Brakes_Risk",
                LocalizedResource.PathFor("Sounds/M1", "English", "M1/Point_Brakes_Risk"));
        }

        [Test]
        public void Una_barra_de_mas_en_la_carpeta_no_parte_la_ruta()
        {
            Assert.AreEqual("Sounds/M1/English/Point_Brakes_Risk",
                LocalizedResource.PathFor("Sounds/M1/", "English", "M1/Point_Brakes_Risk"));
        }

        [Test]
        public void Sin_idioma_no_hay_ruta()
        {
            // Antes de que el aprendiz elija idioma no hay carpeta que valga. Devolver una ruta
            // a medias haria que Resources.Load buscara 'Sounds/M1//clave'.
            Assert.IsNull(LocalizedResource.PathFor("Sounds/M1", "", "M1/Point_Brakes_Risk"));
        }

        [Test]
        public void Sin_clave_no_hay_ruta()
        {
            Assert.IsNull(LocalizedResource.PathFor("Sounds/M1", "Español", ""));
        }
    }
}
