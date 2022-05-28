using System.Collections.Generic;
using Cubic.Audio;

namespace Cubic.Entities.Components;

public class AudioSource : Component
{
    private Dictionary<string, Sound> _sounds;

    public AudioSource(params (string name, string path)[] sounds)
    {
        _sounds = new Dictionary<string, Sound>();
        foreach ((string, string) sound in sounds)
        {
            _sounds.Add(sound.Item1, new Sound(Game.AudioDevice, sound.Item2));
        }
    }

    public void AddSound(string name, string path)
    {
        _sounds.Add(name, new Sound(Game.AudioDevice, path));
    }

    public void RemoveSound(string name)
    {
        _sounds[name].Dispose();
        _sounds.Remove(name);
    }
    
    public void Play(string soundName, float pitch = 1, float volume = 1, bool persistent = false)
    {
        _sounds[soundName].Play(pitch: pitch, volume: volume, persistent: false);
    }
}