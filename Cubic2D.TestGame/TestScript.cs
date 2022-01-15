using System.Numerics;
using Cubic2D.Entities.Components;

namespace Cubic2D.TestGame;

public class TestScript : Component
{
    protected override void Initialize()
    {
        base.Initialize();

        Transform.Position = new Vector2(200);
        Transform.Origin = new Vector2(256);
    }

    protected override void Update()
    {
        base.Update();

        if (Input.KeyDown(Keys.Down))
            Transform.Rotation += 1 * Time.DeltaTime;
        if (Input.KeyDown(Keys.Up))
            Transform.Rotation -= 1 * Time.DeltaTime;

        Transform.Position += Transform.Left * 50 * Time.DeltaTime;
    }
}