using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Configuration;
using Nop.Core.Data;
using Nop.Core.Domain.Configuration;
using Nop.Services.Events;

namespace Nop.Services.Configuration
{
    /// <summary>
    /// Setting manager
    /// </summary>
    public partial class SettingService : ISettingService
    {
        #region Constants

        /// <summary>
        /// Key for caching settings
        /// </summary>
        /// <remarks>
        /// {0} : settings key
        /// </remarks>
        private const string SETTINGS_BY_KEY = "Nop.setting.{0}";

        /// <summary>
        /// Key pattern to clear cache
        /// </summary>
        private const string SETTINGS_PATTERN_KEY = "Nop.setting.";

        #endregion

        #region Fields

        private readonly IRepository<Setting> _settingRepository;
        private readonly IEventPublisher _eventPublisher;
        private readonly ICacheManager _cacheManager;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="cacheManager">Cache manager</param>
        /// <param name="eventPublisher">Event publisher</param>
        /// <param name="settingRepository">Setting repository</param>
        public SettingService(ICacheManager cacheManager, IEventPublisher eventPublisher,
            IRepository<Setting> settingRepository)
        {
            this._cacheManager = cacheManager;
            this._eventPublisher = eventPublisher;
            this._settingRepository = settingRepository;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Gets cached settings by key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>List of settings</returns>
        protected virtual IList<SettingForCaching> GetSettingsForCachingByKey(string key)
        {
            key = key.Trim().ToLowerInvariant();

            //cache settings
            var cacheKey = string.Format(SETTINGS_BY_KEY, key);
            return _cacheManager.Get(key, () =>
            {
                var query = _settingRepository.TableNoTracking
                    .Where(setting => setting.Name.Equals(key, StringComparison.InvariantCultureIgnoreCase));

                return query.Select(setting => new SettingForCaching
                {
                    Id = setting.Id,
                    Name = setting.Name,
                    Value = setting.Value,
                    StoreId = setting.StoreId
                }).ToList();
            });
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds a setting
        /// </summary>
        /// <param name="setting">Setting</param>
        /// <param name="clearCache">A value indicating whether to clear cache after setting update</param>
        public virtual void InsertSetting(Setting setting, bool clearCache = true)
        {
            if (setting == null)
                throw new ArgumentNullException("setting");

            _settingRepository.Insert(setting);

            //cache
            if (clearCache)
                _cacheManager.RemoveByPattern(SETTINGS_PATTERN_KEY);

            //event notification
            _eventPublisher.EntityInserted(setting);
        }

        /// <summary>
        /// Updates a setting
        /// </summary>
        /// <param name="setting">Setting</param>
        /// <param name="clearCache">A value indicating whether to clear cache after setting update</param>
        public virtual void UpdateSetting(Setting setting, bool clearCache = true)
        {
            if (setting == null)
                throw new ArgumentNullException("setting");

            _settingRepository.Update(setting);

            //cache
            if (clearCache)
                _cacheManager.RemoveByPattern(SETTINGS_PATTERN_KEY);

            //event notification
            _eventPublisher.EntityUpdated(setting);
        }

        /// <summary>
        /// Deletes a setting
        /// </summary>
        /// <param name="setting">Setting</param>
        public virtual void DeleteSetting(Setting setting)
        {
            if (setting == null)
                throw new ArgumentNullException("setting");

            _settingRepository.Delete(setting);

            //cache
            _cacheManager.RemoveByPattern(SETTINGS_PATTERN_KEY);

            //event notification
            _eventPublisher.EntityDeleted(setting);
        }

        /// <summary>
        /// Deletes settings
        /// </summary>
        /// <param name="settings">Settings</param>
        public virtual void DeleteSettings(IList<Setting> settings)
        {
            if (settings == null)
                throw new ArgumentNullException("settings");

            _settingRepository.Delete(settings);

            //cache
            _cacheManager.RemoveByPattern(SETTINGS_PATTERN_KEY);

            //event notification
            foreach (var setting in settings)
            {
                _eventPublisher.EntityDeleted(setting);
            }
        }

        /// <summary>
        /// Gets a setting by identifier
        /// </summary>
        /// <param name="settingId">Setting identifier</param>
        /// <returns>Setting</returns>
        public virtual Setting GetSettingById(int settingId)
        {
            if (settingId == 0)
                return null;

            return _settingRepository.GetById(settingId);
        }

        /// <summary>
        /// Get setting by key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="loadSharedValueIfNotFound">A value indicating whether a shared (for all stores) value should be loaded if a value specific for a certain is not found</param>
        /// <returns>Setting</returns>
        public virtual Setting GetSetting(string key, int storeId = 0, bool loadSharedValueIfNotFound = false)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            //get cached settings
            var allSettingsByKey = GetSettingsForCachingByKey(key);

            //try get setting
            var settingForCaching = allSettingsByKey.FirstOrDefault(settings => settings.StoreId == storeId);

            //load shared value?
            if (settingForCaching == null && storeId > 0 && loadSharedValueIfNotFound)
                settingForCaching = allSettingsByKey.FirstOrDefault(settings => settings.StoreId == 0);

            return settingForCaching != null ? GetSettingById(settingForCaching.Id) : null;
        }

        /// <summary>
        /// Get setting value by key
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="key">Key</param>
        /// <param name="defaultValue">Default value</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="loadSharedValueIfNotFound">A value indicating whether a shared (for all stores) value should be loaded if a value specific for a certain is not found</param>
        /// <returns>Setting value</returns>
        public virtual T GetSettingByKey<T>(string key, T defaultValue = default(T), int storeId = 0, bool loadSharedValueIfNotFound = false)
        {
            if (string.IsNullOrEmpty(key))
                return defaultValue;

            //get cached settings
            var allSettingsByKey = GetSettingsForCachingByKey(key);

            //try get setting
            var settingForCaching = allSettingsByKey.FirstOrDefault(settings => settings.StoreId == storeId);

            //load shared value?
            if (settingForCaching == null && storeId > 0 && loadSharedValueIfNotFound)
                settingForCaching = allSettingsByKey.FirstOrDefault(settings => settings.StoreId == 0);

            return settingForCaching != null ? CommonHelper.To<T>(settingForCaching.Value)  : defaultValue;
        }

        /// <summary>
        /// Set setting value
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="clearCache">A value indicating whether to clear cache after setting update</param>
        public virtual void SetSetting<T>(string key, T value, int storeId = 0, bool clearCache = true)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            //get value as string
            var stringValue = TypeDescriptor.GetConverter(typeof(T)).ConvertToInvariantString(value);

            //get cached settings
            var allSettingsByKey = GetSettingsForCachingByKey(key);

            //try get setting
            var settingForCaching = allSettingsByKey.FirstOrDefault(settings => settings.StoreId == storeId);
            if (settingForCaching != null)
            {
                //update setting
                var setting = GetSettingById(settingForCaching.Id);
                setting.Value = stringValue;
                UpdateSetting(setting, clearCache);
            }
            else
            {
                //or insert new one
                var setting = new Setting
                {
                    Name = key,
                    Value = stringValue,
                    StoreId = storeId
                };
                InsertSetting(setting, clearCache);
            }
        }

        /// <summary>
        /// Gets all settings
        /// </summary>
        /// <returns>Settings</returns>
        public virtual IList<Setting> GetAllSettings()
        {
            var query = from s in _settingRepository.Table
                        orderby s.Name, s.StoreId
                        select s;
            var settings = query.ToList();
            return settings;
        }

        /// <summary>
        /// Determines whether a setting exists
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <typeparam name="TPropType">Property type</typeparam>
        /// <param name="settings">Entity</param>
        /// <param name="keySelector">Key selector</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>true -setting exists; false - does not exist</returns>
        public virtual bool SettingExists<T, TPropType>(T settings, 
            Expression<Func<T, TPropType>> keySelector, int storeId = 0) 
            where T : ISettings, new()
        {
            string key = settings.GetSettingKey(keySelector);

            var setting = GetSettingByKey<string>(key, storeId: storeId);
            return setting != null;
        }

        /// <summary>
        /// Load settings
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="storeId">Store identifier for which settigns should be loaded</param>
        public virtual T LoadSetting<T>(int storeId = 0) where T : ISettings, new()
        {
            var settings = Activator.CreateInstance<T>();

            foreach (var prop in typeof(T).GetProperties())
            {
                // get properties we can read and write to
                if (!prop.CanRead || !prop.CanWrite)
                    continue;

                var key = typeof(T).Name + "." + prop.Name;
                //load by store
                var setting = GetSettingByKey<string>(key, storeId: storeId, loadSharedValueIfNotFound: true);
                if (setting == null)
                    continue;

                if (!TypeDescriptor.GetConverter(prop.PropertyType).CanConvertFrom(typeof(string)))
                    continue;

                if (!TypeDescriptor.GetConverter(prop.PropertyType).IsValid(setting))
                    continue;

                object value = TypeDescriptor.GetConverter(prop.PropertyType).ConvertFromInvariantString(setting);

                //set property
                prop.SetValue(settings, value, null);
            }

            return settings;
        }

        /// <summary>
        /// Save settings object
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="storeId">Store identifier</param>
        /// <param name="settings">Setting instance</param>
        public virtual void SaveSetting<T>(T settings, int storeId = 0) where T : ISettings, new()
        {
            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            foreach (var prop in typeof(T).GetProperties())
            {
                // get properties we can read and write to
                if (!prop.CanRead || !prop.CanWrite)
                    continue;

                if (!TypeDescriptor.GetConverter(prop.PropertyType).CanConvertFrom(typeof(string)))
                    continue;

                string key = typeof(T).Name + "." + prop.Name;
                //Duck typing is not supported in C#. That's why we're using dynamic type
                dynamic value = prop.GetValue(settings, null);
                if (value != null)
                    SetSetting(key, value, storeId, false);
                else
                    SetSetting(key, "", storeId, false);
            }

            //and now clear cache
            ClearCache();
        }

        /// <summary>
        /// Save settings object
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <typeparam name="TPropType">Property type</typeparam>
        /// <param name="settings">Settings</param>
        /// <param name="keySelector">Key selector</param>
        /// <param name="storeId">Store ID</param>
        /// <param name="clearCache">A value indicating whether to clear cache after setting update</param>
        public virtual void SaveSetting<T, TPropType>(T settings,
            Expression<Func<T, TPropType>> keySelector,
            int storeId = 0, bool clearCache = true) where T : ISettings, new()
        {
            var member = keySelector.Body as MemberExpression;
            if (member == null)
            {
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a method, not a property.",
                    keySelector));
            }

            var propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
            {
                throw new ArgumentException(string.Format(
                       "Expression '{0}' refers to a field, not a property.",
                       keySelector));
            }

            string key = settings.GetSettingKey(keySelector);
            //Duck typing is not supported in C#. That's why we're using dynamic type
            dynamic value = propInfo.GetValue(settings, null);
            if (value != null)
                SetSetting(key, value, storeId, clearCache);
            else
                SetSetting(key, "", storeId, clearCache);
        }

        /// <summary>
        /// Save settings object (per store). If the setting is not overridden per storem then it'll be delete
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <typeparam name="TPropType">Property type</typeparam>
        /// <param name="settings">Settings</param>
        /// <param name="keySelector">Key selector</param>
        /// <param name="overrideForStore">A value indicating whether to setting is overridden in some store</param>
        /// <param name="storeId">Store ID</param>
        /// <param name="clearCache">A value indicating whether to clear cache after setting update</param>
        public virtual void SaveSettingOverridablePerStore<T, TPropType>(T settings,
            Expression<Func<T, TPropType>> keySelector,
            bool overrideForStore, int storeId = 0, bool clearCache = true) where T : ISettings, new()
        {
            if (overrideForStore || storeId == 0)
                SaveSetting(settings, keySelector, storeId, clearCache);
            else if (storeId > 0)
                DeleteSetting(settings, keySelector, storeId);
        }

        /// <summary>
        /// Delete all settings
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        public virtual void DeleteSetting<T>() where T : ISettings, new()
        {
            var settingsToDelete = new List<Setting>();
            var allSettings = GetAllSettings();
            foreach (var prop in typeof(T).GetProperties())
            {
                string key = typeof(T).Name + "." + prop.Name;
                settingsToDelete.AddRange(allSettings.Where(x => x.Name.Equals(key, StringComparison.InvariantCultureIgnoreCase)));
            }

            DeleteSettings(settingsToDelete);
        }

        /// <summary>
        /// Delete settings object
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <typeparam name="TPropType">Property type</typeparam>
        /// <param name="settings">Settings</param>
        /// <param name="keySelector">Key selector</param>
        /// <param name="storeId">Store ID</param>
        public virtual void DeleteSetting<T, TPropType>(T settings,
            Expression<Func<T, TPropType>> keySelector, int storeId = 0) where T : ISettings, new()
        {
            var key = settings.GetSettingKey(keySelector);

            //get cached settings
            var allSettingsByKey = GetSettingsForCachingByKey(key);

            //try get setting
            var settingForCaching = allSettingsByKey.FirstOrDefault(setting => setting.StoreId == storeId);
            if (settingForCaching != null)
            {
                //delete setting
                var setting = GetSettingById(settingForCaching.Id);
                if (setting != null)
                    DeleteSetting(setting);
            }
        }

        /// <summary>
        /// Clear cache
        /// </summary>
        public virtual void ClearCache()
        {
            _cacheManager.RemoveByPattern(SETTINGS_PATTERN_KEY);
        }

        #endregion
    }
}