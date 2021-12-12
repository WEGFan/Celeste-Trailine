using Monocle;

namespace Celeste.Mod.Trailine.Modules {
    public static class Debug {

        private static TrailineSettings Settings => TrailineModule.Settings;

        public static void Load() {
            On.Celeste.Celeste.RenderCore += On_Celeste_RenderCore;
        }

        public static void Unload() {
            On.Celeste.Celeste.RenderCore -= On_Celeste_RenderCore;
        }

        private static void On_Celeste_RenderCore(On.Celeste.Celeste.orig_RenderCore orig, Celeste self) {
            orig(self);
            if (!Settings.RenderTrailManagerBuffer || Engine.Scene?.Tracker?.IsEntityTracked<TrailineTrailManager>() != true) {
                return;
            }
            HiresRenderer.BeginRender();
            TrailineTrailManager.DebugRender();
            HiresRenderer.EndRender();
        }

    }
}
