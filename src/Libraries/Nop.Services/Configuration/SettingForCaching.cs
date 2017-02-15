using System;

namespace Nop.Services.Configuration
{
    [Serializable]
    public class SettingForCaching
    {
        /// <summary>
        /// Gets or sets the identifier of setting
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of setting
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value of setting
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the store of setting for which the setting is valid; 0 is set when the setting is for all stores
        /// </summary>
        public int StoreId { get; set; }
    }
}
