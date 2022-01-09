using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Trailine.UI {
    public class PatternPreview : TextMenu.Item {

        public TrailPattern Pattern { get; set; }

        private readonly VirtualTexture previewTexture;
        private readonly MTexture indicator;
        private BeforeRenderHook beforeRenderHook;

        private const int GradientBarWidth = 1000;
        private const int GradientBarHeight = 20;

        public PatternPreview(TrailPattern pattern) {
            Pattern = pattern;
            previewTexture = VirtualContent.CreateTexture("pattern-preview", GradientBarWidth, 1, Color.White);
            indicator = GFX.Gui["Trailine/patternPreview/indicator"];
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
            return 80f;
        }

        public void BeforeRender() {
            Color[] data = new Color[GradientBarWidth];
            for (int i = 0; i < GradientBarWidth; i++) {
                data[i] = Pattern.ColorAtTime(Pattern.Duration * i / GradientBarWidth);
            }
            previewTexture.Texture.SetData(data);
        }

        public override void Render(Vector2 position, bool highlighted) {
            float menuCenterX = position.X + Container.Width / 2;
            new MTexture(previewTexture).DrawJustified(new Vector2(menuCenterX, position.Y), new Vector2(0.5f, 0f),
                Color.White, new Vector2(1f, GradientBarHeight));

            Vector2 left = new Vector2(menuCenterX, position.Y) - new Vector2(GradientBarWidth / 2f, 0f);

            if (Pattern.ColorStops.Count == 1) {
                indicator.DrawJustified(left, new Vector2(0.5f, 1f), Pattern.ColorStops[0].Color, 1f);
                indicator.DrawJustified(left + new Vector2(GradientBarWidth, 0), new Vector2(0.5f, 1f), Pattern.ColorStops[0].Color, 1f);
            } else {
                List<float> weightAccumulated = new List<float>(Pattern.ColorStops.Count) {0};
                float currentWeight = 0;
                for (int i = 1; i < Pattern.ColorStops.Count; i++) {
                    currentWeight += Pattern.ColorStops[i].Width;
                    weightAccumulated.Add(currentWeight);
                }
                float weightSum = weightAccumulated.Last();
                List<float> offsets = weightAccumulated.Select(i => GradientBarWidth * (i / weightSum)).ToList();
                for (int i = 0; i < offsets.Count; i++) {
                    indicator.DrawJustified(left + new Vector2(offsets[i], 0), new Vector2(0.5f, 1f), Pattern.ColorStops[i].Color, 1f);
                }
            }
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
