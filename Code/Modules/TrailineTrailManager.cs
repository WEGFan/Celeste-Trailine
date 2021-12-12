using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.Trailine.Modules {
    [Tracked]
    public class TrailineTrailManager : Entity {

        private static BlendState MaxBlendState = new BlendState {
            ColorSourceBlend = Blend.DestinationAlpha,
            AlphaSourceBlend = Blend.DestinationAlpha
        };

        private const int size = columns * rows;

        private const int columns = 64; // 64

        private const int rows = 64;

        private Snapshot[] snapshots = new Snapshot[size];

        private VirtualRenderTarget buffer;

        private bool dirty;

        public TrailineTrailManager() {
            Tag = Tags.Global;
            Depth = 10;
            Add(new BeforeRenderHook(BeforeRender));
            Add(new MirrorReflection());
        }

        public override void Removed(Scene scene) {
            Dispose();
            base.Removed(scene);
        }

        public override void SceneEnd(Scene scene) {
            Dispose();
            base.SceneEnd(scene);
        }

        private void Dispose() {
            if (buffer != null) {
                buffer.Dispose();
            }
            buffer = null;
        }

        private void BeforeRender() {
            if (!dirty) {
                return;
            }
            if (buffer == null) {
                buffer = VirtualContent.CreateRenderTarget("trailine-trail-manager", columns * 64, rows * 64);
            }
            Engine.Graphics.GraphicsDevice.SetRenderTarget(buffer);
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, LightingRenderer.OccludeBlendState);
            for (int i = 0; i < snapshots.Length; i++) {
                if (snapshots[i] != null && !snapshots[i].Drawn) {
                    Draw.Rect(i % columns * 64, i / columns * 64, 64f, 64f, Color.Transparent);
                }
            }
            Draw.SpriteBatch.End();
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, RasterizerState.CullNone);
            for (int j = 0; j < snapshots.Length; j++) {
                if (snapshots[j] == null || snapshots[j].Drawn) {
                    continue;
                }
                Snapshot snapshot = snapshots[j];
                Vector2 vector = new Vector2((j % columns + 0.5f) * 64f, (j / columns + 0.5f) * 64f) - snapshot.Position;
                if (snapshot.Hair != null) {
                    // FIX: limit hair length
                    for (int k = 0; k < snapshot.Hair.Nodes.Count; k++) {
                        snapshot.Hair.Nodes[k] += vector;
                    }
                    snapshot.Hair.Render();
                    for (int l = 0; l < snapshot.Hair.Nodes.Count; l++) {
                        snapshot.Hair.Nodes[l] -= vector;
                    }
                }
                Vector2 scale = snapshot.Sprite.Scale;
                snapshot.Sprite.Scale = snapshot.SpriteScale;
                snapshot.Sprite.Position += vector;
                snapshot.Sprite.Render();
                snapshot.Sprite.Scale = scale;
                snapshot.Sprite.Position -= vector;
                snapshot.Drawn = true;
            }
            Draw.SpriteBatch.End();
            if (TrailineModule.Settings.TrailType != TrailineSettings.TrailTypes.OnionSkin) {
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, MaxBlendState);
                Draw.Rect(0f, 0f, buffer.Width, buffer.Height, new Color(1f, 1f, 1f, 1f));
                Draw.SpriteBatch.End();
            }
            dirty = false;
        }

        public static void Add(Entity entity, Color color, float duration = 1f, bool frozenUpdate = false, bool useRawDeltaTime = false) {
            Image image = entity.Get<PlayerSprite>();
            if (image == null) {
                image = entity.Get<Sprite>();
            }
            PlayerHair hair = entity.Get<PlayerHair>();
            Add(entity.Position, image, hair, image.Scale, color, entity.Depth + 1, duration, frozenUpdate, useRawDeltaTime);
        }

        public static void Add(Entity entity, Vector2 scale, Color color, float duration = 1f) {
            Image image = entity.Get<PlayerSprite>();
            if (image == null) {
                image = entity.Get<Sprite>();
            }
            PlayerHair hair = entity.Get<PlayerHair>();
            Add(entity.Position, image, hair, scale, color, entity.Depth + 1, duration);
        }

        public static void Add(Vector2 position, Image image, Color color, int depth, float duration = 1f) {
            Add(position, image, null, image.Scale, color, depth, duration);
        }

        public static Snapshot Add(Vector2 position, Image sprite, PlayerHair hair, Vector2 scale, Color color, int depth, float duration = 1f, bool frozenUpdate = false, bool useRawDeltaTime = false) {
            TrailineTrailManager trailineTrailManager = Engine.Scene.Tracker.GetEntity<TrailineTrailManager>();
            if (trailineTrailManager == null) {
                trailineTrailManager = new TrailineTrailManager();
                Engine.Scene.Add(trailineTrailManager);
            }
            for (int i = 0; i < trailineTrailManager.snapshots.Length; i++) {
                if (trailineTrailManager.snapshots[i] == null) {
                    Snapshot snapshot = Engine.Pooler.Create<Snapshot>();
                    snapshot.Init(trailineTrailManager, i, position, sprite, hair, scale, color, duration, depth, frozenUpdate, useRawDeltaTime);
                    trailineTrailManager.snapshots[i] = snapshot;
                    trailineTrailManager.dirty = true;
                    Engine.Scene.Add(snapshot);
                    return snapshot;
                }
            }
            return null;
        }

        public static void Clear() {
            TrailineTrailManager entity = Engine.Scene.Tracker.GetEntity<TrailineTrailManager>();
            if (entity == null) {
                return;
            }
            for (int i = 0; i < entity.snapshots.Length; i++) {
                if (entity.snapshots[i] != null) {
                    entity.snapshots[i].RemoveSelf();
                }
            }
        }

        public static void Add(Entity entity, Color color, float duration = 1f) {
            Add(entity, color, duration, false);
        }

        public static void DebugRender() {
            TrailineTrailManager entity = Engine.Scene.Tracker.GetEntity<TrailineTrailManager>();
            if (entity == null) {
                return;
            }

            float scale = 0.5f;
            if (entity.buffer != null) {
                Draw.SpriteBatch.Draw(entity.buffer, new Vector2(0, 0), entity.buffer.Bounds, Color.White, 0f, Vector2.Zero, Vector2.One * scale, SpriteEffects.None, 0f);
                for (int x = 0; x <= entity.buffer.Width; x += 64) {
                    Draw.Line(new Vector2(x, 0) * scale, new Vector2(x, entity.buffer.Height) * scale, Color.Green);
                }
                for (int y = 0; y <= entity.buffer.Height; y += 64) {
                    Draw.Line(new Vector2(0, y) * scale, new Vector2(entity.buffer.Width, y) * scale, Color.Green);
                }
            }
        }

        [Pooled]
        [Tracked]
        public class Snapshot : Entity {

            public TrailineTrailManager Manager;

            public Image Sprite;

            public Vector2 SpriteScale;

            public PlayerHair Hair;

            public int Index;

            public Color Color;

            public float Percent;

            public float Duration;

            public bool Drawn;

            public bool UseRawDeltaTime;

            public Snapshot() {
                Add(new MirrorReflection());
            }

            public void Init(TrailineTrailManager manager, int index, Vector2 position, Image sprite, PlayerHair hair, Vector2 scale, Color color, float duration, int depth, bool frozenUpdate, bool useRawDeltaTime) {
                Tag = Tags.Global;
                if (frozenUpdate) {
                    Tag |= Tags.FrozenUpdate;
                }
                Manager = manager;
                Index = index;
                Position = position;
                Sprite = sprite;
                SpriteScale = scale;
                Hair = hair;
                Color = color;
                Percent = 0f;
                Duration = duration;
                Depth = depth;
                Drawn = false;
                UseRawDeltaTime = useRawDeltaTime;
            }

            public override void Update() {
                if (Duration <= 0f) {
                    if (Drawn) {
                        RemoveSelf();
                    }
                    return;
                }
                if (Percent >= 1f) {
                    RemoveSelf();
                }
                Percent += (UseRawDeltaTime ? Engine.RawDeltaTime : Engine.DeltaTime) / Duration;
            }

            public override void Render() {
                VirtualRenderTarget buffer = Manager.buffer;
                Rectangle value = new Rectangle(Index % columns * 64, Index / columns * 64, 64, 64);
                float num = Duration > 0f ? 0.75f * (1f - Ease.CubeOut(Percent)) : 1f;
                num *= TrailineModule.Settings.TrailOpacity * 5 / 100f;
                if (buffer != null) {
                    Draw.SpriteBatch.Draw((RenderTarget2D)buffer, Position, value, Color * num, 0f, new Vector2(64f, 64f) * 0.5f, Vector2.One, SpriteEffects.None, 0f);
                }
            }

            public override void Removed(Scene scene) {
                if (Manager != null) {
                    Manager.snapshots[Index] = null;
                }
                base.Removed(scene);
            }

        }

    }
}
