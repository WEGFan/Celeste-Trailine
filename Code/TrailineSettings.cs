using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Trailine.Modules;
using Celeste.Mod.Trailine.UI;
using Celeste.Mod.Trailine.Utils;
using Celeste.Mod.UI;
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
        [SettingIgnore]
        [YamlIgnore]
#endif
        public bool RenderTrailManagerBuffer { get; set; } = false;

#if !DEBUG
        [SettingIgnore]
        [YamlIgnore]
#endif
        public bool ShowTextMenuBorders { get; set; } = false;

        public TrailTypes TrailType { get; set; } = TrailTypes.HairColor;

        public int TrailFrequency { get; set; } = 10;

        public int TrailDuration { get; set; } = 200;

        public int TrailOpacity { get; set; } = 16;

        public bool HideOriginalDashTrails { get; set; } = true;

        [SettingIgnore]
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
            TextMenu.Item item = new TextMenu.Slider("Trail Frequency", i => $"{i / 100.0:F2}s",
                    1, 100, TrailFrequency)
                .Change(value => {
                    TrailFrequency = value;
                });
            textMenu.Add(item);
        }

        public void CreateTrailDurationEntry(TextMenu textMenu, bool inGame) {
            TextMenu.Item item = new TextMenu.Slider("Trail Duration", i => $"{i / 100.0:F2}s",
                    0, 6000, TrailDuration)
                .Change(value => {
                    TrailDuration = value;
                });
            textMenu.Add(item);
        }

        public void CreateTrailOpacityEntry(TextMenu textMenu, bool inGame) {
            TextMenu.Item item = new TextMenu.Slider("Trail Opacity", i => $"{i * 5}%",
                    0, 20, TrailOpacity)
                .Change(value => {
                    TrailOpacity = value;
                });
            textMenu.Add(item);
        }

        private void RecreateColorStopEntries(List<TextMenu.Item> existingColorStopItems, TextMenu textMenu, bool inGame) {
            int firstIndex = textMenu.Items.IndexOf(existingColorStopItems[0]);
            existingColorStopItems.ForEach(item => textMenu.Remove(item));
            List<TextMenu.Item> colorStopItems = CreateColorStopEntries(textMenu, inGame);
            foreach (TextMenu.Item item in Enumerable.Reverse(colorStopItems)) {
                textMenu.Insert(firstIndex, item);
            }
        }

        public List<TextMenu.Item> CreateColorStopEntries(TextMenu textMenu, bool inGame) {
            // TODO: refactor this huge mess code
            List<TextMenu.Item> colorStopItems = new List<TextMenu.Item>();
            for (int i = 0; i < CurrentPattern.ColorStops.Count; i++) {
                ColorStop colorStop = CurrentPattern.ColorStops[i];
                TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu($"Color {i + 1}: {ColorUtils.ColorToHex(colorStop.Color)}", false);

                subMenu.Add(new TextMenu.Button($"Color: {ColorUtils.ColorToHex(colorStop.Color)}") {
                        Disabled = inGame
                    }
                    .Pressed(() => {
                        Audio.Play("event:/ui/main/savefile_rename_start");
                        textMenu.SceneAs<Overworld>().Goto<OuiModOptionString>()
                            .Init<OuiModOptions>(ColorUtils.ColorToHex(colorStop.Color).TrimStart('#'),
                                value => {
                                    colorStop.Color = Calc.HexToColor(value);
                                }, 6, 6);
                    }));

                if (i != 0) {
                    subMenu.Add(new TextMenu.Slider("Width", i => i.ToString(), 0, 10, (int)colorStop.Width)
                        .Change(value => {
                            colorStop.Width = value;
                        }));
                }

                int index = i;
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
                                RecreateColorStopEntries(colorStopItems, textMenu, inGame);
                                textMenu.MoveSelection(-1);
                                if (textMenu.Current is TextMenuExt.SubMenu selectedSubMenu) {
                                    // if it's a color stop item, automatically open it
                                    selectedSubMenu.ConfirmPressed();
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
                                RecreateColorStopEntries(colorStopItems, textMenu, inGame);
                                textMenu.MoveSelection(1);
                                if (textMenu.Current is TextMenuExt.SubMenu selectedSubMenu) {
                                    // if it's a color stop item, automatically open it
                                    selectedSubMenu.ConfirmPressed();
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
                            RecreateColorStopEntries(colorStopItems, textMenu, inGame);
                        };
                    }));

                colorStopItems.Add(subMenu);
            }
            colorStopItems.Add(new TextMenu.Button("Add Color")
                .Apply(button => {
                    button.Pressed(() => {
                        CurrentPattern.ColorStops.Add(new ColorStop {
                            Color = Color.White,
                            Width = 1f
                        });
                        textMenu.Scene.OnEndOfFrame += () => {
                            RecreateColorStopEntries(colorStopItems, textMenu, inGame);
                        };
                    });
                }));
            return colorStopItems;
        }

        public void CreatePatternsEntry(TextMenu textMenu, bool inGame) {
            textMenu
                .Add(new PatternPreview(CurrentPattern))
                .Add(new TextMenu.Slider("Duration", i => $"{i}s", 1, 20, (int)CurrentPattern.Duration)
                    .Change(value => {
                        CurrentPattern.Duration = value;
                    }));

            List<TextMenu.Item> colorStopItems = CreateColorStopEntries(textMenu, inGame);
            colorStopItems.ForEach(item => textMenu.Add(item));
        }

        public enum TrailTypes {

            HairColor,
            Pattern,
            OnionSkin

        }

    }

    public record TrailPattern {

        public List<ColorStop> ColorStops { get; set; } = new List<ColorStop> {new ColorStop()};

        public float Duration { get; set; } = 5f;

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
