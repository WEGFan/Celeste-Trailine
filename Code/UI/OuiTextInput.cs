using System.Collections;
using Celeste.Mod.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Trailine.UI {
    public class OuiTextInput : Oui, OuiModOptions.ISubmenu {

        private const float OnScreenX = 0f;
        private const float OffScreenX = 1920f;

        private TextInputMenu menu;
        private float alpha;

        public TextInputMenu Init(TextInputMenu menu) {
            this.menu = menu;
            return menu;
        }

        public override IEnumerator Enter(Oui from) {
            Overworld.ShowInputUI = false;
            Scene.Add(menu);

            Visible = true;
            menu.Visible = true;
            menu.Focused = false;

            for (float p = 0f; p < 1f; p += Engine.DeltaTime * 4f) {
                menu.X = OffScreenX + -1920f * Ease.CubeOut(p);
                alpha = Ease.CubeOut(p);
                yield return null;
            }
            alpha = 1f;

            menu.Focused = true;
        }

        public override IEnumerator Leave(Oui next) {
            Audio.Play(SFX.ui_main_whoosh_large_out);
            menu.Focused = false;
            Overworld.ShowInputUI = true;

            for (float p = 0f; p < 1f; p += Engine.DeltaTime * 4f) {
                menu.X = OnScreenX + 1920f * Ease.CubeIn(p);
                alpha = 1f - Ease.CubeIn(p);
                yield return null;
            }

            menu.Visible = Visible = false;
            menu.RemoveSelf();
            menu = null;
        }

        public override void Render() {
            if (alpha > 0f) {
                Draw.Rect(-10f, -10f, 1940f, 1100f, Color.Black * alpha * 0.4f);
            }
            base.Render();
        }

    }
}
