using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.Trailine.UI {
    public class PatternPreview : TextMenu.Item {

        public TrailPattern Pattern { get; set; }

        private VirtualRenderTarget buffer;
        private BeforeRenderHook beforeRenderHook;

        private const int GradientBarWidth = 1000;
        private const int GradientBarHeight = 20;

        public PatternPreview(TrailPattern pattern) {
            Pattern = pattern;
        }

        public override void Added() {
            Container.Add(beforeRenderHook = new BeforeRenderHook(BeforeRender));
        }

        public void Removed() {
            Container.Remove(beforeRenderHook);
        }

        public override float LeftWidth() {
            return 0;
        }

        public override float Height() {
            return 60f;
        }

        public void BeforeRender() {
            buffer ??= VirtualContent.CreateRenderTarget("pattern-preview", GradientBarWidth, 1);

            Color[] data = new Color[GradientBarWidth];
            for (int i = 0; i < GradientBarWidth; i++) {
                data[i] = Pattern.ColorAtTime(Pattern.Duration * i / GradientBarWidth);
            }
            buffer.Target.SetData(data);
        }

        public override void Render(Vector2 position, bool highlighted) {
            float menuCenter = position.X + Container.Width / 2;

            Draw.SpriteBatch.Draw(buffer.Target, new Vector2(menuCenter, position.Y), null, Color.White, 0f,
                new Vector2(GradientBarWidth / 2f, 0.5f), new Vector2(1f, GradientBarHeight), SpriteEffects.None, 0f);
        }

        public static void Load() {
            On.Celeste.TextMenu.Remove += On_TextMenu_Remove;
        }

        public static void Unload() {
            On.Celeste.TextMenu.Remove -= On_TextMenu_Remove;
        }

        private static TextMenu On_TextMenu_Remove(On.Celeste.TextMenu.orig_Remove orig, TextMenu self, TextMenu.Item item) {
            if (item is PatternPreview patternPreview) {
                patternPreview.Removed();
            }
            return orig(self, item);
        }

    }
}
