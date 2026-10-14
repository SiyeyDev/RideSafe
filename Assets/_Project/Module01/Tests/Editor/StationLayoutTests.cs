using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// Repartir los EPP por el garaje en vez de amontonarlos en el mostrador del fondo.
    /// <para>
    /// La geometría va aparte del montador de escena a propósito: colocar una fila de objetos
    /// sobre una tapa y derivar desde dónde se mira es aritmética, y la aritmética se puede
    /// equivocar en silencio. Aquí se afirma sin abrir una escena.
    /// </para>
    /// </summary>
    public class StationLayoutTests
    {
        /// <summary>Una tapa de 3 m de largo en X, a 1 m de alto, contra la pared del fondo.</summary>
        private static Bounds Mostrador() =>
            new Bounds(new Vector3(0f, .5f, -4.5f), new Vector3(3f, 1f, .6f));

        /// <summary>Una mesa de 2.6 m de largo en Z, contra la pared de la izquierda.</summary>
        private static Bounds MesaLateral() =>
            new Bounds(new Vector3(-4.4f, .46f, 3.2f), new Vector3(.88f, .92f, 2.6f));

        private static List<Vector3> Tres() => new List<Vector3>
        {
            new Vector3(.24f, .31f, .29f),   // un casco
            new Vector3(.21f, .40f, .35f),   // otro casco
            new Vector3(.17f, .08f, .15f),   // unas gafas
        };

        [Test]
        public void Los_elementos_se_apoyan_en_la_tapa_de_la_superficie()
        {
            List<Vector3> tam = Tres();

            Vector3[] sitios = StationLayout.Arrange(Mostrador(), 1f, StationAxis.X, tam, .34f);

            for (int i = 0; i < sitios.Length; i++)
                Assert.AreEqual(1f, sitios[i].y - tam[i].y * .5f, .0001f,
                    "La base del elemento " + i + " no queda sobre la tapa.");
        }

        [Test]
        public void Repartir_por_X_deja_la_profundidad_quieta()
        {
            Bounds mesa = Mostrador();

            Vector3[] sitios = StationLayout.Arrange(mesa, 1f, StationAxis.X, Tres(), .34f);

            Assert.AreEqual(3, sitios.Select(s => Mathf.Round(s.x * 100f)).Distinct().Count(),
                "Repartiendo por X los tres tienen que caer en sitios distintos.");
            foreach (Vector3 s in sitios)
                Assert.AreEqual(mesa.center.z, s.z, .0001f, "La profundidad no se toca.");
        }

        [Test]
        public void Repartir_por_Z_deja_la_anchura_quieta()
        {
            Bounds mesa = MesaLateral();

            Vector3[] sitios = StationLayout.Arrange(mesa, .92f, StationAxis.Z, Tres(), .34f);

            Assert.AreEqual(3, sitios.Select(s => Mathf.Round(s.z * 100f)).Distinct().Count());
            foreach (Vector3 s in sitios)
                Assert.AreEqual(mesa.center.x, s.x, .0001f, "La anchura no se toca.");
        }

        [Test]
        public void Entre_dos_elementos_queda_exactamente_el_hueco_pedido()
        {
            List<Vector3> tam = Tres();

            Vector3[] sitios = StationLayout.Arrange(Mostrador(), 1f, StationAxis.X, tam, .34f);

            for (int i = 0; i < sitios.Length - 1; i++)
            {
                float borde = sitios[i].x + tam[i].x * .5f;
                float siguiente = sitios[i + 1].x - tam[i + 1].x * .5f;
                Assert.AreEqual(.34f, siguiente - borde, .0001f,
                    "Los elementos " + i + " y " + (i + 1) + " no respetan el hueco.");
            }
        }

        [Test]
        public void El_grupo_queda_centrado_en_la_superficie()
        {
            Bounds mesa = Mostrador();
            List<Vector3> tam = Tres();

            Vector3[] sitios = StationLayout.Arrange(mesa, 1f, StationAxis.X, tam, .34f);

            float izq = sitios[0].x - tam[0].x * .5f;
            float der = sitios[2].x + tam[2].x * .5f;
            Assert.AreEqual(mesa.center.x, (izq + der) * .5f, .0001f);
        }

        /// <summary>
        /// Que el grupo quepa se pregunta <b>antes</b> de colocarlo. Si no cabe, lo que pasa es
        /// que los elementos cuelgan por fuera de la mesa, y eso en la captura parece un bug
        /// del arte en vez de una estación mal declarada.
        /// </summary>
        [Test]
        public void Se_puede_saber_si_el_grupo_cabe_antes_de_colocarlo()
        {
            List<Vector3> tam = Tres();
            float largo = StationLayout.Span(tam, StationAxis.X, .34f);

            Assert.AreEqual(.24f + .21f + .17f + .34f * 2f, largo, .0001f);
            Assert.Less(largo, Mostrador().size.x, "En 3 m caben tres cosas pequenas.");
            Assert.Greater(largo, .5f);
        }


        /// <summary>
        /// Una fila mas larga que el mueble no se centra: arranca en su borde. Centrarla la
        /// deja colgando por los dos lados, y lo que cuelga por el principio son los elementos
        /// que van sobre la tapa. Medido: las botas acababan 32 cm fuera de la mesa.
        /// </summary>
        [Test]
        public void Una_fila_mas_larga_que_el_mueble_arranca_en_su_borde()
        {
            Bounds mesa = MesaLateral();                     // 2.6 m de largo en Z
            var tam = new List<Vector3>
            {
                new Vector3(.3f, .2f, .9f), new Vector3(.3f, .2f, .9f),
                new Vector3(.3f, .2f, .9f), new Vector3(.3f, .2f, .9f),
            };

            Vector3[] sitios = StationLayout.Arrange(mesa, .92f, StationAxis.Z, tam, .34f);

            Assert.AreEqual(mesa.min.z, sitios[0].z - tam[0].z * .5f, .001f,
                "El primero tiene que empezar justo en el borde del mueble.");
            Assert.Greater(sitios[3].z + tam[3].z * .5f, mesa.max.z,
                "Lo que sobra cuelga por el final, que es donde van los que acaban en el suelo.");
        }

        [Test]
        public void El_punto_de_vista_se_planta_delante_del_grupo_y_lo_mira()
        {
            Bounds grupo = new Bounds(new Vector3(0f, 1.2f, -4.4f), new Vector3(1.2f, .4f, .3f));

            Pose ojo = StationLayout.ViewPoint(grupo, Vector3.forward, 1.9f, 1.45f, 0f);

            // Lo lateral lo fija el desvio del checklist y lo guardan sus propios tests.
            Assert.AreEqual(-4.4f + 1.9f, ojo.position.z, .0001f, "Se planta a la distancia pedida.");
            Assert.Less(Vector3.Angle(ojo.rotation * Vector3.forward, Vector3.back), .5f,
                "Tiene que mirar hacia el grupo, no hacia la pared de enfrente.");
        }

        [Test]
        public void El_punto_de_vista_va_a_la_altura_de_los_ojos_y_en_horizontal()
        {
            Bounds alto = new Bounds(new Vector3(0f, 2.4f, -4.4f), Vector3.one * .3f);
            Bounds bajo = new Bounds(new Vector3(0f, .3f, -4.4f), Vector3.one * .3f);

            Pose a = StationLayout.ViewPoint(alto, Vector3.forward, 1.9f, 1.45f, 0f);
            Pose b = StationLayout.ViewPoint(bajo, Vector3.forward, 1.9f, 1.45f, 0f);

            Assert.AreEqual(1.45f, a.position.y, .0001f);
            Assert.AreEqual(1.45f, b.position.y, .0001f);
            // En VR el cabeceo lo pone el visor, no el rig: inclinar el ancla no se ve y encima
            // hace crecer la caja del pedestal del inspector. Ya se probó y fue peor.
            Assert.AreEqual(0f, (a.rotation * Vector3.forward).y, .0001f, "El ancla va en horizontal.");
        }


        /// <summary>
        /// El checklist vive en la mitad derecha de la pantalla y tapa casi la mitad del
        /// encuadre. Con el grupo centrado delante del aprendiz, lo que cae a su derecha
        /// desaparece: medido, el <b>casco bueno</b> —el elemento clave de su zona— quedaba
        /// entero detrás del panel, y la bufanda de la zona de ropa también.
        /// <para>
        /// Así que el aprendiz se planta un poco hacia el lado del checklist, lo justo para que
        /// el grupo entero caiga en la mitad limpia. Cuánto se aparta <b>se deriva del propio
        /// grupo</b>, no es un número escrito: al añadir un elemento la fila crece y el desvío
        /// crece con ella.
        /// </para>
        /// </summary>
        [Test]
        public void El_grupo_entero_queda_fuera_de_donde_vive_el_checklist()
        {
            // Mirando hacia -Z, la derecha de la camara es -X.
            Bounds grupo = new Bounds(new Vector3(-2.47f, 1.2f, -4.47f), new Vector3(1.30f, .4f, .36f));

            Pose ojo = StationLayout.ViewPoint(grupo, Vector3.forward, 1.9f, 1.45f, .25f);

            Vector3 derecha = ojo.rotation * Vector3.right;
            float d = Vector3.Distance(
                new Vector3(ojo.position.x, 0f, ojo.position.z),
                new Vector3(grupo.center.x, 0f, grupo.center.z));
            float mediaPantalla = Module01Stations.HalfWidthPerMeter * d;
            float dondeEmpiezaElPanel = (2f * Module01Stations.HudClearFraction - 1f) * mediaPantalla;

            float e0 = Vector3.Dot(new Vector3(grupo.min.x - ojo.position.x, 0f, 0f), derecha);
            float e1 = Vector3.Dot(new Vector3(grupo.max.x - ojo.position.x, 0f, 0f), derecha);

            Assert.LessOrEqual(Mathf.Max(e0, e1), dondeEmpiezaElPanel - .25f + .001f,
                "El borde del grupo se mete debajo del checklist.");
            Assert.GreaterOrEqual(Mathf.Min(e0, e1), -mediaPantalla + .25f - .001f,
                "El otro borde del grupo se sale por la izquierda de la pantalla.");
        }

        [Test]
        public void Un_grupo_mas_ancho_se_mira_desde_mas_al_lado()
        {
            Bounds corto = new Bounds(new Vector3(0f, 1.2f, -4.4f), new Vector3(.60f, .4f, .3f));
            Bounds largo = new Bounds(new Vector3(0f, 1.2f, -4.4f), new Vector3(1.80f, .4f, .3f));

            Pose a = StationLayout.ViewPoint(corto, Vector3.forward, 1.9f, 1.45f, .25f);
            Pose b = StationLayout.ViewPoint(largo, Vector3.forward, 1.9f, 1.45f, .25f);

            Assert.Greater(Mathf.Abs(b.position.x), Mathf.Abs(a.position.x),
                "Una fila mas larga pide apartarse mas, o el ultimo elemento vuelve a taparse.");
        }

        [Test]
        public void Apartarse_no_cambia_la_distancia_al_grupo_en_profundidad()
        {
            Bounds grupo = new Bounds(new Vector3(0f, 1.2f, -4.4f), new Vector3(1.3f, .4f, .3f));

            Pose ojo = StationLayout.ViewPoint(grupo, Vector3.forward, 1.9f, 1.45f, .25f);

            Assert.AreEqual(-4.4f + 1.9f, ojo.position.z, .0001f,
                "Si el grupo cabe, la profundidad es la de siempre y el desvio es lateral.");
        }


        /// <summary>
        /// Si la fila no cabe en la mitad limpia, hay que apartarse mas. Los tres elementos de
        /// la zona de ropa miden 1.70 m y a 1.90 m las botas se salian por el borde izquierdo:
        /// no era cuestion de desviarse mas, sino de alejarse.
        /// </summary>
        [Test]
        public void Un_grupo_que_no_cabe_en_la_mitad_limpia_se_mira_desde_mas_lejos()
        {
            Bounds ancho = new Bounds(new Vector3(0f, 1.2f, -4.4f), new Vector3(2.60f, .4f, .3f));

            Pose ojo = StationLayout.ViewPoint(ancho, Vector3.forward, 1.9f, 1.45f, .25f);

            float d = ojo.position.z - (-4.4f);
            Assert.Greater(d, 1.9f, "Con 2.60 m de fila, 1.90 m de distancia no dan para verla.");
            Assert.GreaterOrEqual(StationLayout.ClearWidthPerMeter * d, 2.60f + .5f - .001f,
                "La distancia elegida tiene que dar para la fila mas sus dos margenes.");
        }

        [Test]
        public void Mirar_desde_otro_lado_cambia_el_sitio_y_el_giro()
        {
            Bounds grupo = new Bounds(new Vector3(-4.4f, 1f, 3.2f), new Vector3(.3f, .4f, 1.2f));

            Pose ojo = StationLayout.ViewPoint(grupo, Vector3.right, 1.9f, 1.45f, 0f);

            Assert.AreEqual(-4.4f + 1.9f, ojo.position.x, .0001f, "Se planta a la distancia pedida.");
            Assert.Less(Vector3.Angle(ojo.rotation * Vector3.forward, Vector3.left), .5f);
        }
    }

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
                    "La zona '" + zone.ZoneId + "' no tiene estacion: sus EPP se quedarian "
                    + "donde el arte los dejo, sin agrupar y sin ancla.");
        }

        /// <summary>
        /// Lo que pidió Charlie: usar el entorno. Si las tres estaciones vuelven a caer en el
        /// mismo mueble, el garaje vuelve a ser una pared y un montón de objetos.
        /// </summary>
        [Test]
        public void Las_estaciones_estan_repartidas_por_muebles_distintos()
        {
            var superficies = Module01Stations.All.Select(s => s.SurfaceName).ToList();

            CollectionAssert.AllItemsAreUnique(superficies,
                "Dos zonas comparten mueble: eso es lo que habia antes.");
            Assert.GreaterOrEqual(superficies.Count, 3);
        }

        [Test]
        public void Cada_estacion_declara_desde_que_lado_se_mira()
        {
            foreach (Station s in Module01Stations.All)
            {
                Assert.AreEqual(1f, s.Outward.magnitude, .001f,
                    "El lado de '" + s.ZoneId + "' tiene que ser una direccion unitaria.");
                Assert.AreEqual(0f, s.Outward.y, .0001f,
                    "Se mira en horizontal: el aprendiz no flota ni se agacha.");
            }
        }

        [Test]
        public void Ninguna_estacion_se_apoya_a_una_altura_imposible()
        {
            foreach (Station s in Module01Stations.All)
                Assert.That(s.WorktopHeight, Is.InRange(.6f, 1.3f),
                    "La tapa de '" + s.ZoneId + "' (" + s.SurfaceName + ") esta a "
                    + s.WorktopHeight + " m: o es el suelo o es un armario alto.");
        }
    }
}
