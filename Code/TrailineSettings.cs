using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Trailine.Modules;
using Celeste.Mod.Trailine.UI;
using Celeste.Mod.Trailine.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using YamlDotNet.Serialization;

namespace Celeste.Mod.Trailine {
    public class TrailineSettings : EverestModuleSettings {

        public bool Enabled { get; set; } = true;

        [SettingIgnore]
        public int SettingsVersion { get; set; } = 1;

#if !DEBUG
        [YamlIgnore]
#endif
        public DebugSettings Debug { get; set; } = new DebugSettings();

        public TrailTypes TrailType { get; set; } = TrailTypes.HairColor;

        [YamlIgnore]
        public int TrailFrequencySliderValue { get; set; } = (int)(0.1 * 100);

        public float TrailFrequency {
            get => TrailFrequencySliderValue / 100f;
            set => TrailFrequencySliderValue = Calc.Clamp((int)Math.Round(value * 100), 1, 60 * 100);
        }

        [YamlIgnore]
        public int TrailDurationSliderValue { get; set; } = 5 * 20;

        public float TrailDuration {
            get => TrailDurationSliderValue / 20f;
            set => TrailDurationSliderValue = Calc.Clamp((int)Math.Round(value * 20), 1, 60 * 20);
        }

        [YamlIgnore]
        public int TrailOpacitySliderValue { get; set; } = (int)(0.8 * 20);

        public float TrailOpacity {
            get => TrailOpacitySliderValue / 20f;
            set => TrailOpacitySliderValue = Calc.Clamp((int)Math.Round(value * 20), 0, 1 * 20);
        }

        public bool HideOriginalDashTrails { get; set; } = true;

        public int CurrentPatternIndex { get; set; } = 0;

        [YamlIgnore]
        public TrailPattern CurrentPattern => Patterns[CurrentPatternIndex];

        public List<TrailPattern> Patterns { get; set; } = new List<TrailPattern> {
            new TrailPattern {
                ColorStops = new List<ColorStop> {
                    new ColorStop {Color = Calc.HexToColor("#ff4040")},
                    new ColorStop {Color = Calc.HexToColor("#ffff40"), Width = 1},
                    new ColorStop {Color = Calc.HexToColor("#40ff40"), Width = 1},
                    new ColorStop {Color = Calc.HexToColor("#40ffff"), Width = 1},
                    new ColorStop {Color = Calc.HexToColor("#4040ff"), Width = 1},
                    new ColorStop {Color = Calc.HexToColor("#ff40ff"), Width = 1},
                    new ColorStop {Color = Calc.HexToColor("#ff4040"), Width = 1}
                },
                Duration = 5f
            },
            new TrailPattern {
                ColorStops = new List<ColorStop> {
                    new ColorStop {Color = Player.NormalHairColor},
                    new ColorStop {Color = Player.TwoDashesHairColor, Width = 1},
                    new ColorStop {Color = Player.UsedHairColor, Width = 2},
                    new ColorStop {Color = Player.NormalBadelineHairColor, Width = 2},
                    new ColorStop {Color = Player.NormalHairColor, Width = 1}
                },
                Duration = 2f
            }
        };

        private List<TextMenu.Item> currentPatternItems = new List<TextMenu.Item>();
        private PatternPreview currentPatternPreview;

        public void CreateEnabledEntry(TextMenu textMenu, bool inGame) {
            TextMenu.Item item = new TextMenu.OnOff("Enabled", Enabled)
                .Change(value => {
                    Enabled = value;
                    if (value) {
                        TrailineModule.Instance.Load();
                    } else {
                        TrailineModule.Instance.Unload();
                    }
                });
            textMenu.Add(item);
        }

#if !DEBUG
        public void CreateDebugEntry(TextMenu textMenu, bool inGame) {
            // everest hasn't support using SettingIgnore on submenus yet, use an empty method to override default behaviour instead 
        }
#endif

        public void CreateTrailTypeEntry(TextMenu textMenu, bool inGame) {
            Dictionary<TrailTypes, string> optionLabels = new Dictionary<TrailTypes, string> {
                {TrailTypes.HairColor, "Hair Color"},
                {TrailTypes.Pattern, "Pattern"},
                {TrailTypes.OnionSkin, "Onion Skin"},
            };
            TextMenu.Item item = new TextMenuExt.EnumerableSlider<TrailTypes>("Trail Type", optionLabels, TrailType)
                .Change(value => {
                    TrailType = value;
                    TrailineTrailManager.Clear();
                });
            textMenu.Add(item);
        }

        public void CreateTrailFrequencyEntry(TextMenu textMenu, bool inGame) {
            TextMenu.Item item = new FastMoveSlider("Trail Frequency", i => $"{i / 100f:F2}s",
                    1, 1 * 100, TrailFrequencySliderValue)
                .Change(value => {
                    TrailFrequencySliderValue = value;
                });
            textMenu.Add(item);
        }

        public void CreateTrailDurationEntry(TextMenu textMenu, bool inGame) {
            TextMenu.Item item = new FastMoveSlider("Trail Duration", i => $"{i / 20f:F2}s",
                    0, 30 * 20, TrailDurationSliderValue)
                .Change(value => {
                    TrailDurationSliderValue = value;
                });
            textMenu.Add(item);
        }

        public void CreateTrailOpacityEntry(TextMenu textMenu, bool inGame) {
            TextMenu.Item item = new TextMenu.Slider("Trail Opacity", i => $"{100 * i / 20f}%",
                    0, 1 * 20, TrailOpacitySliderValue)
                .Change(value => {
                    TrailOpacitySliderValue = value;
                });
            textMenu.Add(item);
        }

        public void CreateCurrentPatternIndexEntry(TextMenu textMenu, bool inGame) {
            TextMenu.Slider currentPatternSlider;
            TextMenu.Button removePatternButton = null;
            textMenu
                .Add((currentPatternSlider = new TextMenu.Slider("Current Pattern", i => $"Pattern {i + 1}",
                        0, Patterns.Count - 1, CurrentPatternIndex))
                    .Change(value => {
                        CurrentPatternIndex = value;
                        currentPatternPreview.Pattern = CurrentPattern;
                        RecreatePatternEntries(textMenu, inGame);
                    }))
                .Add(new TextMenu.Button("Add Pattern")
                    .Pressed(() => {
                        Patterns.Add(new TrailPattern());
                        CurrentPatternIndex = Patterns.Count - 1;
                        currentPatternSlider.Add($"Pattern {CurrentPatternIndex + 1}", CurrentPatternIndex, false);
                        currentPatternSlider.Index = CurrentPatternIndex;
                        currentPatternSlider.OnValueChange(CurrentPatternIndex);
                        // ReSharper disable once AccessToModifiedClosure
                        removePatternButton!.Disabled = false;
                    }))
                .Add((removePatternButton = new TextMenu.Button("Remove Pattern") {
                        Disabled = Patterns.Count == 1
                    })
                    .Pressed(() => {
                        currentPatternSlider.Values.RemoveAt(Patterns.Count - 1);
                        Patterns.RemoveAt(CurrentPatternIndex);
                        CurrentPatternIndex = Calc.Clamp(CurrentPatternIndex, 0, Patterns.Count - 1);
                        currentPatternSlider.Index = CurrentPatternIndex;
                        currentPatternSlider.OnValueChange(currentPatternSlider.Values[currentPatternSlider.Index].Item2);
                        if (Patterns.Count == 1) {
                            removePatternButton.Disabled = true;
                            textMenu.MoveSelection(-1);
                        }
                    }));
        }

        private void RecreatePatternEntries(TextMenu textMenu, bool inGame) {
            int firstIndex = textMenu.Items.IndexOf(currentPatternItems[0]);
            currentPatternItems.ForEach(item => textMenu.Remove(item));
            CreatePatternEntries(textMenu, inGame);
            foreach (TextMenu.Item item in Enumerable.Reverse(currentPatternItems)) {
                textMenu.Insert(firstIndex, item);
            }
        }

        public void CreatePatternEntries(TextMenu textMenu, bool inGame) {
            currentPatternItems.Clear();
            // TODO: refactor this huge mess code
            currentPatternItems.Add(currentPatternPreview = new PatternPreview(CurrentPattern));
            currentPatternItems.Add(new FastMoveSlider("Duration", i => $"{i / 20f:F2}s", 1, 60 * 20, CurrentPattern.DurationSliderValue)
                .Change(value => {
                    CurrentPattern.DurationSliderValue = value;
                }));
            for (int i = 0; i < CurrentPattern.ColorStops.Count; i++) {
                int index = i;

                ColorStop colorStop = CurrentPattern.ColorStops[i];
                TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu($"Color {i + 1}: {ColorUtils.ColorToHex(colorStop.Color)}", false);

                subMenu.Add(new TextInputButton("Color", ColorUtils.ColorToHex(colorStop.Color).Trim('#'), 6, 6) {
                        Disabled = false,
                        LetterChars = string.Join("\n",
                            "0 1 2 3",
                            "4 5 6 7",
                            "8 9 A B",
                            "C D E F"
                        )
                    }
                    .Apply(it => {
                        it.Pressed(() => {
                            // temporary fix for pressing a button in a submenu item which goes to another menu
                            // in this case, textMenu.Focused is false but subMenu.Focused is true
                            // so the button can be pressed multiple times and cause issues
                            subMenu.Focused = false;
                        });
                        it.Change(value => {
                            colorStop.Color = Calc.HexToColor(value);
                        });
                        it.OnMenuExit += confirm => {
                            subMenu.Focused = true;
                            if (!confirm) {
                                return;
                            }

                            subMenu.Exit();
                            RecreatePatternEntries(textMenu, inGame);
                            textMenu.MoveSelection(0);
                            if (textMenu.Current is TextMenuExt.SubMenu selectedSubMenu) {
                                string originalConfirmSfx = selectedSubMenu.ConfirmSfx;
                                selectedSubMenu.ConfirmSfx = SFX.NONE;
                                selectedSubMenu.ConfirmPressed();
                                selectedSubMenu.ConfirmSfx = originalConfirmSfx;
                                new DynData<TextMenuExt.SubMenu>(selectedSubMenu).Set("ease", 1f);
                                selectedSubMenu.Selection = -1;
                                selectedSubMenu.MoveSelection(1);
                            }
                        };
                    }));

                if (i != 0) {
                    subMenu.Add(new TextMenu.Slider("Width", i => i.ToString(), 0, 10, (int)colorStop.Width)
                        .Change(value => {
                            colorStop.Width = value;
                        }));
                }

                subMenu.Add(new TextMenu.Button("Move Up") {
                        Disabled = i == 0
                    }
                    .Apply(button => {
                        button.Pressed(() => {
                            // ReSharper disable once SwapViaDeconstruction
                            ColorStop temp = CurrentPattern.ColorStops[index - 1];
                            CurrentPattern.ColorStops[index - 1] = CurrentPattern.ColorStops[index];
                            CurrentPattern.ColorStops[index] = temp;
                            subMenu.Exit();
                            textMenu.Scene.OnEndOfFrame += () => {
                                RecreatePatternEntries(textMenu, inGame);
                                textMenu.MoveSelection(-1);
                                if (textMenu.Current is TextMenuExt.SubMenu selectedSubMenu) {
                                    // if it's a color stop item, automatically open it
                                    string originalConfirmSfx = selectedSubMenu.ConfirmSfx;
                                    selectedSubMenu.ConfirmSfx = SFX.NONE;
                                    selectedSubMenu.ConfirmPressed();
                                    selectedSubMenu.ConfirmSfx = originalConfirmSfx;
                                    new DynData<TextMenuExt.SubMenu>(selectedSubMenu).Set("ease", 1f);
                                    // select "Move Up"
                                    selectedSubMenu.Selection = selectedSubMenu.Items.FindIndex(item => item is TextMenu.Button {Label: "Move Up"}) - 1;
                                    selectedSubMenu.MoveSelection(1);
                                }
                            };
                        });
                    }));
                subMenu.Add(new TextMenu.Button("Move Down") {
                        Disabled = i == CurrentPattern.ColorStops.Count - 1
                    }
                    .Apply(button => {
                        button.Pressed(() => {
                            // ReSharper disable once SwapViaDeconstruction
                            ColorStop temp = CurrentPattern.ColorStops[index + 1];
                            CurrentPattern.ColorStops[index + 1] = CurrentPattern.ColorStops[index];
                            CurrentPattern.ColorStops[index] = temp;
                            subMenu.Exit();
                            textMenu.Scene.OnEndOfFrame += () => {
                                RecreatePatternEntries(textMenu, inGame);
                                textMenu.MoveSelection(1);
                                if (textMenu.Current is TextMenuExt.SubMenu selectedSubMenu) {
                                    // if it's a color stop item, automatically open it
                                    string originalConfirmSfx = selectedSubMenu.ConfirmSfx;
                                    selectedSubMenu.ConfirmSfx = SFX.NONE;
                                    selectedSubMenu.ConfirmPressed();
                                    selectedSubMenu.ConfirmSfx = originalConfirmSfx;
                                    new DynData<TextMenuExt.SubMenu>(selectedSubMenu).Set("ease", 1f);
                                    // select "Move Down"
                                    selectedSubMenu.Selection = selectedSubMenu.Items.FindIndex(item => item is TextMenu.Button {Label: "Move Down"}) - 1;
                                    selectedSubMenu.MoveSelection(1);
                                }
                            };
                        });
                    }));

                subMenu.Add(new TextMenu.Button("Remove") {
                        Disabled = CurrentPattern.ColorStops.Count <= 1
                    }
                    .Pressed(() => {
                        CurrentPattern.ColorStops.RemoveAt(index);
                        subMenu.Exit();
                        textMenu.Scene.OnEndOfFrame += () => {
                            RecreatePatternEntries(textMenu, inGame);
                        };
                    }));

                currentPatternItems.Add(subMenu);
            }
            currentPatternItems.Add(new TextMenu.Button("Add Color")
                .Apply(button => {
                    button.Pressed(() => {
                        CurrentPattern.ColorStops.Add(new ColorStop {
                            Color = Color.White,
                            Width = 1f
                        });
                        textMenu.Scene.OnEndOfFrame += () => {
                            RecreatePatternEntries(textMenu, inGame);
                        };
                    });
                }));
        }

        public void CreatePatternsEntry(TextMenu textMenu, bool inGame) {
            CreatePatternEntries(textMenu, inGame);
            currentPatternItems.ForEach(item => textMenu.Add(item));
        }

        public enum TrailTypes {

            HairColor,
            Pattern,
            OnionSkin

        }

        [SettingSubMenu]
        public class DebugSettings {

            public bool RenderTrailManagerBuffer { get; set; } = false;

            public bool ShowTextMenuBorders { get; set; } = false;

        }

    }

    public record TrailPattern {

        public List<ColorStop> ColorStops { get; set; } = new List<ColorStop> {new ColorStop()};

        public float Duration {
            get => DurationSliderValue / 20f;
            set => DurationSliderValue = Calc.Clamp((int)Math.Round(value * 20), 1, 60 * 20);
        }

        [YamlIgnore]
        public int DurationSliderValue { get; set; } = 5 * 20;

        public Color ColorAtTime(float time) {
            if (ColorStops.Count == 1 || Duration == 0f) {
                return ColorStops[0].Color;
            }
            time %= Duration;
            List<float> weightAccumulated = new List<float>(ColorStops.Count) {0};
            float currentWeight = 0;
            for (int i = 1; i < ColorStops.Count; i++) {
                currentWeight += ColorStops[i].Width;
                weightAccumulated.Add(currentWeight);
            }
            float weightSum = weightAccumulated.Last();
            if (weightSum == 0f) {
                return ColorStops[0].Color;
            }
            List<float> offsets = weightAccumulated.Select(i => Duration * (i / weightSum)).ToList();
            int index = offsets.FindIndex(i => time < i) - 1;
            return Color.Lerp(ColorStops[index].Color, ColorStops[index + 1].Color,
                (time - offsets[index]) / (offsets[index + 1] - offsets[index]));
        }

    }

    public record ColorStop {

        public Color Color { get; set; } = Color.White;

        public float Width { get; set; } = 1;

    }
}
