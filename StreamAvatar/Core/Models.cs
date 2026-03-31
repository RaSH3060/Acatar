using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace StreamAvatar.Core
{
    /// <summary>
    /// Represents a bone in the skeleton animation system
    /// </summary>
    public class Bone
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public float X { get; set; }
        public float Y { get; set; }
        public float Rotation { get; set; }
        public float Length { get; set; } = 50f;
        public string? ParentId { get; set; }
        
        [JsonIgnore]
        public Bone? Parent { get; set; }
        
        [JsonIgnore]
        public List<Bone> Children { get; set; } = new();
        
        public Bone Clone()
        {
            return new Bone
            {
                Id = this.Id,
                Name = this.Name,
                X = this.X,
                Y = this.Y,
                Rotation = this.Rotation,
                Length = this.Length,
                ParentId = this.ParentId
            };
        }
    }

    /// <summary>
    /// Represents a sprite layer for the avatar
    /// </summary>
    public class AvatarLayer
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string ImagePath { get; set; } = "";
        public int Order { get; set; }
        public float Opacity { get; set; } = 1f;
        public bool Visible { get; set; } = true;
        public string? AttachedBoneId { get; set; }
        
        // Mouth-specific properties
        public bool IsMouthLayer { get; set; } = false;
        public List<string> MouthFramePaths { get; set; } = new();
        public int CurrentMouthFrame { get; set; } = 0;
        
        // Eye-specific properties
        public bool IsEyeLayer { get; set; } = false;
        public float EyeMoveRadius { get; set; } = 10f;
        
        public AvatarLayer Clone()
        {
            return new AvatarLayer
            {
                Id = this.Id,
                Name = this.Name,
                ImagePath = this.ImagePath,
                Order = this.Order,
                Opacity = this.Opacity,
                Visible = this.Visible,
                AttachedBoneId = this.AttachedBoneId,
                IsMouthLayer = this.IsMouthLayer,
                MouthFramePaths = new List<string>(this.MouthFramePaths),
                CurrentMouthFrame = this.CurrentMouthFrame,
                IsEyeLayer = this.IsEyeLayer,
                EyeMoveRadius = this.EyeMoveRadius
            };
        }
    }

    /// <summary>
    /// Avatar preset containing all configuration
    /// </summary>
    public class AvatarPreset
    {
        public string Name { get; set; } = "New Avatar";
        public string Description { get; set; } = "";
        public List<AvatarLayer> Layers { get; set; } = new();
        public List<Bone> Bones { get; set; } = new();
        public AnimationSettings AnimationSettings { get; set; } = new();
        public AudioSettings AudioSettings { get; set; } = new();
        
        public void SaveToFile(string path)
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(path, json);
        }
        
        public static AvatarPreset LoadFromFile(string path)
        {
            var json = File.ReadAllText(path);
            var preset = JsonConvert.DeserializeObject<AvatarPreset>(json);
            
            if (preset == null)
                throw new InvalidOperationException("Failed to load avatar preset");
            
            // Rebuild bone hierarchy
            var boneDict = new Dictionary<string, Bone>();
            foreach (var bone in preset.Bones)
            {
                boneDict[bone.Id] = bone;
            }
            
            foreach (var bone in preset.Bones)
            {
                if (!string.IsNullOrEmpty(bone.ParentId) && boneDict.ContainsKey(bone.ParentId))
                {
                    bone.Parent = boneDict[bone.ParentId];
                    boneDict[bone.ParentId].Children.Add(bone);
                }
            }
            
            return preset;
        }
        
        public AvatarPreset Clone()
        {
            var clone = new AvatarPreset
            {
                Name = this.Name,
                Description = this.Description,
                AnimationSettings = this.AnimationSettings.Clone(),
                AudioSettings = this.AudioSettings.Clone()
            };
            
            foreach (var layer in this.Layers)
            {
                clone.Layers.Add(layer.Clone());
            }
            
            foreach (var bone in this.Bones)
            {
                clone.Bones.Add(bone.Clone());
            }
            
            return clone;
        }
    }

    /// <summary>
    /// Animation settings for the avatar
    /// </summary>
    public class AnimationSettings
    {
        public float IdleSpeed { get; set; } = 0.5f;
        public float BlinkIntervalMin { get; set; } = 2f;
        public float BlinkIntervalMax { get; set; } = 5f;
        public float BlinkDuration { get; set; } = 0.1f;
        public float ShakeThreshold { get; set; } = 0.8f;
        public float ShakeIntensity { get; set; } = 5f;
        public float MouthSensitivity { get; set; } = 1f;
        public int MouthFrameCount { get; set; } = 5;
        
        public AnimationSettings Clone()
        {
            return new AnimationSettings
            {
                IdleSpeed = this.IdleSpeed,
                BlinkIntervalMin = this.BlinkIntervalMin,
                BlinkIntervalMax = this.BlinkIntervalMax,
                BlinkDuration = this.BlinkDuration,
                ShakeThreshold = this.ShakeThreshold,
                ShakeIntensity = this.ShakeIntensity,
                MouthSensitivity = this.MouthSensitivity,
                MouthFrameCount = this.MouthFrameCount
            };
        }
    }

    /// <summary>
    /// Audio processing settings
    /// </summary>
    public class AudioSettings
    {
        public string InputDeviceId { get; set; } = "";
        public bool EnableReverb { get; set; } = false;
        public float ReverbMix { get; set; } = 0.3f;
        public bool EnablePitchShift { get; set; } = false;
        public float PitchShiftAmount { get; set; } = 0f;
        public float Volume { get; set; } = 1f;
        public bool EnableVirtualMic { get; set; } = false;
        
        public AudioSettings Clone()
        {
            return new AudioSettings
            {
                InputDeviceId = this.InputDeviceId,
                EnableReverb = this.EnableReverb,
                ReverbMix = this.ReverbMix,
                EnablePitchShift = this.EnablePitchShift,
                PitchShiftAmount = this.PitchShiftAmount,
                Volume = this.Volume,
                EnableVirtualMic = this.EnableVirtualMic
            };
        }
    }
}
