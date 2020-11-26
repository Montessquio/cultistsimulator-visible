
using Noon;
using OrbCreationExtensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Assets.TabletopUi.Scripts.Infrastructure.Modding
{
    public class SubscribedStorefrontMod
    {
        public string ModRootFolder { get; set; }
    }

    public enum ModInstallType {Unknown=0,Local=1,SteamWorkshop=2}

    public class Mod
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Author { get; set; }

        public Version Version { get; set; }

        public string Description { get; set; }

        public Sprite PreviewImage { get; set; }

        public string PreviewImageFilePath
        {
            get { return Path.Combine(ModRootFolder, NoonConstants.WORKSHOP_PREVIEW_IMAGE_FILE_NAME); }
        }

        public string DescriptionLong { get; set; }

        public List<Dependency> Dependencies { get; set; }

        public Dictionary<string, List<Hashtable>> Contents { get; set; }

        public Dictionary<string, Sprite> Images { get; set; }
        
        public bool Enabled { get; set; }

        public string ModRootFolder { get; set; }

        public string PublishedFileIdPath {get {return Path.Combine(ModRootFolder,
                NoonConstants.WORKSHOP_ITEM_PUBLISHED_ID_FILE_NAME);
            }
        }

        public string ContentFolder { get; set; }
        public string LocFolder { get; set; }

        public string CataloguingLog { get; set; }


        private const string DependencyPattern = @"^\s*(\w+)(?:\s*(<=|<|>=|>|==)\s*([\d.]+))?\s*$";

        public virtual bool IsValid { get; private set; }
    

        public ModInstallType ModInstallType { get; set; }


        public Mod(
            string id, string modRootFolder)
        {
            Id = id;
            ModRootFolder = modRootFolder;

            ModInstallType = ModInstallType.Unknown;

            Dependencies =  new List<Dependency>();
            Contents = new Dictionary<string, List<Hashtable>>();
            Images = new Dictionary<string, Sprite>();
            Enabled = false;
            IsValid = true;
        }

        public void MarkAsInvalid()
        {
            IsValid=false;
        }

        public HashSet<string> PopulateFromSynopsis(Hashtable manifest)
        {
            var errors = new HashSet<string>();

            Name = manifest.GetStringOrLogError("name", errors);
            Author = manifest.GetStringOrLogError("author", errors);
            try
            {
                Version = new Version(manifest.GetStringOrLogError("version", errors));
            }
            catch
            {
                errors.Add("Invalid format for 'version'");
            }
            Description = manifest.GetStringOrLogError("description", errors);
            DescriptionLong = manifest.GetString("description");

            // Validate the dependencies
            var dependenciesData = manifest.GetArrayList("dependencies");
            if (dependenciesData != null)
            {
                Dependencies = new List<Dependency>();
                foreach (var dependencySpecification in dependenciesData)
                {
                    if (!(dependencySpecification is string))
                    {
                        errors.Add("Invalid dependency specification type, should be string");
                        MarkAsInvalid();
                        continue;
                    }
                    var regex = new Regex(DependencyPattern);
                    var match = regex.Match((string) dependencySpecification);
                    if (match.Success)
                    {
                        var invalidDependency = false;
                        var dependency = new Dependency {ModId = match.Groups[1].Value};

                        // Validate the version, if specified
                        if (match.Groups.Count > 2
                            && match.Groups[2].Value.Length > 0 && match.Groups[3].Value.Length > 0)
                        {
                            switch (match.Groups[2].Value)
                            {
                                case "<":
                                    dependency.VersionOperator = DependencyOperator.LessThan;
                                    break;
                                case "<=":
                                    dependency.VersionOperator = DependencyOperator.LessThanOrEqual;
                                    break;
                                case ">":
                                    dependency.VersionOperator = DependencyOperator.GreaterThan;
                                    break;
                                case ">=":
                                    dependency.VersionOperator = DependencyOperator.GreaterThanOrEqual;
                                    break;
                                case "==":
                                    dependency.VersionOperator = DependencyOperator.Equal;
                                    break;
                                default:
                                    invalidDependency = true;
                                    errors.Add(
                                        "Invalid version operator '" + match.Groups[2].Value + "' for '" + dependency.ModId + "'");
                                    break;
                            }
                            try
                            {
                                dependency.Version = new Version(match.Groups[3].Value);
                            }
                            catch
                            {
                                errors.Add(
                                    "Invalid version number '" + match.Groups[3].Value + "' for '" + dependency.ModId + "'");
                                invalidDependency = true;
                                MarkAsInvalid();
                            }
                        }

                        if (!invalidDependency)
                        {
                            Dependencies.Add(dependency);
                        }
                    }
                    else
                    {
                        errors.Add(
                            "Invalid dependency specification format '" + dependencySpecification + "'");
                        MarkAsInvalid();
                    }
                }
            }
            else if (manifest.ContainsKey("dependencies"))
            {
                errors.Add("Invalid type for 'dependencies', should be array");
                MarkAsInvalid();
            }

            return errors;
        }


        public bool LoadImage(string imageFilePath)
        {
            //need to determine the subfolder tidily, to avoid absolute path problem for image key

            Sprite spriteToLoad;

            
            //we don't want the absolute root in here, because later we'll match it against relative locations for core images
            string relativePath = imageFilePath.Replace(ModRootFolder, string.Empty);

            
            string relativePathWithoutFileExtension = relativePath.Replace(Path.GetFileName(relativePath),
                Path.GetFileNameWithoutExtension(relativePath));

            string relativePathWithoutLeadingSlash = relativePathWithoutFileExtension.Remove(0, 1);

            try
            {
                spriteToLoad = LoadSprite(imageFilePath);
            }
            catch
            {
                NoonUtility.Log(
                    "Invalid image file '" + imageFilePath + "'",
                    2);
                return false;
            }

            Images.Add(relativePathWithoutLeadingSlash, spriteToLoad);

            return true;
        }

        private Sprite LoadSprite(string imagePath)
        {
            Sprite sprite;

            if (!File.Exists(imagePath))
                return null;

            var fileData = File.ReadAllBytes(imagePath);

            // Try to load the image data into a sprite


            var texture = new Texture2D(2, 2);
            texture.LoadImage(fileData);
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = 9;
            texture.mipMapBias = (float)-0.5;
            texture.Apply();
            sprite = Sprite.Create(
                texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            return sprite;
        }


    }
}
