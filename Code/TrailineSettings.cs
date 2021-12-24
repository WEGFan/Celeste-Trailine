using Celeste.Mod.Trailine.Modules;

namespace Celeste.Mod.Trailine {
    public class TrailineSettings : EverestModuleSettings {

        public bool Enabled { get; set; } = true;

        [SettingIgnore]
        public int SettingsVersion { get; set; } = 1;

#if !DEBUG
        [SettingIgnore]
        [YamlIgnore]
#endif
        public bool RenderTrailManagerBuffer { get; set; } = false;

#if !DEBUG
        [SettingIgnore]
        [YamlIgnore]
#endif
        public bool ShowTextMenuBorders { get; set; } = false;

        public TrailTypes TrailType { get; set; } = TrailTypes.HairColor;

        public int TrailFrequency { get; set; } = 10;

        public int TrailDuration { get; set; } = 200;

        public int TrailOpacity { get; set; } = 16;

        public bool HideOriginalDashTrails { get; set; } = true;

        public void CreateEnabledEntry(TextMenu textMenu, bool inGame) {
            TextMenu.Item item = new TextMenu.OnOff("Enabled", Enabled)
                .Change(value => {
                    Enabled = value;
                    if (value) {
                        TrailineModule.Instance.Load();
                    } else {
                        TrailineModule.Instance.Unload();
                    }
                });
            textMenu.Add(item);
        }

        public void CreateTrailTypeEntry(TextMenu textMenu, bool inGame) {
            Dictionary<TrailTypes, string> optionLabels = new Dictionary<TrailTypes, string> {
                {TrailTypes.HairColor, "Hair Color"},
                {TrailTypes.Pattern, "Pattern"},
                {TrailTypes.OnionSkin, "Onion Skin"},
            };
            TextMenu.Item item = new TextMenuExt.EnumerableSlider<TrailTypes>("Trail Type", optionLabels, TrailType)
                .Change(value => {
                    TrailType = value;
                    TrailineTrailManager.Clear();
                });
            textMenu.Add(item);
        }

        public void CreateTrailFrequencyEntry(TextMenu textMenu, bool inGame) {
            TextMenu.Item item = new TextMenu.Slider("Trail Frequency", i => $"{i / 100.0:F2}s",
                    1, 100, TrailFrequency)
                .Change(value => {
                    TrailFrequency = value;
                });
            textMenu.Add(item);
        }

        public void CreateTrailDurationEntry(TextMenu textMenu, bool inGame) {
            TextMenu.Item item = new TextMenu.Slider("Trail Duration", i => $"{i / 100.0:F2}s",
                    0, 6000, TrailDuration)
                .Change(value => {
                    TrailDuration = value;
                });
            textMenu.Add(item);
        }

        public void CreateTrailOpacityEntry(TextMenu textMenu, bool inGame) {
            TextMenu.Item item = new TextMenu.Slider("Trail Opacity", i => $"{i * 5}%",
                    0, 20, TrailOpacity)
                .Change(value => {
                    TrailOpacity = value;
                });
            textMenu.Add(item);
        }

        public enum TrailTypes {

            HairColor,
            Pattern,
            OnionSkin

        }

    }
}
