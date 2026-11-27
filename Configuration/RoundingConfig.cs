using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;

namespace Resto.Front.Api.CashRounding.Configuration
{
    public sealed class RoundingConfig
    {
        public const bool DefaultEnabled = true;
        public const string DefaultDiscountTypeName = "Arrotondamento";
        public const decimal DefaultRoundingStep = 0.05m;
        public const bool DefaultShowNotification = true;
        public const bool DefaultAutoApplyOnEnterPaymentScreen = false;

        private const string SectionGroupName = "applicationSettings";
        private const string SectionName = "Resto.Front.Api.CashRounding.Settings";

        private readonly object syncRoot = new object();

        public bool Enabled { get; set; } = DefaultEnabled;
        public Guid? DiscountTypeId { get; set; }
        public string DiscountTypeName { get; set; } = DefaultDiscountTypeName;
        public decimal RoundingStep { get; set; } = DefaultRoundingStep;
        public bool ShowNotification { get; set; } = DefaultShowNotification;
        public bool AutoApplyOnEnterPaymentScreen { get; set; } = DefaultAutoApplyOnEnterPaymentScreen;

        public static RoundingConfig Load()
        {
            var config = new RoundingConfig();
            try
            {
                var exeConfig = OpenPluginConfiguration();
                var section = GetSettingsSection(exeConfig);
                if (section == null)
                {
                    PluginContext.Log.Warn("CashRounding settings section not found; using default values.");
                    return config;
                }

                var enabledRaw = GetSettingValue(section, "Enabled");
                if (bool.TryParse(enabledRaw, out var enabled))
                    config.Enabled = enabled;

                var discountIdRaw = GetSettingValue(section, "DiscountTypeId");
                if (Guid.TryParse(discountIdRaw, out var discountId) && discountId != Guid.Empty)
                    config.DiscountTypeId = discountId;

                var discountNameRaw = GetSettingValue(section, "DiscountTypeName");
                if (!string.IsNullOrWhiteSpace(discountNameRaw))
                    config.DiscountTypeName = discountNameRaw;

                var stepRaw = GetSettingValue(section, "RoundingStep");
                if (decimal.TryParse(stepRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var step) && step > 0m)
                    config.RoundingStep = step;

                var showNotificationRaw = GetSettingValue(section, "ShowNotification");
                if (bool.TryParse(showNotificationRaw, out var showNotification))
                    config.ShowNotification = showNotification;

                var autoApplyRaw = GetSettingValue(section, "AutoApplyOnEnterPaymentScreen");
                if (bool.TryParse(autoApplyRaw, out var autoApply))
                    config.AutoApplyOnEnterPaymentScreen = autoApply;

                PluginContext.Log.Info(
                    $"CashRounding config loaded: Enabled={config.Enabled}, DiscountTypeId={config.DiscountTypeId}, " +
                    $"DiscountTypeName='{config.DiscountTypeName}', RoundingStep={config.RoundingStep.ToString("0.00", CultureInfo.InvariantCulture)}, " +
                    $"ShowNotification={config.ShowNotification}, AutoApplyOnEnterPaymentScreen={config.AutoApplyOnEnterPaymentScreen}");
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"Failed to load CashRounding configuration, using defaults: {ex.Message}");
            }

            return config;
        }

        public void Save()
        {
            lock (syncRoot)
            {
                var exeConfig = OpenPluginConfiguration();
                var section = GetSettingsSection(exeConfig);
                if (section == null)
                {
                    throw new InvalidOperationException(
                        $"Configuration section '{SectionName}' not found in '{exeConfig.FilePath}'.");
                }

                SetSettingValue(section, "Enabled", Enabled ? "true" : "false");
                SetSettingValue(section, "DiscountTypeId", DiscountTypeId.HasValue ? DiscountTypeId.Value.ToString() : string.Empty);
                SetSettingValue(section, "DiscountTypeName", DiscountTypeName?.Trim() ?? string.Empty);
                SetSettingValue(section, "RoundingStep", RoundingStep.ToString("0.00", CultureInfo.InvariantCulture));
                SetSettingValue(section, "ShowNotification", ShowNotification ? "true" : "false");
                SetSettingValue(section, "AutoApplyOnEnterPaymentScreen", AutoApplyOnEnterPaymentScreen ? "true" : "false");

                section.SectionInformation.ForceSave = true;
                exeConfig.Save(ConfigurationSaveMode.Modified);

                PluginContext.Log.Info(
                    $"CashRounding config saved: Enabled={Enabled}, DiscountTypeId={DiscountTypeId}, " +
                    $"DiscountTypeName='{DiscountTypeName}', RoundingStep={RoundingStep.ToString("0.00", CultureInfo.InvariantCulture)}, " +
                    $"ShowNotification={ShowNotification}, AutoApplyOnEnterPaymentScreen={AutoApplyOnEnterPaymentScreen}");
            }
        }

        private static System.Configuration.Configuration OpenPluginConfiguration()
        {
            var assemblyPath = Assembly.GetExecutingAssembly().Location;
            var configPath = assemblyPath + ".config";
            if (File.Exists(configPath))
            {
                var map = new ExeConfigurationFileMap { ExeConfigFilename = configPath };
                return ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
            }

            return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        }

        private static ClientSettingsSection GetSettingsSection(System.Configuration.Configuration exeConfig)
        {
            var group = exeConfig.GetSectionGroup(SectionGroupName);
            return group?.Sections[SectionName] as ClientSettingsSection;
        }

        private static string GetSettingValue(ClientSettingsSection section, string key)
        {
            var value = section.Settings.Get(key)?.Value?.ValueXml?.InnerText;
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static void SetSettingValue(ClientSettingsSection section, string key, string value)
        {
            var setting = section.Settings.Get(key);
            if (setting == null)
            {
                setting = new SettingElement(key, SettingsSerializeAs.String);
                var doc = new XmlDocument();
                var valueNode = doc.CreateElement("value");
                valueNode.InnerText = value;
                setting.Value = new SettingValueElement { ValueXml = valueNode };
                section.Settings.Add(setting);
            }
            else
            {
                setting.Value.ValueXml.InnerText = value;
            }
        }
    }
}
