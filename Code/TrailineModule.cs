using System;

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

            Loaded = true;
        }

        public override void Unload() {
            if (!Loaded) {
                return;
            }

            Loaded = false;
        }

    }
}
