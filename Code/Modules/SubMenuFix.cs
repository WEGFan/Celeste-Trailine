using System;
using System.Collections.Generic;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.Trailine.Modules {
    /// <summary>
    /// Include submenu fixes from Everest 3197 temporarily until it becomes stable
    /// https://github.com/EverestAPI/Everest/commit/82e785e
    /// </summary>
    public static class SubMenuFix {

        public static void Load() {
            if (Everest.Version == new Version(0, 0, 0) || Everest.Version >= new Version(1, 3197, 0)) {
                return;
            }
            using (new DetourContext {After = new List<string> {"*"}}) {
                On.Celeste.TextMenuExt.SubMenu.GetYOffsetOf += SubMenuOnGetYOffsetOf;
                On.Celeste.TextMenuExt.SubMenu.Exit += SubMenuOnExit;
            }
        }

        public static void Unload() {
            On.Celeste.TextMenuExt.SubMenu.GetYOffsetOf -= SubMenuOnGetYOffsetOf;
            On.Celeste.TextMenuExt.SubMenu.Exit -= SubMenuOnExit;
        }

        private static float SubMenuOnGetYOffsetOf(On.Celeste.TextMenuExt.SubMenu.orig_GetYOffsetOf orig, TextMenuExt.SubMenu self, TextMenu.Item item) {
            float offset = self.Container.GetYOffsetOf(self) - self.Height() * 0.5f;
            if (item == null) {
                // common case is all items in submenu are disabled when item is null
                return offset + self.TitleHeight * 0.5f;
            }
            offset += self.TitleHeight;
            foreach (TextMenu.Item child in self.Items) {
                if (child.Visible) {
                    offset += child.Height() + self.ItemSpacing;
                }
                if (child == item) {
                    break;
                }
            }
            return offset - item.Height() * 0.5f - self.ItemSpacing;
        }

        private static void SubMenuOnExit(On.Celeste.TextMenuExt.SubMenu.orig_Exit orig, TextMenuExt.SubMenu self) {
            self.Current?.OnLeave?.Invoke();
            self.Focused = false;
            if (!Input.MenuUp.Repeating && !Input.MenuDown.Repeating) {
                Audio.Play(SFX.ui_main_button_back);
            }
            self.Container.AutoScroll = new DynData<TextMenuExt.SubMenu>(self).Get<bool>("containerAutoScroll");
            self.Container.Focused = true;
        }

    }
}
