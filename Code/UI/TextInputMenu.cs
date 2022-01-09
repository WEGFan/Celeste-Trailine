using System;
using Celeste.Mod.Core;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Trailine.UI {
    public class TextInputMenu : Entity {

        private readonly Color unselectColor = Color.LightGray;
        private readonly Color selectColorA = Calc.HexToColor("84FF54");
        private readonly Color selectColorB = Calc.HexToColor("FCFF59");
        private readonly Color disableColor = Color.DarkSlateBlue;

        public bool Finished;
        public bool Focused;
        public string StartingValue;
        private string value;
        public int MinValueLength;
        public int MaxValueLength;
        private string[] letters;
        private int index = 0;
        private int line = 0;
        private float widestLetter;
        private float widestLine;
        private int widestLineCount;
        private bool selectingOptions = true;
        private int optionsIndex;
        private float lineHeight;
        private float lineSpacing;
        private float boxPadding;
        private float optionsScale;
        private string cancel;
        private string space;
        private string backspace;
        private string accept;
        private float cancelWidth;
        private float spaceWidth;
        private float backspaceWidth;
        private float beginWidth;
        private float optionsWidth;
        private float boxWidth;
        private float boxHeight;
        private float pressedTimer;
        private float timer;
        private Wiggler wiggler;
        private bool consumedButton;

        public event Action<string> OnValueChange;

        public event Action<bool> OnExit;

        public string Value {
            get => value;
            set {
                this.value = value;
                OnValueChange?.Invoke(value);
            }
        }

        public string LetterChars { get; set; }

        public bool UseKeyboardInput {
            get {
                CoreModuleSettings settings = CoreModule.Instance._Settings as CoreModuleSettings;
                return settings?.UseKeyboardForTextInput ?? false;
            }
        }

        private Vector2 BoxTopLeft => Position + new Vector2((1920f - boxWidth) / 2f, 360f + (680f - boxHeight) / 2f);

        public TextInputMenu(string value, Action<string> onValueChange, int minValueLength = 1, int maxValueLength = 12) {
            wiggler = Wiggler.Create(0.25f, 4f);
            Visible = false;
            Tag = Tags.HUD | Tags.PauseUpdate;

            this.value = StartingValue = value;
            OnValueChange = onValueChange;

            MinValueLength = minValueLength;
            MaxValueLength = maxValueLength;

            LetterChars = string.Join("\n",
                "ABCDEFGHI abcdefghi",
                "JKLMNOPQR jklmnopqr",
                "STUVWXYZ  stuvwxyz",
                "1234567890+=:~!@$%",
                "^&*_-#\"'()<>/\\.,|`"
            );
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            TextInput.OnInput += OnTextInput;

            if (UseKeyboardInput) {
                Engine.Commands.Enabled = false;
                MInput.Disabled = true;
            }

            selectingOptions = false;
            optionsIndex = 0;
            index = 0;
            line = 0;

            letters = LetterChars.Split('\n');

            foreach (char c in LetterChars) {
                float width = ActiveFont.Measure(c).X;
                if (width > widestLetter) {
                    widestLetter = width;
                }
            }

            widestLineCount = 0;
            foreach (string letter in letters) {
                if (letter.Length > widestLineCount) {
                    widestLineCount = letter.Length;
                }
            }

            widestLine = widestLineCount * widestLetter;
            lineHeight = ActiveFont.LineHeight;
            lineSpacing = ActiveFont.LineHeight * 0.1f;
            boxPadding = widestLetter;
            optionsScale = 0.75f;
            cancel = Dialog.Clean("name_back");
            space = Dialog.Clean("name_space");
            backspace = Dialog.Clean("name_backspace");
            accept = Dialog.Clean("name_accept");
            cancelWidth = ActiveFont.Measure(cancel).X * optionsScale;
            spaceWidth = ActiveFont.Measure(space).X * optionsScale;
            backspaceWidth = ActiveFont.Measure(backspace).X * optionsScale;
            beginWidth = ActiveFont.Measure(accept).X * optionsScale;
            optionsWidth = cancelWidth + spaceWidth + backspaceWidth + beginWidth + widestLetter * 3f;
            boxWidth = Math.Max(widestLine, optionsWidth) + boxPadding * 2f;
            boxHeight = (letters.Length + 1f) * lineHeight + letters.Length * lineSpacing + boxPadding * 3f;

            Visible = true;
            Focused = true;

            wiggler.Start();
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            TextInput.OnInput -= OnTextInput;

            Engine.Commands.Enabled = Celeste.PlayMode == Celeste.PlayModes.Debug;
            MInput.Disabled = false;
            Focused = false;
            Visible = false;
        }

        public void OnTextInput(char c) {
            if (!UseKeyboardInput) {
                return;
            }

            bool isValidButton = true;
            if (c == '\r') {
                // enter
                Finish();
            } else if (c == '\b') {
                // backspace
                Backspace();
            } else if (c == (char)22) {
                // paste
                string value = Value + TextInput.GetClipboardText();
                if (value.Length > MaxValueLength) {
                    value = value.Substring(0, MaxValueLength);
                }
                Value = value;
            } else if (c == ' ') {
                if (Value.Length < MaxValueLength) {
                    Audio.Play(SFX.ui_main_rename_entry_space);
                    Value += c;
                } else {
                    Audio.Play(SFX.ui_main_button_invalid);
                    isValidButton = false;
                }
            } else if (!char.IsControl(c)) {
                if (Value.Length < MaxValueLength && ActiveFont.FontSize.Characters.ContainsKey(c)) {
                    Audio.Play(SFX.ui_main_rename_entry_char);
                    Value += c;
                } else {
                    Audio.Play(SFX.ui_main_button_invalid);
                    isValidButton = false;
                }
            }

            if (isValidButton) {
                consumedButton = true;
                MInput.Disabled = true;
                MInput.UpdateNull();
            } else {
                Audio.Play(SFX.ui_main_button_invalid);
            }
        }

        public override void SceneEnd(Scene scene) {
            base.SceneEnd(scene);
            TextInput.OnInput -= OnTextInput;
            Engine.Commands.Enabled = Celeste.PlayMode == Celeste.PlayModes.Debug;
            MInput.Disabled = false;
        }

        public override void Update() {
            if (UseKeyboardInput) {
                MInput.Disabled = consumedButton;
            }
            consumedButton = false;

            bool wasFocused = Focused;

            // only "focus" if we're not using the keyboard for input
            Focused = wasFocused && !UseKeyboardInput;

            base.Update();

            if (Focused) {
                if (Input.MenuRight.Pressed && (optionsIndex < 3 || !selectingOptions) && (Value.Length > 0 || !selectingOptions)) {
                    if (selectingOptions) {
                        optionsIndex = Math.Min(optionsIndex + 1, 3);
                    } else {
                        do {
                            index = (index + 1) % letters[line].Length;
                        } while (letters[line][index] == ' ');
                    }
                    wiggler.Start();
                    Audio.Play(SFX.ui_main_rename_entry_roll);
                } else if (Input.MenuLeft.Pressed && (optionsIndex > 0 || !selectingOptions)) {
                    if (selectingOptions) {
                        optionsIndex = Math.Max(optionsIndex - 1, 0);
                    } else {
                        do {
                            index = (index + letters[line].Length - 1) % letters[line].Length;
                        } while (letters[line][index] == ' ');
                    }
                    wiggler.Start();
                    Audio.Play(SFX.ui_main_rename_entry_roll);
                } else if (Input.MenuDown.Pressed && !selectingOptions) {
                    int lineNext = line + 1;
                    bool something = true;
                    for (; lineNext < letters.Length; lineNext++) {
                        if (index < letters[lineNext].Length && letters[lineNext][index] != ' ') {
                            something = false;
                            break;
                        }
                    }

                    if (something) {
                        selectingOptions = true;
                    } else {
                        line = lineNext;
                    }

                    if (selectingOptions) {
                        float pos = index * widestLetter;
                        float offs = boxWidth - boxPadding * 2f;
                        if (Value.Length == 0 || pos < cancelWidth + (offs - cancelWidth - beginWidth - backspaceWidth - spaceWidth - widestLetter * 3f) / 2f) {
                            optionsIndex = 0;
                        } else if (pos < offs - beginWidth - backspaceWidth - widestLetter * 2f) {
                            optionsIndex = 1;
                        } else if (pos < offs - beginWidth - widestLetter) {
                            optionsIndex = 2;
                        } else {
                            optionsIndex = 3;
                        }
                    }

                    wiggler.Start();
                    Audio.Play(SFX.ui_main_rename_entry_roll);
                } else if ((Input.MenuUp.Pressed || (selectingOptions && Value.Length <= 0 && optionsIndex > 0)) && (line > 0 || selectingOptions)) {
                    if (selectingOptions) {
                        line = letters.Length;
                        selectingOptions = false;
                        float offs = boxWidth - boxPadding * 2f;
                        index = optionsIndex switch {
                            0 => (int)(cancelWidth / 2f / widestLetter),
                            1 => (int)((offs - beginWidth - backspaceWidth - spaceWidth / 2f - widestLetter * 2f) / widestLetter),
                            2 => (int)((offs - beginWidth - backspaceWidth / 2f - widestLetter) / widestLetter),
                            3 => (int)((offs - beginWidth / 2f) / widestLetter),
                            _ => index
                        };
                    }
                    do {
                        line--;
                    } while (line > 0 && (index >= letters[line].Length || letters[line][index] == ' '));
                    while (index >= letters[line].Length || letters[line][index] == ' ') {
                        index--;
                    }
                    wiggler.Start();
                    Audio.Play(SFX.ui_main_rename_entry_roll);
                } else if (Input.MenuConfirm.Pressed) {
                    if (selectingOptions) {
                        if (optionsIndex == 0) {
                            Cancel();
                        } else if (optionsIndex == 1 && Value.Length > 0) {
                            Space();
                        } else if (optionsIndex == 2) {
                            Backspace();
                        } else if (optionsIndex == 3) {
                            Finish();
                        }
                    } else if (Value.Length < MaxValueLength) {
                        Value += letters[line][index].ToString();
                        wiggler.Start();
                        Audio.Play(SFX.ui_main_rename_entry_char);
                    } else {
                        Audio.Play(SFX.ui_main_button_invalid);
                    }
                } else if (Input.MenuCancel.Pressed) {
                    if (Value.Length > 0) {
                        Backspace();
                    } else {
                        Cancel();
                    }
                } else if (Input.Pause.Pressed) {
                    Input.Pause.ConsumeBuffer();
                    Finish();
                }
            }

            if (wasFocused && !Focused) {
                if (Input.ESC) {
                    Cancel();
                    wasFocused = false;
                }
            }

            Focused = wasFocused;

            pressedTimer -= Engine.DeltaTime;
            timer += Engine.DeltaTime;
            wiggler.Update();
        }

        private void Space() {
            if (Value.Length < MaxValueLength) {
                Value += " ";
                wiggler.Start();
                Audio.Play(SFX.ui_main_rename_entry_char);
            } else {
                Audio.Play(SFX.ui_main_button_invalid);
            }
        }

        private void Backspace() {
            if (Value.Length > 0) {
                Value = Value.Substring(0, Value.Length - 1);
                Audio.Play(SFX.ui_main_rename_entry_backspace);
            } else {
                Audio.Play(SFX.ui_main_button_invalid);
            }
        }

        private void Finish() {
            if (Value.Length >= MinValueLength) {
                Focused = false;
                TextInput.OnInput -= OnTextInput;
                Engine.Scene.OnEndOfFrame += () => {
                    OnExit?.Invoke(true);
                    OnExit = null;
                };
                Audio.Play(SFX.ui_main_rename_entry_accept);
            } else {
                Audio.Play(SFX.ui_main_button_invalid);
            }
        }

        private void Cancel() {
            Value = StartingValue;
            Focused = false;
            TextInput.OnInput -= OnTextInput;
            Engine.Scene.OnEndOfFrame += () => {
                OnExit?.Invoke(false);
                OnExit = null;
            };
            Audio.Play(SFX.ui_main_button_back);
        }

        public override void Render() {
            int prevIndex = index;
            if (UseKeyboardInput) {
                index = -1;
            }

            Vector2 pos = BoxTopLeft + new Vector2(boxPadding, boxPadding);

            int letterIndex = 0;
            foreach (string letter in letters) {
                for (int i = 0; i < letter.Length; i++) {
                    bool selected = letterIndex == line && i == index && !selectingOptions;
                    Vector2 scale = Vector2.One * (selected ? 1.2f : 1f);
                    Vector2 posLetter = pos + new Vector2(widestLetter, lineHeight) / 2f;
                    if (selected) {
                        posLetter += new Vector2(0f, wiggler.Value) * 8f;
                    }
                    DrawOptionText(letter[i].ToString(), posLetter, new Vector2(0.5f, 0.5f), scale, selected);
                    pos.X += widestLetter;
                }
                pos.X = BoxTopLeft.X + boxPadding;
                pos.Y += lineHeight + lineSpacing;
                letterIndex++;
            }
            float wiggle = wiggler.Value * 8f;

            pos.Y = BoxTopLeft.Y + boxHeight - lineHeight - boxPadding;
            Draw.Rect(pos.X, pos.Y - boxPadding * 0.5f, boxWidth - boxPadding * 2f, 4f, Color.White);

            DrawOptionText(cancel, pos + new Vector2(0f, lineHeight + (selectingOptions && optionsIndex == 0 ? wiggle : 0f)), new Vector2(0f, 1f), Vector2.One * optionsScale, selectingOptions && optionsIndex == 0);
            pos.X = BoxTopLeft.X + boxWidth - backspaceWidth - widestLetter - spaceWidth - widestLetter - beginWidth - boxPadding;

            DrawOptionText(space, pos + new Vector2(0f, lineHeight + (selectingOptions && optionsIndex == 1 ? wiggle : 0f)), new Vector2(0f, 1f), Vector2.One * optionsScale, selectingOptions && optionsIndex == 1, Value.Length == 0 || !Focused);
            pos.X += spaceWidth + widestLetter;

            DrawOptionText(backspace, pos + new Vector2(0f, lineHeight + (selectingOptions && optionsIndex == 2 ? wiggle : 0f)), new Vector2(0f, 1f), Vector2.One * optionsScale, selectingOptions && optionsIndex == 2, Value.Length <= 0 || !Focused);
            pos.X += backspaceWidth + widestLetter;

            DrawOptionText(accept, pos + new Vector2(0f, lineHeight + (selectingOptions && optionsIndex == 3 ? wiggle : 0f)), new Vector2(0f, 1f), Vector2.One * optionsScale, selectingOptions && optionsIndex == 3, Value.Length < 1 || !Focused);

            ActiveFont.DrawEdgeOutline(Value, Position + new Vector2(960f, 256f), new Vector2(0.5f, 0.5f), Vector2.One * 2f, Color.Gray, 4f, Color.DarkSlateBlue, 2f, Color.Black);

            index = prevIndex;
        }

        private void DrawOptionText(string text, Vector2 at, Vector2 justify, Vector2 scale, bool selected, bool disabled = false) {
            if (UseKeyboardInput) {
                selected = false;
                disabled = true;
            }

            Color color = disabled ? disableColor : GetTextColor(selected);
            Color edgeColor = disabled ? Color.Lerp(disableColor, Color.Black, 0.7f) : Color.Gray;
            if (selected && pressedTimer > 0f) {
                ActiveFont.Draw(text, at + Vector2.UnitY, justify, scale, color);
            } else {
                ActiveFont.DrawEdgeOutline(text, at, justify, scale, color, 4f, edgeColor);
            }
        }

        private Color GetTextColor(bool selected) {
            if (selected) {
                return Calc.BetweenInterval(timer, 0.1f) ? selectColorA : selectColorB;
            }
            return unselectColor;
        }

    }
}
