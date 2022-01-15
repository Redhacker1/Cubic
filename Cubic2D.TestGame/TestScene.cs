using System;
using System.Drawing;
using System.Numerics;
using Cubic2D.Entities;
using Cubic2D.Entities.Components;
using Cubic2D.Render;
using Cubic2D.Scenes;
using Cubic2D.Utilities;
using Cubic2D.Windowing;

namespace Cubic2D.TestGame;

public class TestScene : Scene
{
    private Texture2D _texture;

    private Vector2 _pos;

    private SpriteFlipMode _mode;

    private float _rot;
    
    protected override void Initialize()
    {
        base.Initialize();

        World.ClearColor = Color.CornflowerBlue;
        
        Console.WriteLine("Initialized!");

        _texture = new Texture2D("Content/awesomeface.png");

        Entity entity = new Entity();
        entity.AddComponent(new TestScript());
        entity.AddComponent(new Sprite(_texture));
        Entities.Add("myEntity", entity);
    }

    protected override void Update()
    {
        base.Update();

        //_pos += new Vector2(50f * Time.DeltaTime);

        if (Input.KeyPressed(Keys.Down))
            _mode = SpriteFlipMode.None;
        if (Input.KeyPressed(Keys.Up))
            _mode = SpriteFlipMode.FlipXY;
        if (Input.KeyPressed(Keys.Right))
            _mode = SpriteFlipMode.FlipX;
        if (Input.KeyPressed(Keys.Left))
            _mode = SpriteFlipMode.FlipY;
        
        if (Input.KeyPressed(Keys.Escape))
            CubicGame.Current.Close();

        _rot += 1 * Time.DeltaTime;

        //Camera.Main.Transform.Position.Y = 500;
        
        Console.WriteLine(Input.KeysPressed(Keys.A, Keys.B));
    }

    protected override void Draw()
    {
        base.Draw();
        
        /*Graphics.SpriteRenderer.Begin(Camera.Main.TransformMatrix);
        Graphics.SpriteRenderer.Draw(_texture, _pos, null, Color.White, _rot, _texture.Size.ToVector2() / 2f, new Vector2(5f, 0.5f), _mode);
        Graphics.SpriteRenderer.Draw(_texture, new Vector2(100), null, Color.White, _rot, Vector2.Zero, new Vector2(0.5f), _mode);
        Graphics.SpriteRenderer.End();*/
    }
}