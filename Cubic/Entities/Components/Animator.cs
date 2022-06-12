using System.Collections.Generic;
using System.Drawing;

namespace Cubic.Entities.Components;

/// <summary>
/// Represents a set of animations for a sprite.
/// </summary>
public sealed class Animator : Component
{
    /// <summary>
    /// This event is called whenever an animation is finished. Useful to return a sprite to an idle animation once a
    /// playing animation has completed.
    /// </summary>
    public event OnAnimationFinished AnimationFinished;
    
    private bool _animated;
    private Animation _activeAnimation;
    private uint _currentFrame;
    private Sprite _sprite;
    private float _time;
    
    /// <summary>
    /// All the animations stored in this component, with their names.
    /// </summary>
    public readonly Dictionary<string, Animation> Animations;
    
    /// <summary>
    /// The idle animation name, if any. If one is set, a sprite will return to this animation automatically once any
    /// non-looping animation has finished.
    /// </summary>
    public readonly string IdleAnimationName;
    
    /// <summary>
    /// Create a new animator component, with the given animations.
    /// </summary>
    /// <param name="animations">The animations to add to this component.</param>
    /// <param name="idleAnimationName">The idle animation name, if any.</param>
    public Animator(IEnumerable<Animation> animations, string idleAnimationName = "")
    {
        Animations = new Dictionary<string, Animation>();
        foreach (Animation animation in animations)
            Animations.Add(animation.Name, animation);
        
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

    /// <summary>
    /// Play an animation with the given name.
    /// </summary>
    /// <param name="name">The animation name to play.</param>
    public void Play(string name)
    {
        _currentFrame = 0;
        _activeAnimation = Animations[name];
        _sprite.Source = _activeAnimation.Frames[_currentFrame];
        _animated = true;
    }

    /// <summary>
    /// Resume any paused animation <b>(DO NOT use this to initialize a new animation.)</b>
    /// </summary>
    public void Play()
    {
        _animated = true;
    }

    /// <summary>
    /// Pause the current animation on the current frame.
    /// </summary>
    public void Pause()
    {
        _animated = false;
    }
    
    /// <summary>
    /// Represents an animation that can be used in the <see cref="Animator"/>.
    /// </summary>
    public struct Animation
    {
        /// <summary>
        /// The name of this animation.
        /// </summary>
        public readonly string Name;
        
        /// <summary>
        /// The FPS (frames per second) this animation will run at.
        /// </summary>
        public readonly uint Fps;
        
        /// <summary>
        /// The source rectangles that this animation uses.
        /// </summary>
        public readonly Rectangle[] Frames;
        
        /// <summary>
        /// Whether or not this animation will loop.
        /// </summary>
        public readonly bool Loop;
        internal readonly float Delta;

        /// <summary>
        /// Create an animation with a name, FPS, source rectangles, and loop.
        /// </summary>
        /// <param name="name">The name of the animation.</param>
        /// <param name="fps">The FPS (frames per second) the animation will run at.</param>
        /// <param name="frames">The source rectangles for this animation. This is relative to the sprite sheet for the sprite.</param>
        /// <param name="loop">Whether or not this animation should loop.</param>
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