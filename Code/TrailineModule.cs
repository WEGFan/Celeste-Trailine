using System;
using Celeste.Mod.Trailine.Modules;

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
            Debug.Load();

            Loaded = true;
        }

        public override void Unload() {
            if (!Loaded) {
                return;
            }

            CustomTrail.Unload();
            Debug.Unload();

            Loaded = false;
        }

        public override void SaveSettings() {
            Settings.SettingsVersion = 1;
            base.SaveSettings();
        }

    }
}
