using System;
using UnityEngine;

public enum SoundType : byte
{
    None = 0,
    GunShot = 1,
    AutoShot = 2,
    RocketShot = 3,
    LaserShot = 4,
    Explosion = 5,
    AsteroidExplosion = 6, 
    Hit = 7
}

[Serializable]
public class SoundData
{
    public string name;
    public SoundType type;
    public AudioClip clip;

    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.5f, 1.5f)] public float pitch = 1f;

    [Header("3D Settings")]
    [Range(0f, 1f)] public float spatialBlend = 1f;
    public float minDistance = 5f;
    public float maxDistance = 50f;
}