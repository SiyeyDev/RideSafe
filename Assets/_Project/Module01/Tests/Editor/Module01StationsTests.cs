using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// El reparto en sí: qué superficie le toca a cada zona. Es dato, no código, porque cuál
    /// mesa queda bien solo se acierta mirándolo en la escena.
    /// </summary>
    public class Module01StationsTests
    {
        private const string CatalogPath = "Assets/_Project/Module01/Data/SO_Module01Catalog.asset";

        [Test]
        public void Cada_zona_del_catalogo_tiene_su_estacion_declarada()
        {
            Module01CatalogSO catalog = AssetDatabase.LoadAssetAtPath<Module01CatalogSO>(CatalogPath);
            Assert.IsNotNull(catalog);

            foreach (ZoneSO zone in catalog.Zones)
                Assert.IsTrue(Module01Stations.TryGet(zone.ZoneId, out _),
                    "La zona '" + zone.ZoneId + "' no tiene estacion: sus signos «!» no tendrian "
                    + "mueble donde apoyarse.");
        }

        /// <summary>
        /// Lo que pidió Charlie: usar el entorno. Si las tres mesas vuelven a tener el mismo
        /// mueble principal, el garaje vuelve a ser una pared y un montón de objetos.
        /// </summary>
        [Test]
        public void Cada_mesa_tiene_su_propio_mueble_principal()
        {
            var principales = Module01Stations.All.Select(s => s.Surfaces[0].Name).ToList();

            CollectionAssert.AllItemsAreUnique(principales,
                "Dos zonas comparten mueble principal: eso es lo que habia antes.");
            Assert.GreaterOrEqual(principales.Count, 3);
        }

        [Test]
        public void Ninguna_tapa_esta_a_una_altura_imposible()
        {
            // La tapa principal va a la altura de una mesa; los niveles de más pueden ser un
            // estante a la altura de los ojos, como el de la entrada a 1,75 que pidió la PO.
            foreach (Station s in Module01Stations.All)
                for (int i = 0; i < s.Surfaces.Count; i++)
                    Assert.That(s.Surfaces[i].WorktopHeight, Is.InRange(.6f, i == 0 ? 1.3f : 1.8f),
                        "La tapa de '" + s.ZoneId + "' (" + s.Surfaces[i].Name + ") esta a "
                        + s.Surfaces[i].WorktopHeight + " m: o es el suelo o es un armario alto.");
        }
    }
}
