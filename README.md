# Cubic2D
A fast, cross-platform 2D game engine.

### Component system
In Cubic2D, it's easy to add components and custom scripts to your entities.
```c#
Entity entity = new Entity();
entity.AddComponent(typeof(Sprite), new Texture2D("Content/MyTexture.png"));
entity.AddComponent(typeof(MyScript));
```

### Unity-like scripting system
It's easy to create scripts, too!
```c#
public class MyScript : Component
{
    protected override void Initialize()
    {
        Transform.Position = new Vector2(100, 200);
    }
    
    protected override void Update()
    {
        Transform.Rotation = Transform.LookAt(Input.MousePosition);
    
        const float moveSpeed = 50.0f;
    
        if (Input.KeysDown(Keys.Up, Keys.W))
            Transform.Position += Transform.Forward * moveSpeed * Time.DeltaTime;
    }
}
```

### Cross-platform
Cubic2D supports many different platforms, including Windows, Linux, MacOS, and soon, mobile OSs too.