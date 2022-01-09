using System;
using Celeste.Mod.Trailine.Utils;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Trailine.UI {
    public class TextInputButton : TextMenu.Item {

        public bool AlwaysCenter;
        public string Label;
        public int MinValueLength;
        public int MaxValueLength;
        public string Value;
        public string LetterChars;

        public Action<string> OnValueChange;
        public Action<bool> OnMenuExit;

        public string ButtonLabel => $"{Label}: {Value}";

        public TextInputButton(string label, string value, int minValueLength = 1, int maxValueLength = 12) {
            Label = label;
            Value = value;
            MinValueLength = Math.Max(minValueLength, 1);
            MaxValueLength = Math.Max(maxValueLength, 1);
            Selectable = true;
            LetterChars = string.Join("\n",
                "ABCDEFGHI abcdefghi",
                "JKLMNOPQR jklmnopqr",
                "STUVWXYZ  stuvwxyz",
                "1234567890+=:~!@$%",
                "^&*_-#\"'()<>/\\.,|`"
            );
        }

        public TextInputButton Change(Action<string> action) {
            OnValueChange = action;
            return this;
        }

        public TextInputButton MenuExit(Action<bool> action) {
            OnMenuExit = action;
            return this;
        }

        public override void ConfirmPressed() {
            Audio.Play(SFX.ui_main_button_select);

            TextInputMenu textInputMenu = new TextInputMenu(Value, OnValueChange, MinValueLength, MaxValueLength) {
                LetterChars = LetterChars
            };
            switch (Container.Scene) {
                case Overworld overworld: {
                    textInputMenu.OnExit += confirm => {
                        overworld.Goto(overworld.Last.GetType());
                    };
                    overworld.Goto<OuiTextInput>().Init(textInputMenu);
                    break;
                }
                case Level level: {
                    Container.RemoveSelf();

                    // notify the pause menu that we aren't in the main menu anymore (hides the strawberry tracker)
                    bool comesFromPauseMainMenu = level.PauseMainMenuOpen;
                    level.PauseMainMenuOpen = false;

                    textInputMenu.OnExit += confirm => {
                        textInputMenu.RemoveSelf();
                        level.Add(Container);
                        level.PauseMainMenuOpen = comesFromPauseMainMenu;
                    };

                    // add the menu to the scene
                    level.Add(textInputMenu);
                    break;
                }
            }
            textInputMenu.OnExit += OnMenuExit;
        }

        public override float LeftWidth() {
            return ActiveFont.Measure(ButtonLabel).X;
        }

        public override float Height() {
            return ActiveFont.LineHeight;
        }

        public override void Render(Vector2 position, bool highlighted) {
            float alpha = Container.Alpha;
            Color color = Disabled ? Color.DarkSlateGray : (highlighted ? Container.HighlightColor : Color.White) * alpha;
            Color strokeColor = Color.Black * (alpha * alpha * alpha);
            bool center = Container.InnerContent == TextMenu.InnerContentMode.TwoColumn && !AlwaysCenter;
            Vector2 position2 = position + (center ? Vector2.Zero : new Vector2(Container.Width * 0.5f, 0f));
            Vector2 justify = center && !AlwaysCenter ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f);
            ActiveFont.DrawOutline(ButtonLabel, position2, justify, Vector2.One, color, 2f, strokeColor);
        }

    }
}
