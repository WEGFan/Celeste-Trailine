using MonoMod.Utils;

namespace Celeste.Mod.Trailine.Utils {
    /// <summary>
    /// Dynamic data wrapper for <see cref="T:Celeste.Mod.EverestModule"/>
    /// </summary>
    public class DDW_EverestModule {

        public DynamicData DynamicData { get; }

        public EverestModule Object => DynamicData.Target as EverestModule;

        public DDW_EverestModule(EverestModule instance) {
            DynamicData = new DynamicData(typeof(EverestModule), instance);
        }

        public int ForceSaveDataFlush {
            get => DynamicData.Get<int>(nameof(ForceSaveDataFlush));
            set => DynamicData.Set(nameof(ForceSaveDataFlush), value);
        }

    }
}
