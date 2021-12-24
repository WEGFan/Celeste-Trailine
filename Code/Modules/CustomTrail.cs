using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Trailine.Modules {
    public static class CustomTrail {

        private static TrailineSettings Settings => TrailineModule.Settings;

        public static void Load() {
            On.Celeste.Player.Update += On_Player_Update;
            On.Celeste.Player.CreateTrail += On_Player_CreateTrail;
            On.Celeste.TrailManager.Clear += On_TrailManager_Clear;
        }

        public static void Unload() {
            On.Celeste.Player.Update -= On_Player_Update;
            On.Celeste.Player.CreateTrail -= On_Player_CreateTrail;
            On.Celeste.TrailManager.Clear -= On_TrailManager_Clear;
        }

        private static void On_Player_Update(On.Celeste.Player.orig_Update orig, Player self) {
            orig(self);
            if (self.StateMachine.State != Player.StIntroRespawn && (self.Scene?.OnInterval(Settings.TrailFrequency / 100f) ?? false)) {
                Color color = TrailineModule.Settings.TrailType switch {
                    TrailineSettings.TrailTypes.Pattern => TrailineModule.Settings.CurrentPattern.ColorAtTime(self.Scene.TimeActive),
                    TrailineSettings.TrailTypes.HairColor => self.Hair.Color,
                    TrailineSettings.TrailTypes.OnionSkin => Color.White,
                    _ => Color.Transparent
                };
                Vector2 scale = new Vector2(Math.Abs(self.Sprite.Scale.X) * (float)self.Facing, self.Sprite.Scale.Y);
                TrailineTrailManager.Add(self, scale, color, Settings.TrailDuration / 100f);
            }
        }

        private static void On_Player_CreateTrail(On.Celeste.Player.orig_CreateTrail orig, Player self) {
            if (Settings.HideOriginalDashTrails) {
                return;
            }
            orig(self);
        }

        private static void On_TrailManager_Clear(On.Celeste.TrailManager.orig_Clear orig) {
            orig();
            TrailineTrailManager.Clear();
        }

    }
}
