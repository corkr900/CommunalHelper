﻿using Celeste.Mod.CommunalHelper.Utils;
using System.Collections;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/AeroBlockFlying")]
public class AeroBlockFlying : AeroBlock
{
    public enum TravelMode
    {
        Loop,
        BackAndForth,
        WithPlayer,
        WithPlayerOnce,
    }

    private static readonly Color propellerColor = Calc.HexToColor("686f99");

    public Vector2 Home { get; set; }
    new public Vector2 Speed { get; set; }

    private bool inactive;
    private bool airborne;

    public bool Carrying { get; private set; }

    private float accelLerp, carryLerp;
    private float particleCooldown;

    private float angularSpeed;
    private readonly Shape3D propeller;
    private float hoverLerp = 1.0f;
    private float deployLerp = 1.0f;
    private bool showPropeller = true;
    private const float propellerMoveDelay = 0.45f;

    private readonly SineWave sine;
    private readonly SoundSource sfx;

    public bool Hover { get; set; } = true;

    public AeroBlockFlying(EntityData data, Vector2 offset)
        : this(data.NodesWithPosition(offset), data.Width, data.Height, data.Bool("inactive", false), data.Float("travelSpeed", 32.0f), data.Enum("travelMode", TravelMode.Loop))
    { }

    public AeroBlockFlying(Vector2[] points, int width, int height, bool inactive, float travelSpeed = 32.0f, TravelMode mode = TravelMode.Loop)
        : this(points[0], width, height)
    {
        this.inactive = inactive;
        if (inactive)
        {
            sfx.Pause();
            hoverLerp = 0.0f;
            deployLerp = 0.0f;
            showPropeller = false;
            propeller.Visible = false;
        }

        if (inactive)
            points = points.Skip(1).Distinct().ToArray();

        Add(new Coroutine(TravelRoutine(inactive, travelSpeed, mode, points)));
    }

    public AeroBlockFlying(Vector2 position, int width, int height)
       : base(position, width, height)
    {
        Home = position;

        Add(sine = new(0.25f, Calc.Random.NextAngle()));
        Add(sfx = new SoundSource(CustomSFX.game_aero_block_loop)
        {
            Position = new Vector2(width / 2f, height),
        });

        float radius = width / 2 - 6;
        float teethsize = 5 / radius;
        Add(propeller = new Shape3D(Shapes.Gear(8, teethsize, 2f, 0.3f, 4f, radius, Color.White))
        {
            Position = new Vector3(width / 2f, height + 4, 0),
            Matrix = Matrix.CreateRotationX(MathHelper.PiOver2),
            Texture = CommunalHelperGFX.Blank,
            HighlightStrength = 0.5f,
            Depth = Depths.BGTerrain,
        });
        propeller.SetTint(propellerColor);
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        airborne = !CollideCheck<Platform>(Position + Vector2.UnitY);
    }

    public void GotoHome()
    {
        Vector2 offset = Home - Position;
        MoveTo(Home);
        foreach (JumpThru jt in jumpthrus)
        {
            jt.MoveH(offset.X);
            jt.MoveV(offset.Y);
        }
    }

    public void ActivateQuietly()
    {
        sfx.Play(CustomSFX.game_aero_block_loop);
        inactive = false;
        showPropeller = true;
        propeller.Visible = true;
        propeller.Position = new Vector3(Width / 2f, Height + 4, 0);
        hoverLerp = 1.0f;
        deployLerp = 1.0f;
    }

    public void Activate()
    {
        if (!sfx.Playing)
            sfx.Resume();

        inactive = false;
        {
            Level level = Scene as Level;
            Alarm.Set(this, propellerMoveDelay, () =>
            {
                showPropeller = true;
                Audio.Play(CustomSFX.game_aero_block_deploy_propeller, Center);
                level.ParticlesBG.Emit(P_Steam, (int) Width / 2, BottomCenter + Vector2.UnitY * 5, new(Width / 2f, 6), MathHelper.PiOver2);
            });
        }
    }

    public void Deactivate()
    {
        inactive = true;

        Alarm.Set(this, propellerMoveDelay, () =>
        {
            showPropeller = false;
            Audio.Play(CustomSFX.game_aero_block_retract_propeller, Center);
        });
    }

    public override void Update()
    {
        Level level = Scene as Level;

        Carrying = PlayerRiding();
        if (inactive)
        {
            if (airborne)
                Speed = Calc.Approach(Speed, Vector2.UnitY * 160f, Engine.DeltaTime * 160f);
            else
                airborne = !CollideCheck<Platform>(Position + Vector2.UnitY);
        }
        else
        {
            Vector2 target = Carrying && Hover
                ? Home + Vector2.UnitY * 18
                : Home;

            carryLerp = Calc.Approach(carryLerp, Carrying ? 1 : 0, Engine.DeltaTime * 1.5f);

            Vector2 to = target;
            if (Hover)
                to += Vector2.UnitY * sine.Value * 7f;

            bool slow = target.Y < Y && Speed.Y < 0 && !Carrying;

            accelLerp = Calc.Approach(accelLerp, Math.Sign(Speed.Y), Engine.DeltaTime * 1.12f);

            Speed *= 0.8f;
            Speed += (to - Position) * (slow ? 0.35f : 0.55f);

            float forceLerp = Calc.Clamp(Calc.Map(accelLerp, -1.0f, 1.0f, -1.0f, 0.5f) + carryLerp * 0.8f, -1, 1);
            sfx.Param("force", forceLerp);

            bool particles = false;
            particleCooldown -= Engine.DeltaTime;
            if (particleCooldown <= 0f)
            {
                particleCooldown = 0.16f - forceLerp / 9;
                particles = true;
            }
            if (particles)
                level.ParticlesBG.Emit(P_Steam, 1, BottomCenter + Vector2.UnitY * 5, new(Width / 4f, 6), MathHelper.PiOver2);
        }

        Vector2 prev = Position;
        bool collideX = MoveHCollideSolids(Speed.X * Engine.DeltaTime, thruDashBlocks: true);
        bool collideY = MoveVCollideSolids(Speed.Y * Engine.DeltaTime, thruDashBlocks: true);

        foreach (JumpThru jt in jumpthrus)
        {
            jt.MoveH(Position.X - prev.X, Speed.X);
            jt.MoveV(Position.Y - prev.Y, Speed.Y);
        }

        if (inactive && collideY)
        {
            Speed = Vector2.Zero;
            airborne = false;
            Audio.Play(SFX.game_03_fallingblock_wood_impact, Center);
            level.DirectionalShake(Vector2.UnitY);
        }

        hoverLerp = Calc.Approach(hoverLerp, !inactive && showPropeller ? 1f : 0f, Engine.DeltaTime * 0.4f);
        sfx.Param("power", hoverLerp);

        angularSpeed = hoverLerp * Engine.DeltaTime * (6 + accelLerp * 2 + carryLerp * 4f);
        if (angularSpeed != 0.0f)
        {
            Rotation += angularSpeed;
            propeller.Matrix = Matrix.CreateFromYawPitchRoll(0, MathHelper.PiOver2, Rotation);
        }
        deployLerp = Calc.Approach(deployLerp, showPropeller ? 1f : 0f, Engine.DeltaTime * 6f);
        propeller.Position = new Vector3(Width / 2f, Height - 2 + 6 * deployLerp, 0);
        propeller.Visible = deployLerp > 0.0f;

        base.Update();
    }

    private IEnumerator TravelRoutine(bool initiallyInactive, float speed, TravelMode mode, Vector2[] points)
    {
        Home = points[0];

        if (initiallyInactive)
        {
            while (!Carrying)
                yield return null;

            Level level = Scene as Level;

            var shakeSfx = Audio.Play(CustomSFX.game_shapeshifter_shake, Center);
            StartShaking(0.25f);
            yield return 0.25f;

            Activate();

            Audio.Stop(shakeSfx);
            Audio.Play(CustomSFX.game_aero_block_smash, BottomCenter);

            level.DirectionalShake(Vector2.UnitY, 0.4f);
            level.ParticlesFG.Emit(P_Steam, 16, BottomRight - Vector2.UnitX * 5, Vector2.UnitX * 8f, 0f);
            level.ParticlesFG.Emit(P_Steam, 16, BottomLeft + Vector2.UnitX * 5, Vector2.UnitX * 8f, MathHelper.Pi);

            yield return 0.5f;
        }

        if (points.Length == 1)
            yield break;

        float timer = 0.0f;
        while (true)
        {
            float t = mode switch
            {
                TravelMode.BackAndForth => Calc.Map(Util.TriangleWave(timer / points.Length), -1, 1, points.Length - 1, 0),
                _ => timer,
            };

            int a = (int) Math.Floor(t) % points.Length;
            int b = (int) (Math.Floor(t) + 1) % points.Length;
            float lerp = t - (float) Math.Floor(t);

            Vector2 from = points[a];
            Vector2 to = points[b];

            Home = Vector2.Lerp(from, to, lerp);

            float increment = Engine.DeltaTime * speed / Vector2.Distance(from, to);
            if (mode is TravelMode.WithPlayer or TravelMode.WithPlayerOnce)
                timer = Calc.Approach(timer, Carrying ? points.Length - 1 : 0, increment);
            else
                timer += increment;

            if (mode is TravelMode.WithPlayerOnce && timer == points.Length - 1)
                break;

            yield return null;
        }

        yield return 0.6f;
        Deactivate();
    }
}
