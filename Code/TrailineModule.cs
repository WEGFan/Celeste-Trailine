using System;
using Celeste.Mod.Trailine.Modules;
using Celeste.Mod.Trailine.UI;

namespace Celeste.Mod.Trailine {
    public class TrailineModule : EverestModule {

        public TrailineModule() {
            Instance = this;
        }

        public static TrailineModule Instance { get; private set; }

        public override Type SettingsType => typeof(TrailineSettings);

        public static TrailineSettings Settings => Instance._Settings as TrailineSettings;

        public static bool Loaded = false;

        public override void Load() {
            if (Loaded || !Settings.Enabled) {
                return;
            }

            CustomTrail.Load();
            PatternPreview.Load();
            SubMenuFix.Load();
            Debug.Load();

            Loaded = true;
        }

        public override void Unload() {
            if (!Loaded) {
                return;
            }

            CustomTrail.Unload();
            PatternPreview.Unload();
            SubMenuFix.Unload();
            Debug.Unload();

            Loaded = false;
        }

        public override void SaveSettings() {
            Settings.SettingsVersion = 1;
            base.SaveSettings();
        }

    }
}
