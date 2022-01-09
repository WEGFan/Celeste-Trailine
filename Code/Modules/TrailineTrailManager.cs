using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.Trailine.Modules {
    [Tracked]
    public class TrailineTrailManager : Entity {

        private const int Rows = 64;
        private const int Columns = 64;

        private static readonly BlendState maxBlendState = new BlendState {
            ColorSourceBlend = Blend.DestinationAlpha,
            AlphaSourceBlend = Blend.DestinationAlpha
        };

        private readonly Snapshot[] snapshots = new Snapshot[Rows * Columns];
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
            buffer?.Dispose();
            buffer = null;
        }

        private void BeforeRender() {
            if (!dirty) {
                return;
            }

            buffer ??= VirtualContent.CreateRenderTarget("trailine-trail-manager", Columns * Snapshot.SnapshotWidth, Rows * Snapshot.SnapshotHeight);

            Engine.Graphics.GraphicsDevice.SetRenderTarget(buffer);
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, LightingRenderer.OccludeBlendState);
            for (int i = 0; i < snapshots.Length; i++) {
                if (snapshots[i] != null && !snapshots[i].Drawn) {
                    Draw.Rect(i % Columns * Snapshot.SnapshotWidth, i / Columns * Snapshot.SnapshotHeight, Snapshot.SnapshotWidth, Snapshot.SnapshotHeight, Color.Transparent);
                }
            }
            Draw.SpriteBatch.End();

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, RasterizerState.CullNone);
            for (int i = 0; i < snapshots.Length; i++) {
                if (snapshots[i] == null || snapshots[i].Drawn) {
                    continue;
                }
                Snapshot snapshot = snapshots[i];
                Vector2 vector = new Vector2((i % Columns + 0.5f) * Snapshot.SnapshotWidth, (i / Columns + 0.5f) * Snapshot.SnapshotHeight) - snapshot.Position;
                if (snapshot.Hair != null) {
                    PlayerHair hair = snapshot.Hair;
                    List<Vector2> nodes = hair.Nodes;

                    // count how many hair nodes should be used to draw trail
                    int drawNodesCount = 1;
                    int originalHairCount = hair.Sprite.HairCount;
                    for (int j = 1; j < originalHairCount; j++) {
                        // calculate node's position and size
                        MTexture hairTexture = hair.GetHairTexture(j);
                        Vector2 hairScale = hair.PublicGetHairScale(j).Abs();
                        Vector2 pos = new Vector2(-5f, -5f) * hairScale - snapshot.Position + nodes[j] + new Vector2(Snapshot.SnapshotWidth, Snapshot.SnapshotHeight) * 0.5f;
                        Vector2 size = new Vector2(hairTexture.Width, hairTexture.Height) * hairScale;
                        Hitbox rect = new Hitbox(size.X, size.Y, pos.X, pos.Y);

                        // if node is not fully inside trail's render box, don't render remaining nodes
                        // otherwise it will be drawn to other trails' box
                        if (!(rect.TopLeft is {X: >= 2f, Y: >= 2f} && rect.BottomRight is {X: <= Snapshot.SnapshotWidth - 2 * 2f, Y: <= Snapshot.SnapshotHeight - 2 * 2f})) {
                            break;
                        }
                        drawNodesCount++;
                    }

                    for (int j = 0; j < drawNodesCount; j++) {
                        nodes[j] += vector;
                    }
                    if (hair.DrawPlayerSpriteOutline) {
                        hair.Sprite.Position += vector;
                    }

                    // modify hair count so the trail has a better hair
                    int original = hair.Sprite.HairCount;
                    hair.Sprite.HairCount = drawNodesCount;

                    hair.Render();

                    hair.Sprite.HairCount = original;
                    for (int j = 0; j < drawNodesCount; j++) {
                        nodes[j] -= vector;
                    }
                    if (hair.DrawPlayerSpriteOutline) {
                        hair.Sprite.Position -= vector;
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
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, maxBlendState);
                Draw.Rect(0f, 0f, buffer.Width, buffer.Height, new Color(1f, 1f, 1f, 1f));
                Draw.SpriteBatch.End();
            }

            dirty = false;
        }

        public static void Add(Entity entity, Color color, float duration = 1f, bool frozenUpdate = false, bool useRawDeltaTime = false) {
            Image image = entity.Get<PlayerSprite>() ?? entity.Get<Sprite>();
            PlayerHair hair = entity.Get<PlayerHair>();
            Add(entity.Position, image, hair, image.Scale, color, entity.Depth + 1, duration, frozenUpdate, useRawDeltaTime);
        }

        public static void Add(Entity entity, Vector2 scale, Color color, float duration = 1f) {
            Image image = entity.Get<PlayerSprite>() ?? entity.Get<Sprite>();
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
            foreach (Snapshot snapshot in entity.snapshots) {
                snapshot?.RemoveSelf();
            }
        }

        public static void DebugRender() {
            TrailineTrailManager entity = Engine.Scene.Tracker.GetEntity<TrailineTrailManager>();
            if (entity == null) {
                return;
            }

            float scale = 0.5f;
            if (entity.buffer != null) {
                Draw.SpriteBatch.Draw(entity.buffer, new Vector2(0, 0), entity.buffer.Bounds, Color.White, 0f, Vector2.Zero, Vector2.One * scale, SpriteEffects.None, 0f);
                for (int x = 0; x <= entity.buffer.Width; x += Snapshot.SnapshotWidth) {
                    Draw.Line(new Vector2(x, 0) * scale, new Vector2(x, entity.buffer.Height) * scale, Color.Green);
                }
                for (int y = 0; y <= entity.buffer.Height; y += Snapshot.SnapshotHeight) {
                    Draw.Line(new Vector2(0, y) * scale, new Vector2(entity.buffer.Width, y) * scale, Color.Green);
                }
            }
        }

        [Pooled]
        [Tracked]
        public class Snapshot : Entity {

            public const int SnapshotWidth = 64;
            public const int SnapshotHeight = 64;

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
                Rectangle value = new Rectangle(Index % Columns * SnapshotWidth, Index / Columns * SnapshotHeight, SnapshotWidth, SnapshotHeight);
                float opacity = Duration > 0f ? 1f - Ease.CubeOut(Percent) : 1f;
                opacity *= TrailineModule.Settings.TrailOpacity;
                if (buffer != null) {
                    Draw.SpriteBatch.Draw((RenderTarget2D)buffer, Position, value, Color * opacity, 0f, new Vector2(SnapshotWidth, SnapshotHeight) * 0.5f, Vector2.One, SpriteEffects.None, 0f);
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
