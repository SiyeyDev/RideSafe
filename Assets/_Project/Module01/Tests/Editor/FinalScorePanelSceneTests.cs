using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// El panel de la nota final existe en la escena, está cableado y sigue las reglas de UI del
    /// kit del atlas, como el resto de los paneles del módulo (PO, 2026-10-29).
    /// <para>
    /// Se mide contra la escena y no contra el constructor: lo que se rompe de verdad es la
    /// referencia serializada —alguien reconstruye la presentación, el panel se recrea y el
    /// campo del componente se queda apuntando al viejo—, y eso solo se ve en el asset.
    /// </para>
    /// </summary>
    public class FinalScorePanelSceneTests
    {
        private const string V5Path = "Assets/_Project/Scene/RideSafe_Garaje_v5.unity";

        private Scene _v5;
        private bool _abiertaAqui;

        [OneTimeSetUp]
        public void Open()
        {
            _v5 = SceneManager.GetSceneByPath(V5Path);
            _abiertaAqui = !_v5.isLoaded;
            if (_abiertaAqui)
                _v5 = EditorSceneManager.OpenScene(V5Path, OpenSceneMode.Additive);
        }

        [OneTimeTearDown]
        public void Close()
        {
            if (_abiertaAqui)
                EditorSceneManager.CloseScene(_v5, true);
        }

        private IEnumerable<GameObject> Todo() => _v5.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject);

        private Module01Experience Experiencia() =>
            Todo().Select(g => g.GetComponent<Module01Experience>()).FirstOrDefault(x => x != null);

        private SerializedObject Campos() => new SerializedObject(Experiencia());

        private GameObject Referido(string campo)
        {
            SerializedProperty p = Campos().FindProperty(campo);
            Assert.IsNotNull(p, campo + ": el campo no existe en Module01Experience");
            Object v = p.objectReferenceValue;
            Assert.IsNotNull(v, campo + ": está sin asignar en la escena");
            return v is GameObject go ? go : ((Component)v).gameObject;
        }

        [Test]
        public void La_experiencia_esta_en_la_escena()
        {
            Assert.IsNotNull(Experiencia(), "sin Module01Experience no hay nada que medir");
        }

        [TestCase("_finalScorePanel")]
        [TestCase("_finalAverageText")]
        [TestCase("_finalSectionsText")]
        [TestCase("_finalVerdictText")]
        public void Los_cuatro_campos_del_panel_final_estan_cableados(string campo)
        {
            Assert.IsNotNull(Referido(campo));
        }

        [Test]
        public void El_panel_nace_apagado()
        {
            // Si naciera encendido se vería la nota del módulo desde el primer segundo, con los
            // ceros de no haber hecho nada. La nota es el cierre, no el estado inicial.
            Assert.IsFalse(Referido("_finalScorePanel").activeSelf);
        }

        [Test]
        public void Los_tres_textos_cuelgan_del_panel()
        {
            Transform panel = Referido("_finalScorePanel").transform;
            foreach (string campo in new[] { "_finalAverageText", "_finalSectionsText", "_finalVerdictText" })
                Assert.IsTrue(Referido(campo).transform.IsChildOf(panel),
                    campo + " no está dentro del panel: se quedaría visible al ocultarlo");
        }

        [Test]
        public void Los_textos_usan_las_fuentes_del_kit_y_no_la_de_por_defecto()
        {
            // La regla de UI del proyecto: nada de la fuente por defecto de TMP. El promedio va
            // en la fuente de titular y los dos renglones de apoyo en la de texto.
            foreach (string campo in new[] { "_finalAverageText", "_finalSectionsText", "_finalVerdictText" })
            {
                TMP_Text t = Referido(campo).GetComponent<TMP_Text>();
                Assert.IsNotNull(t, campo + " no es un texto de TMP");
                Assert.IsNotNull(t.font, campo + " sin fuente");
                Assert.IsTrue(t.font.name.StartsWith("Manrope") || t.font.name.StartsWith("Inter"),
                    campo + " usa " + t.font.name + ", que no es del kit");
            }
        }

        [Test]
        public void El_promedio_se_lee_mas_grande_que_el_resto()
        {
            // Es la cifra que la persona busca. Si todo mide igual hay que leerse el panel para
            // encontrarla.
            float promedio = Referido("_finalAverageText").GetComponent<TMP_Text>().fontSize;
            foreach (string campo in new[] { "_finalSectionsText", "_finalVerdictText" })
                Assert.Greater(promedio, Referido(campo).GetComponent<TMP_Text>().fontSize,
                    "el promedio tiene que destacar sobre " + campo);
        }

        [Test]
        public void El_titulo_del_panel_sale_de_I2_y_no_esta_escrito_a_mano()
        {
            // Ningún texto visible en código: es criterio de «terminado» del módulo. El título
            // es el único de los cuatro que es fijo, así que es el único que puede traer clave.
            Transform titulo = Referido("_finalScorePanel").transform.Find("Title");
            Assert.IsNotNull(titulo, "el panel no tiene Title");

            // El componente del proyecto se llama `Module01Text` y guarda la clave; es lo que
            // vuelve a resolver el texto al cambiar de idioma en el módulo 0.
            Component localizador = titulo.GetComponents<Component>()
                .FirstOrDefault(c => c != null && c.GetType().Name == "Module01Text");
            Assert.IsNotNull(localizador,
                "Title no tiene Module01Text: el texto se quedaría en el idioma del build");
        }

        [Test]
        public void El_panel_no_tapa_los_botones_de_continuar_y_repetir()
        {
            // Los dos botones del cierre viven en la misma capa. Si el panel los solapa, el clic
            // se lo come él y la pantalla final parece colgada.
            RectTransform panel = Referido("_finalScorePanel").GetComponent<RectTransform>();
            Assert.IsNotNull(panel);

            foreach (string nombre in new[] { "ContinueModule2Button", "RepeatModuleButton" })
            {
                GameObject boton = Todo().FirstOrDefault(g => g.name == nombre);
                if (boton == null)
                    continue;
                RectTransform rt = boton.GetComponent<RectTransform>();
                if (rt == null || rt.transform.parent == panel.transform)
                    continue;

                Assert.IsFalse(Solapan(panel, rt), nombre + " queda debajo del panel de la nota");
            }
        }

        /// <summary>Cajas en el mundo, que es lo que el raycaster compara.</summary>
        private static bool Solapan(RectTransform a, RectTransform b)
        {
            Bounds ca = Caja(a), cb = Caja(b);
            return ca.Intersects(cb);
        }

        private static Bounds Caja(RectTransform rt)
        {
            Vector3[] esquinas = new Vector3[4];
            rt.GetWorldCorners(esquinas);
            Bounds b = new Bounds(esquinas[0], Vector3.zero);
            for (int i = 1; i < 4; i++)
                b.Encapsulate(esquinas[i]);
            return b;
        }
    }
}
