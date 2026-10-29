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
            foreach (Station s in Module01Stations.All)
                foreach (Surface top in s.Surfaces)
                    Assert.That(top.WorktopHeight, Is.InRange(.6f, 1.3f),
                        "La tapa de '" + s.ZoneId + "' (" + top.Name + ") esta a "
                        + top.WorktopHeight + " m: o es el suelo o es un armario alto.");
        }
    }
}
