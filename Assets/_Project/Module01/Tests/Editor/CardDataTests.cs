using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using RideSafe.UI;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// Lo que una tarjeta de evaluación necesita saber (documento del equipo, 2026-11-04:
    /// cuadrícula 3×2, «el render del objeto por encima del texto», icono de estado en la
    /// esquina superior derecha).
    /// <para>
    /// Aquí solo se afirma el <b>dato</b>, no la maquetación: de dónde sale la imagen de cada
    /// tarjeta, de qué elemento es y qué estado le toca. Con esto puesto, diagramar es colocar.
    /// </para>
    /// <para>
    /// Hasta hoy la identidad del elemento viajaba en <c>Detail</c> —el mismo campo que se
    /// enseña como texto—, así que una tarjeta no podía decir de qué es sin leer su descripción.
    /// </para>
    /// </summary>
    public class CardDataTests : CatalogFixture
    {
        private readonly List<Object> _sprites = new List<Object>();

        [TearDown]
        public void DestroySprites()
        {
            foreach (Object s in _sprites)
                if (s != null) Object.DestroyImmediate(s);
            _sprites.Clear();
        }

        private Sprite NuevoSprite()
        {
            Texture2D tex = new Texture2D(4, 4);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.one * .5f);
            _sprites.Add(tex);
            _sprites.Add(sprite);
            return sprite;
        }

        private List<ComparisonEntry> Tarjetas(Module01CatalogSO catalog, params string[] elegidos)
        {
            SelectionLedger ledger = new SelectionLedger();
            foreach (string id in elegidos)
                ledger.Add(id);
            SelectionReport report = SelectionGrader.Grade(catalog, ledger);
            return ReportPresenter.BuildEntries(catalog, report, k => k);
        }

        [Test]
        public void Cada_tarjeta_sabe_de_que_elemento_es_sin_mirar_su_texto()
        {
            SafetyItemSO casco = Item("helmet_ok", SafetyItemCategory.Core);
            Module01CatalogSO catalog = Catalog(Zone("proteccion", casco));

            List<ComparisonEntry> tarjetas = Tarjetas(catalog, "helmet_ok");

            Assert.That(tarjetas, Has.Count.EqualTo(1), "premisa: una tarjeta por elemento elegido");
            Assert.That(tarjetas[0].ItemId, Is.EqualTo("helmet_ok"),
                        "la tarjeta identifica su elemento por id propio, no por el texto de detalle");
        }

        [Test]
        public void La_tarjeta_lleva_la_imagen_que_declara_su_elemento()
        {
            SafetyItemSO casco = Item("helmet_ok", SafetyItemCategory.Core);
            Sprite render = NuevoSprite();
            casco.ConfigureThumbnailForTests(render);
            Module01CatalogSO catalog = Catalog(Zone("proteccion", casco));

            List<ComparisonEntry> tarjetas = Tarjetas(catalog, "helmet_ok");

            Assert.That(tarjetas[0].Image, Is.SameAs(render),
                        "el render del elemento es lo que manda en la tarjeta");
        }

        [Test]
        public void Un_elemento_sin_render_deja_la_imagen_vacia_en_vez_de_reventar()
        {
            SafetyItemSO guantes = Item("gloves", SafetyItemCategory.Optional);
            Module01CatalogSO catalog = Catalog(Zone("proteccion", guantes));

            List<ComparisonEntry> tarjetas = Tarjetas(catalog, "gloves");

            Assert.That(tarjetas[0].Image, Is.Null,
                        "sin render la tarjeta se queda sin imagen; la maquetación decidirá qué poner");
            Assert.That(tarjetas[0].ItemId, Is.EqualTo("gloves"), "y sigue sabiendo de qué es");
        }
    }
}
