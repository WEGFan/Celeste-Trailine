using System;
using System.IO;
using Celeste.Mod.Core;
using Celeste.Mod.Trailine.Modules;
using Celeste.Mod.Trailine.UI;
using Celeste.Mod.Trailine.Utils;
using YamlDotNet.Serialization;

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
            DDW_EverestModule wrapper = new DDW_EverestModule(this);

            bool forceFlush = wrapper.ForceSaveDataFlush > 0;
            if (forceFlush) {
                wrapper.ForceSaveDataFlush--;
            }

            if (SettingsType == null || _Settings == null) {
                return;
            }

            Settings.SettingsVersion = 1;

            string path = UserIO.GetSaveFilePath("modsettings-" + Metadata.Name);
            if (File.Exists(path)) {
                File.Delete(path);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            try {
                using (FileStream stream = File.OpenWrite(path)) {
                    using (StreamWriter writer = new StreamWriter(stream)) {
                        // disable anchors in serialized yaml
                        // https://github.com/EverestAPI/Everest/pull/404
                        ISerializer serializer = new SerializerBuilder()
                            .ConfigureDefaultValuesHandling(DefaultValuesHandling.Preserve)
                            .DisableAliases()
                            .Build();
                        serializer.Serialize(writer, _Settings, SettingsType);
                        if (forceFlush || ((CoreModule.Settings.SaveDataFlush ?? true) && !MainThreadHelper.IsMainThread)) {
                            stream.Flush(true);
                        }
                    }
                }
            } catch (Exception e) {
                Logger.Log(LogLevel.Warn, "EverestModule", $"Failed to save the settings of {Metadata.Name}!");
                Logger.LogDetailed(e);
            }
        }

    }
}
