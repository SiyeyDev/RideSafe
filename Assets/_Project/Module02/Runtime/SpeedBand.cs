namespace RideSafe.Module02
{
    /// <summary>Color de las flechas según la velocidad (reunión 2026-11-11).</summary>
    public enum SpeedBand { Green, Yellow, Red }

    public static class SpeedBands
    {
        /// <summary>
        /// Verde hasta el límite inclusive, amarillo hasta límite + tolerancia inclusive, rojo por encima.
        /// </summary>
        public static SpeedBand Classify(float speedKmh, float limitKmh, float toleranceKmh)
        {
            if (speedKmh <= limitKmh) return SpeedBand.Green;
            return speedKmh <= limitKmh + toleranceKmh ? SpeedBand.Yellow : SpeedBand.Red;
        }
    }
}
