using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Trailine.UI {
    /// <summary>
    /// A slider optimized for large integer ranges.<br />
    /// Modified from <see cref="TextMenuExt.IntSlider" /> and added more customizable settings.
    /// </summary>
    public class FastMoveSlider : TextMenu.Item {

        public string Label;
        public int Index;
        public Action<int> OnValueChange;
        public Func<float, int> GetIndexIncrementFunc;
        public Func<float> RightWidthFunc;

        private float sine;
        private int lastDir;
        private int min;
        private int max;
        private float fastMoveTimer;
        private Func<int, string> values;

        public FastMoveSlider(string label, Func<int, string> values, int min, int max, int value = 0) {
            Label = label;
            Selectable = true;
            this.min = min;
            this.max = max;
            this.values = values;
            Index = value < min ? min : value > max ? max : value;
            GetIndexIncrementFunc = DefaultGetIndexIncrementFunc;
            RightWidthFunc = DefaultRightWidthFunc;
        }

        public FastMoveSlider Change(Action<int> action) {
            OnValueChange = action;
            return this;
        }

        private int GetIndexIncrement(float time) {
            return (GetIndexIncrementFunc ?? DefaultGetIndexIncrementFunc)(time);
        }

        private int DefaultGetIndexIncrementFunc(float time) {
            return time switch {
                < 1f => 1,
                < 3f => 5,
                < 5f => 10,
                < 7f => 25,
                < 9f => 50,
                _ => 100
            };
        }

        private float DefaultRightWidthFunc() {
            // Measure Index in case it is externally set outside the bounds
            float width = Calc.Max(0f, ActiveFont.Measure(values(max)).X, ActiveFont.Measure(values(min)).X, ActiveFont.Measure(values(Index)).X);
            return width + 120f;
        }

        public override void Added() {
            Container.InnerContent = TextMenu.InnerContentMode.TwoColumn;
        }

        public override void LeftPressed() {
            if (Input.MenuLeft.Repeating) {
                fastMoveTimer += Engine.RawDeltaTime * 8;
            } else {
                fastMoveTimer = 0;
            }

            if (Index > min) {
                Audio.Play("event:/ui/main/button_toggle_off");
                Index -= GetIndexIncrement(fastMoveTimer);
                Index = Math.Max(min, Index); // ensure we stay within bounds
                lastDir = -1;
                ValueWiggler.Start();
                OnValueChange?.Invoke(Index);
            }
        }

        public override void RightPressed() {
            if (Input.MenuRight.Repeating) {
                fastMoveTimer += Engine.RawDeltaTime * 8;
            } else {
                fastMoveTimer = 0;
            }

            if (Index < max) {
                Audio.Play("event:/ui/main/button_toggle_on");
                Index += GetIndexIncrement(fastMoveTimer);
                Index = Math.Min(max, Index); // ensure we stay within bounds
                lastDir = 1;
                ValueWiggler.Start();
                OnValueChange?.Invoke(Index);
            }
        }

        public override void ConfirmPressed() {
            if (max - min == 1) {
                if (Index == min) {
                    Audio.Play("event:/ui/main/button_toggle_on");
                } else {
                    Audio.Play("event:/ui/main/button_toggle_off");
                }
                lastDir = Index == min ? 1 : -1;
                Index = Index == min ? max : min;
                ValueWiggler.Start();
                OnValueChange?.Invoke(Index);
            }
        }

        public override void Update() {
            sine += Engine.RawDeltaTime;
        }

        public override float LeftWidth() {
            return ActiveFont.Measure(Label).X + 32f;
        }

        public override float RightWidth() {
            return (RightWidthFunc ?? DefaultRightWidthFunc)();
        }

        public override float Height() {
            return ActiveFont.LineHeight;
        }

        public override void Render(Vector2 position, bool highlighted) {
            float alpha = Container.Alpha;
            Color strokeColor = Color.Black * (alpha * alpha * alpha);
            Color color = Disabled ? Color.DarkSlateGray : (highlighted ? Container.HighlightColor : Color.White) * alpha;
            ActiveFont.DrawOutline(Label, position, new Vector2(0f, 0.5f), Vector2.One, color, 2f, strokeColor);
            if (max - min > 0) {
                float rWidth = RightWidth();
                ActiveFont.DrawOutline(values(Index), position + new Vector2(Container.Width - rWidth * 0.5f + lastDir * ValueWiggler.Value * 8f, 0f), new Vector2(0.5f, 0.5f), Vector2.One * 0.8f, color, 2f, strokeColor);

                Vector2 vector = Vector2.UnitX * (float)(highlighted ? Math.Sin(sine * 4f) * 4f : 0f);

                Vector2 position2 = position + new Vector2(Container.Width - rWidth + 40f + (lastDir < 0 ? -ValueWiggler.Value * 8f : 0f), 0f) - (Index > min ? vector : Vector2.Zero);
                ActiveFont.DrawOutline("<", position2, new Vector2(0.5f, 0.5f), Vector2.One, Index > min ? color : Color.DarkSlateGray * alpha, 2f, strokeColor);

                position2 = position + new Vector2(Container.Width - 40f + (lastDir > 0 ? ValueWiggler.Value * 8f : 0f), 0f) + (Index < max ? vector : Vector2.Zero);
                ActiveFont.DrawOutline(">", position2, new Vector2(0.5f, 0.5f), Vector2.One, Index < max ? color : Color.DarkSlateGray * alpha, 2f, strokeColor);
            }
        }

    }
}
