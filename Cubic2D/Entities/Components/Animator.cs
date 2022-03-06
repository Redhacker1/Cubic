using System.Collections.Generic;
using System.Drawing;

namespace Cubic2D.Entities.Components;

public sealed class Animator : Component
{
    public event OnAnimationFinished AnimationFinished;
    
    private bool _animated;
    private Animation _activeAnimation;
    private uint _currentFrame;
    private Sprite _sprite;
    private float _time;
    
    public readonly Dictionary<string, Animation> Animations;
    public readonly string IdleAnimationName;
    
    public Animator(Animation[] animations)
    {
        Animations = new Dictionary<string, Animation>();
        foreach (Animation animation in animations)
            Animations.Add(animation.Name, animation);
    }

    public Animator(Animation[] animations, string idleAnimationName) : this(animations)
    {
        IdleAnimationName = idleAnimationName;
    }
    
    protected internal override void Initialize()
    {
        base.Initialize();
        
        _sprite = GetComponent<Sprite>();
        if (IdleAnimationName != "")
            Play(IdleAnimationName);
    }

    protected internal override void Update()
    {
        base.Update();

        if (!_animated)
            return;

        _time += Time.DeltaTime;

        if (_time >= _activeAnimation.Delta)
        {
            _currentFrame++;
            if (_currentFrame >= _activeAnimation.Frames.Length)
            {
                if (_activeAnimation.Loop)
                    _currentFrame = 0;
                else
                {
                    if (IdleAnimationName != "")
                        Play(IdleAnimationName);
                    AnimationFinished?.Invoke(_activeAnimation.Name);
                }
            }

            _sprite.Source = _activeAnimation.Frames[_currentFrame];
            _time = 0;
        }
    }

    public void Play(string name)
    {
        _currentFrame = 0;
        _activeAnimation = Animations[name];
        _sprite.Source = _activeAnimation.Frames[_currentFrame];
        _animated = true;
    }

    public void Play()
    {
        _animated = true;
    }

    public void Pause()
    {
        _animated = false;
    }
    
    public struct Animation
    {
        public readonly string Name;
        public readonly uint Fps;
        public readonly Rectangle[] Frames;
        public readonly bool Loop;
        internal readonly float Delta;

        public Animation(string name, uint fps, Rectangle[] frames, bool loop = true)
        {
            Name = name;
            Fps = fps;
            Frames = frames;
            Loop = loop;
            Delta = 1f / fps;
        }
    }

    public delegate void OnAnimationFinished(string animationName);
}