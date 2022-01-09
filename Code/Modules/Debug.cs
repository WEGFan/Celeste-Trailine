using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Trailine.Modules {
    public static class Debug {

        private static TrailineSettings Settings => TrailineModule.Settings;

        public static void Load() {
            On.Celeste.Celeste.RenderCore += On_Celeste_RenderCore;
            On.Celeste.TextMenu.Render += On_TextMenu_Render;
        }

        public static void Unload() {
            On.Celeste.Celeste.RenderCore -= On_Celeste_RenderCore;
            On.Celeste.TextMenu.Render -= On_TextMenu_Render;
        }

        private static void On_Celeste_RenderCore(On.Celeste.Celeste.orig_RenderCore orig, Celeste self) {
            orig(self);
            if (!Settings.Debug.RenderTrailManagerBuffer || Engine.Scene?.Tracker?.IsEntityTracked<TrailineTrailManager>() != true) {
                return;
            }
            HiresRenderer.BeginRender();
            TrailineTrailManager.DebugRender();
            HiresRenderer.EndRender();
        }

        private static void On_TextMenu_Render(On.Celeste.TextMenu.orig_Render orig, TextMenu self) {
            orig(self);
            if (!Settings.Debug.ShowTextMenuBorders) {
                return;
            }
            Vector2 position = self.Position - self.Justify * new Vector2(self.Width, self.Height);
            for (int i = 0; i < self.Items.Count; i++) {
                TextMenu.Item item = self.Items[i];
                if (item.Visible) {
                    float width = self.Width;
                    float height = item.Height();
                    Color color = Color.Red;
                    if (item.Disabled || !item.Selectable) {
                        color = Color.DarkRed;
                    } else if (self.Current == item) {
                        color = Color.Green;
                    }
                    if (position.Y + height * 0.5f > 0f && position.Y - height * 0.5f < Engine.Height) {
                        Draw.HollowRect(position, width, height, color);
                        Draw.TextJustified(Draw.DefaultFont, i.ToString(), position - new Vector2(10f, 0f), color, 1f, new Vector2(1f, 0));
                    }
                    position.Y += height + self.ItemSpacing;
                }
            }
        }

    }
}
