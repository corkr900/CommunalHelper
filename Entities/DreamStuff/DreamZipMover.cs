﻿using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/DreamZipMover")]
    [TrackedAs(typeof(DreamBlock))]
    public class DreamZipMover : CustomDreamBlock {

        private DreamZipMoverPathRenderer pathRenderer;

        private Vector2 start;
        private Vector2 target;
        private float percent = 0f;

        private SoundSource sfx;

        private bool dreamAesthetic;
        private bool noReturn;
        private MTexture cross;

        public DreamZipMover(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, data.Bool("featherMode"), data.Bool("oneUse"), data.Bool("doubleRefill", false)) {
            start = Position;
            target = data.Nodes[0] + offset;

            noReturn = data.Bool("noReturn");
            dreamAesthetic = data.Bool("dreamAesthetic");

            Add(new Coroutine(Sequence()));
            Add(new LightOcclude());
            Add(sfx = new SoundSource {
                Position = Center
            });
            cross = GFX.Game["objects/CommunalHelper/dreamMoveBlock/x"];
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            scene.Add(pathRenderer = new DreamZipMoverPathRenderer(this, dreamAesthetic));
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
        }

        public override void Removed(Scene scene) {
            scene.Remove(pathRenderer);
            pathRenderer = null;
            base.Removed(scene);
        }

        public override void Render() {
            Vector2 position = Position;
            Position += Shake;
            base.Render();
            if (noReturn) {
                cross.DrawCentered(Center + baseData.Get<Vector2>("shake"));
            }
            Position = position;
        }

        private void ScrapeParticlesCheck(Vector2 to) {
            if (!Scene.OnInterval(0.03f)) {
                return;
            }
            bool flag = to.Y != ExactPosition.Y;
            bool flag2 = to.X != ExactPosition.X;
            if (flag && !flag2) {
                int num = Math.Sign(to.Y - ExactPosition.Y);
                Vector2 value = (num != 1) ? TopLeft : BottomLeft;
                int num2 = 4;
                if (num == 1) {
                    num2 = Math.Min((int) Height - 12, 20);
                }
                int num3 = (int) Height;
                if (num == -1) {
                    num3 = Math.Max(16, (int) Height - 16);
                }
                if (Scene.CollideCheck<Solid>(value + new Vector2(-2f, num * -2))) {
                    for (int i = num2; i < num3; i += 8) {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, TopLeft + new Vector2(0f, i + num * 2f), (num == 1) ? (-(float) Math.PI / 4f) : ((float) Math.PI / 4f));
                    }
                }
                if (Scene.CollideCheck<Solid>(value + new Vector2(Width + 2f, num * -2))) {
                    for (int j = num2; j < num3; j += 8) {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, TopRight + new Vector2(-1f, j + num * 2f), (num == 1) ? ((float) Math.PI * -3f / 4f) : ((float) Math.PI * 3f / 4f));
                    }
                }
            } else {
                if (!flag2 || flag) {
                    return;
                }
                int num4 = Math.Sign(to.X - ExactPosition.X);
                Vector2 value2 = (num4 != 1) ? TopLeft : TopRight;
                int num5 = 4;
                if (num4 == 1) {
                    num5 = Math.Min((int) Width - 12, 20);
                }
                int num6 = (int) Width;
                if (num4 == -1) {
                    num6 = Math.Max(16, (int) Width - 16);
                }
                if (Scene.CollideCheck<Solid>(value2 + new Vector2(num4 * -2, -2f))) {
                    for (int k = num5; k < num6; k += 8) {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, TopLeft + new Vector2(k + num4 * 2f, -1f), (num4 == 1) ? ((float) Math.PI * 3f / 4f) : ((float) Math.PI / 4f));
                    }
                }
                if (Scene.CollideCheck<Solid>(value2 + new Vector2(num4 * -2, Height + 2f))) {
                    for (int l = num5; l < num6; l += 8) {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, BottomLeft + new Vector2(l + num4 * 2f, 0f), (num4 == 1) ? ((float) Math.PI * -3f / 4f) : (-(float) Math.PI / 4f));
                    }
                }
            }
        }

        private IEnumerator Sequence() {
            Vector2 start = Position;
            Vector2 end = target;
            while (true) {
                if (!HasPlayerRider()) {
                    yield return null;
                    continue;
                }
                sfx.Play(CustomSFX.game_dreamZipMover_dream_zip_mover);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                StartShaking(0.1f);
                yield return 0.1f;

                StopPlayerRunIntoAnimation = false;
                float at2 = 0f;
                while (at2 < 1f) {
                    yield return null;
                    at2 = Calc.Approach(at2, 1f, 2f * Engine.DeltaTime);
                    percent = Ease.SineIn(at2);
                    Vector2 to = Vector2.Lerp(start, end, percent);
                    ScrapeParticlesCheck(to);
                    if (Scene.OnInterval(0.1f)) {
                        pathRenderer.CreateSparks();
                    }
                    MoveTo(to);
                }
                StartShaking(0.2f);
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                SceneAs<Level>().Shake();
                StopPlayerRunIntoAnimation = true;
                yield return 0.5f;

                if (!noReturn) {
                    StopPlayerRunIntoAnimation = false;
                    float at = 0f;
                    while (at < 1f) {
                        yield return null;
                        at = Calc.Approach(at, 1f, 0.5f * Engine.DeltaTime);
                        percent = 1f - Ease.SineIn(at);
                        Vector2 to2 = Vector2.Lerp(end, start, Ease.SineIn(at));
                        MoveTo(to2);
                    }
                    StopPlayerRunIntoAnimation = true;
                    StartShaking(0.2f);
                    yield return 0.5f;
                } else {
                    sfx.Stop();
                    Vector2 temp = start;
                    start = end;
                    end = temp;
                }
            }
        }

        protected override void OneUseDestroy() {
            base.OneUseDestroy();
            Scene.Remove(pathRenderer);
            pathRenderer = null;
            sfx.Stop();
        }

        private class DreamZipMoverPathRenderer : Entity {
            public DreamZipMover DreamZipMover;
            private MTexture cog;
            private MTexture cogWhite;

            private Vector2 from;
            private Vector2 to;

            private Vector2 sparkAdd;
            private float sparkDirFromA;
            private float sparkDirFromB;
            private float sparkDirToA;
            private float sparkDirToB;

            private static readonly Color ropeColor = Calc.HexToColor("663931");
            private static readonly Color ropeLightColor = Calc.HexToColor("9b6157");
            private static readonly Color dreamRopeColor = Calc.HexToColor("ffffff");

            private Color[] dreamColors = new Color[9];

            private bool dreamAesthetic;

            public DreamZipMoverPathRenderer(DreamZipMover dreamZipMover, bool dreamAesthetic) {
                Depth = 5000;
                DreamZipMover = dreamZipMover;
                this.dreamAesthetic = dreamAesthetic;
                from = DreamZipMover.start + new Vector2(DreamZipMover.Width / 2f, DreamZipMover.Height / 2f);
                to = DreamZipMover.target + new Vector2(DreamZipMover.Width / 2f, DreamZipMover.Height / 2f);
                sparkAdd = (from - to).SafeNormalize(5f).Perpendicular();
                float num = (from - to).Angle();
                sparkDirFromA = num + (float) Math.PI / 8f;
                sparkDirFromB = num - (float) Math.PI / 8f;
                sparkDirToA = num + (float) Math.PI - (float) Math.PI / 8f;
                sparkDirToB = num + (float) Math.PI + (float) Math.PI / 8f;
                cog = GFX.Game[dreamAesthetic ? "objects/CommunalHelper/dreamZipMover/cog" : "objects/zipmover/cog"];
                cogWhite = GFX.Game["objects/CommunalHelper/dreamZipMover/cogWhite"];

                dreamColors[0] = Calc.HexToColor("FFEF11");
                dreamColors[1] = Calc.HexToColor("FF00D0");
                dreamColors[2] = Calc.HexToColor("08a310");
                dreamColors[3] = Calc.HexToColor("5fcde4");
                dreamColors[4] = Calc.HexToColor("7fb25e");
                dreamColors[5] = Calc.HexToColor("E0564C");
                dreamColors[6] = Calc.HexToColor("5b6ee1");
                dreamColors[7] = Calc.HexToColor("CC3B3B");
                dreamColors[8] = Calc.HexToColor("7daa64");
            }

            public void CreateSparks() {
                SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, from + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromA);
                SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, from - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromB);
                SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, to + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToA);
                SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, to - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToB);
            }

            public override void Render() {
                DrawCogs(Vector2.UnitY, Color.Black);
                DrawCogs(Vector2.Zero);
            }

            private void DrawCogs(Vector2 offset, Color? colorOverride = null) {
                float colorLerp = DreamZipMover.ColorLerp;
                Color colorLerpTarget = DreamZipMover.baseData.Get<Color>("activeLineColor");
                Vector2 travelDir = (to - from).SafeNormalize();
                Vector2 hOffset1 = travelDir.Perpendicular() * 3f;
                Vector2 hOffset2 = -travelDir.Perpendicular() * 4f;
                float rotation = -DreamZipMover.percent * (float) Math.PI * 2f;

                Color color = Color.Lerp(dreamAesthetic ? dreamRopeColor : ropeColor, colorLerpTarget, colorLerp);
                Draw.Line(from + hOffset1 + offset, to + hOffset1 + offset, colorOverride ?? color);
                Draw.Line(from + hOffset2 + offset, to + hOffset2 + offset, colorOverride ?? color);
                float dist = (to - from).Length();
                float shiftProgress = DreamZipMover.percent * (float) Math.PI * 8f;
                for (float lengthProgress = shiftProgress % 4f; lengthProgress < dist; lengthProgress += 4f) {
                    Vector2 value3 = from + hOffset1 + travelDir.Perpendicular() + travelDir * lengthProgress;
                    Vector2 value4 = to + hOffset2 - travelDir * lengthProgress;

                    Color lightColor = ropeLightColor;
                    if (dreamAesthetic) {
                        lightColor = dreamColors[(int) mod((float) Math.Round((lengthProgress - shiftProgress) / 4f), 9f)];
                    }
                    lightColor = Color.Lerp(lightColor, colorLerpTarget, colorLerp);
                    Draw.Line(value3 + offset, value3 + travelDir * 2f + offset, colorOverride ?? lightColor);
                    Draw.Line(value4 + offset, value4 - travelDir * 2f + offset, colorOverride ?? lightColor);
                }
                cog.DrawCentered(from + offset, colorOverride ?? Color.White, 1f, rotation);
                cog.DrawCentered(to + offset, colorOverride ?? Color.White, 1f, rotation);
                if (colorLerp > 0f && !colorOverride.HasValue) {
                    Color tempColor = Color.Lerp(Color.Transparent, colorLerpTarget, colorLerp);
                    cogWhite.DrawCentered(from + offset, tempColor, 1f, rotation);
                    cogWhite.DrawCentered(to + offset, tempColor, 1f, rotation);
                }
            }

            private float mod(float x, float m) {
                return (x % m + m) % m;
            }
        }

    }
}
