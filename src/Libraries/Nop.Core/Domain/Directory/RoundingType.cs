namespace Nop.Core.Domain.Directory
{
    public enum RoundingType
    {
        /// <summary>
        /// Default rounding (Match.Round(num, 2))
        /// </summary>
        Rounding001 = 0,
        /// <summary>
        /// Prices are rounded up to the nearest multiple of 5 cents for sales ending in: 3¢ & 4¢ round to 5¢; and, 8¢ & 9¢ round to 10¢
        /// </summary>
        Rounding005Up = 1,
        /// <summary>
        /// Prices are rounded down to the nearest multiple of 5 cents for sales ending in: 1¢ & 2¢ to 0¢; and, 6¢ & 7¢ to 5¢;
        /// </summary>
        Rounding005Down = 2,
        /// <summary>
        /// round up to the nearest 10 cent value for sales ending in 5¢;
        /// </summary>
        Rounding01Up = 3,
        /// <summary>
        /// round down to the nearest 10 cent value for sales ending in 5¢;
        /// </summary>
        Rounding01Down = 4,
        /// <summary>
        /// Sales ending in 1–24 öre round down to 0 öre.
        /// Sales ending in 25–49 öre round up to 50 öre.
        /// Sales ending in 51–74 öre round down to 50 öre.
        /// Sales ending in 75–99 öre round up to the next whole krona.
        /// </summary>
        Rounding05 = 5,
        /// <summary>
        /// Sales ending in 1–49 öre/øre round down to 0 öre/øre.
        /// Sales ending in 50–99 öre/øre round up to the next whole krona/krone.
        /// </summary>
        Rounding1 = 6,
        /// <summary>
        /// Sales ending in 1–99 öre/øre round up to the next whole krona/krone.
        /// </summary>
        Rounding1Up = 7
    }
}
