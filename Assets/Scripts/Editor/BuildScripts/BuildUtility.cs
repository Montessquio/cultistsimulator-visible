﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SecretHistories.Entities;
using SecretHistories.Editor.BuildScripts;
using Galaxy;

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.WSA;
using Application = UnityEngine.Application;
using Storefront = TabletopUi.Scripts.Services.Storefront;

namespace SecretHistories.Utility
{

    public static class BuildUtility
    {

        private const string BUILD_DIR_PREFIX = "csunity-";
        private const string DEFAULT_BUILD_ROOT = "c:\\builds\\cs\\build_outputs";
        private const string SCENES_FOLDER = "assets/TabletopUi/Scenes";
        private const string CONST_DLC_FOLDER = "DLC";
        private const string CONST_PERPETUALEDITION_DLCTITLE = "PERPETUAL";
        private const string CONST_PERPETUALEDITION_SEMPER_RELATIVE_PATH_TO_FILE = "StreamingAssets\\edition\\semper.txt";
        private const char CONST_NAME_SEPARATOR_CHAR = '_';


        private static readonly string[] ContentTypes =
        {
            "decks",
            "elements",
            "endings",
            "legacies",
            "recipes",
            "verbs"
        };

        private static readonly string[] Locales =
        {
            null,  // Default locale (en)
            "ru",
            "zh-hans"
        };

        [MenuItem("Tools/Build (ALL)")]
        public static void PerformAllBuilds()
        {
            PerformWindowsBuild();
            PerformOsxBuild();
            PerformLinuxBuild();
        }

        [MenuItem("Tools/Build (Windows)")]
        public static void PerformWindowsBuild()
        {
            PerformBuild(BuildTarget.StandaloneWindows);
        }

        [MenuItem("Tools/Build (OSX)")]
        public static void PerformOsxBuild()
        {
            PerformBuild(BuildTarget.StandaloneOSX);
        }

        [MenuItem("Tools/Build (Linux)")]
        public static void PerformLinuxBuild()
        {
            PerformBuild(BuildTarget.StandaloneLinux64);
        }


        [MenuItem("Tools/Make Distribution (ALL)")]
        public static void MakeAllDistributions()
        {
            MakeSteamDistribution();
            MakeGogDistribution();
            MakeHumbleDistribution();

        }

        [MenuItem("Tools/Make Distribution (Steam)")]
        public static void MakeSteamDistribution()
        {
            
            BuildEnvironment env = new BuildEnvironment(DEFAULT_BUILD_ROOT);

            List<BuildOS> OSs=new List<BuildOS>();
            List<BuildProduct> products=new List<BuildProduct>();

            BuildOS osw=new BuildOS(BuildTarget.StandaloneWindows64);
            BuildOS oso=new BuildOS(BuildTarget.StandaloneOSX);
            BuildOS osl=new BuildOS(BuildTarget.StandaloneLinux64);
            OSs.Add(osw);
            OSs.Add(oso);
            OSs.Add(osl);


            BuildProduct vanillaEdition=new BuildProduct(env,Product.VANILLA,false);
            BuildProduct perpetualDLC=new BuildProduct(env,Product.PERPETUAL,true);
            BuildProduct DancerDLC=new BuildProduct(env,Product.DANCER,true);
            BuildProduct PriestDLC=new BuildProduct(env,Product.PRIEST,true);
            BuildProduct GhoulDLC=new BuildProduct(env,Product.GHOUL,true);
             BuildProduct ExileDLC=new BuildProduct(env,Product.EXILE,true);

            products.Add(vanillaEdition);
            products.Add(perpetualDLC);
            products.Add(DancerDLC);
            products.Add(PriestDLC);
            products.Add(GhoulDLC);
            products.Add(ExileDLC);

            BuildStorefront buildStorefront=new BuildStorefront(Storefront.Steam,OSs,products);

            MakeDistribution(buildStorefront);


        }

        [MenuItem("Tools/Make Distribution (Gog)")]
        public static void MakeGogDistribution()
        {
            BuildEnvironment env = new BuildEnvironment(DEFAULT_BUILD_ROOT);

            List<BuildOS> OSs=new List<BuildOS>();
            List<BuildProduct> products=new List<BuildProduct>();

            BuildOS osw=new BuildOS(BuildTarget.StandaloneWindows64);
            BuildOS oso=new BuildOS(BuildTarget.StandaloneOSX);
            BuildOS osl=new BuildOS(BuildTarget.StandaloneLinux64);
            OSs.Add(osw);
            OSs.Add(oso);
            OSs.Add(osl);


            BuildProduct vanillaEdition=new BuildProduct(env,Product.VANILLA,false);
            BuildProduct perpetualDLC=new BuildProduct(env,Product.PERPETUAL,true);
            BuildProduct DancerDLC=new BuildProduct(env,Product.DANCER,true);
            BuildProduct PriestDLC=new BuildProduct(env,Product.PRIEST,true);
            BuildProduct GhoulDLC=new BuildProduct(env,Product.GHOUL,true);
            BuildProduct ExileDLC=new BuildProduct(env,Product.EXILE,true);

            products.Add(vanillaEdition);
            products.Add(perpetualDLC);
            products.Add(DancerDLC);
            products.Add(PriestDLC);
            products.Add(GhoulDLC);
            products.Add(ExileDLC);

            BuildStorefront buildStorefront=new BuildStorefront(Storefront.Gog,OSs,products);

            MakeDistribution(buildStorefront);

        }

        [MenuItem("Tools/Make Distribution (Humble)")]
        public static void MakeHumbleDistribution()
        {
            BuildEnvironment env = new BuildEnvironment(DEFAULT_BUILD_ROOT);

            List<BuildOS> OSs = new List<BuildOS>();
            List<BuildProduct> products = new List<BuildProduct>();

            BuildOS osw = new BuildOS(BuildTarget.StandaloneWindows64);
            OSs.Add(osw);


            BuildProduct vanillaEdition = new BuildProduct(env, Product.VANILLA, false);
            BuildProduct perpetualAllDlc = new BuildProduct(env, Product.PERPETUAL_ALLDLC, false);
            BuildProduct DancerDLC = new BuildProduct(env, Product.DANCER, true);
            BuildProduct PriestDLC = new BuildProduct(env, Product.PRIEST, true);
            BuildProduct GhoulDLC = new BuildProduct(env, Product.GHOUL, true);
            BuildProduct ExileDLC = new BuildProduct(env, Product.EXILE, true);

            products.Add(vanillaEdition);
            products.Add(perpetualAllDlc);
            products.Add(DancerDLC);
            products.Add(PriestDLC);
            products.Add(GhoulDLC);
            products.Add(ExileDLC);

            BuildStorefront buildStorefront = new BuildStorefront(Storefront.Humble, OSs, products);

            MakeDistribution(buildStorefront);

        }

        private static void PerformBuild(BuildTarget target)
        {
            var env=new BuildEnvironment(DEFAULT_BUILD_ROOT);


            var os=new BuildOS(target);
            var product=new BuildProduct(env,Product.VANILLA,false);

            env.DeleteProductWithOSBuildPath(product,os);


            if(!Directory.Exists(SCENES_FOLDER))
                throw new ApplicationException($"Can't find scenes folder {SCENES_FOLDER}: exiting");

            List<string> sceneFilesInFolder = new List<string>();
            
            
            sceneFilesInFolder.AddRange(Directory.GetFiles(SCENES_FOLDER).ToList().FindAll(f=>f.EndsWith(".unity")));
            foreach(var f in sceneFilesInFolder)
            {
                Log("Adding scene to build: "+ f);
            }

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                target = target,
                locationPathName = NoonUtility.JoinPaths(env.GetProductWithOSBuildPath(product, os), os.ExeName),
                scenes = sceneFilesInFolder.ToArray()
        };
            Log("Building " + target + " version to build directory: " + buildPlayerOptions.locationPathName);            

            BuildPipeline.BuildPlayer(buildPlayerOptions);
            
        }



        [PostProcessBuild]
        public static void OnBuildComplete(BuildTarget target, string pathToBuiltProject)
        {

            if (target != BuildTarget.StandaloneWindows
                && target != BuildTarget.StandaloneWindows64
                && target != BuildTarget.StandaloneOSX
                && target != BuildTarget.StandaloneLinux64)
            {
                return;
            }

            PostBuildFileTasks(target, GetParentDirectory(pathToBuiltProject), new BuildOS(target));
        }

        private static void PostBuildFileTasks(BuildTarget buildTarget, string pathToBuiltProject,BuildOS os)
        {
            BuildEnvironment env=new BuildEnvironment(DEFAULT_BUILD_ROOT);

            CopyStorefrontLibraries(buildTarget, pathToBuiltProject);
            AddVersionNumber(pathToBuiltProject);

            string perpetualEditionsFolderPath =NoonUtility.JoinPaths(env.GetProductWithOSBuildPath(new BuildProduct(env,Product.PERPETUAL_ALLDLC,false),os));
            BuildPerpetualEdition(pathToBuiltProject,perpetualEditionsFolderPath,os);

            string dlcFolderPath=NoonUtility.JoinPaths(env.GetBaseBuildsPath(), CONST_DLC_FOLDER);

            ExtractDLCFilesFromBaseBuilds(pathToBuiltProject,dlcFolderPath, os);

          //  RunContentTests(buildTarget, builtAtPath);



        }

        private static void MakeDistribution(BuildStorefront storefront)
        {
            BuildEnvironment env = new BuildEnvironment(DEFAULT_BUILD_ROOT);

            var distributions = storefront.GetDistributionsForStorefront(env);

            foreach(var d in distributions)
                d.CopyFilesFromEnvironment(env);


        }

        private static void BuildPerpetualEdition(string from,string to,BuildOS os)
        {
            // Set up the Perpetual Edition, with all its DLC

            Log("Copying whole project with DLC from " + from + " to " + to);

            if (Directory.Exists(to))
                Directory.Delete(to, true);
            NoonUtility.CopyDirectoryRecursively(from, to);


            string semperPath = NoonUtility.JoinPaths(
                to,
                os.GetDataFolderPath(),
                CONST_PERPETUALEDITION_SEMPER_RELATIVE_PATH_TO_FILE);

            WriteSemperFile(semperPath);
        }


        private static void ExtractDLCFilesFromBaseBuilds(string from,string to, BuildOS os)
        {
// Take the DLCs out of the base edition and into their own directories

   
            foreach (var dlcContentType in ContentTypes)
            foreach (var locale in Locales)
                MoveDlcContent(from, to,os, dlcContentType, locale);
        }



        private static void CopyStorefrontLibraries(BuildTarget target, string builtAtPath)
        {
            Log("Copying Steam libraries to build: " + builtAtPath);
            CopySteamLibraries.Copy(target, builtAtPath);
            Log("Copying Galaxy libraries to build: " + builtAtPath);
            CopyGalaxyLibraries.Copy(target, builtAtPath);
        }

        private static void MoveDlcContent(string from, string to, BuildOS os, string contentOfType, string locale)
        {
            var baseEditionContentPath = NoonUtility.JoinPaths(from,os.GetCoreContentPath(locale),contentOfType);
            Log("Searching for DLC in " + baseEditionContentPath);

            var contentFiles = Directory.GetFiles(baseEditionContentPath).ToList().Where(f => f.EndsWith(".json"));
            string dlcTitleLastLoop =
                "This is a dummy string: whenever it's a new title, we want to create a new directory, but we don't want to recreate it every loop for the same title or we lose previous files of that content type.";
            foreach (var contentFilePath in contentFiles)
            {

                int dlcMarkerIndex = contentFilePath.IndexOf(CONST_DLC_FOLDER + CONST_NAME_SEPARATOR_CHAR, StringComparison.Ordinal);

                // Does it begin with "DLC_"?
                if (dlcMarkerIndex <= -1)
                    continue;

                // Extract the DLC title so it can be moved to the appropriate subdirectory
                string dlcFilenameWithoutPath = contentFilePath.Substring(dlcMarkerIndex);
                Log("DLC file found: " + dlcFilenameWithoutPath);
                string dlcTitle = dlcFilenameWithoutPath.Split(CONST_NAME_SEPARATOR_CHAR)[1];
                if (string.IsNullOrEmpty(dlcTitle))
                    throw new ApplicationException("Couldn't find DLC title for file " + contentFilePath);

                //get the DLC destination location (../DLC/[title]/[platform]/[datafolder]/StreamingAssets/content/core/[contentOfType]/[thatfile]
                string dlcDestinationDir =NoonUtility.JoinPaths(to,dlcTitle, os.GetRelativePath(), os.GetCoreContentPath(locale), contentOfType);

                string dlcFileDestinationPath = NoonUtility.JoinPaths(dlcDestinationDir, dlcFilenameWithoutPath);

                if (dlcTitle != dlcTitleLastLoop)
                if (Directory.Exists(dlcDestinationDir))
                {
                    Directory.Delete(dlcDestinationDir, true);
                    Log("Removing old directory: " + dlcDestinationDir);
                }
                Log("Creating directory: " + dlcDestinationDir);
                Directory.CreateDirectory(dlcDestinationDir);

                Log("Moving file " + contentFilePath + " to " + dlcFileDestinationPath);
                File.Move(contentFilePath, dlcFileDestinationPath);

                dlcTitleLastLoop = dlcTitle;
            }

            // Create the Perpetual Edition DLC
            string semperPath = NoonUtility.JoinPaths(
                to,
                CONST_PERPETUALEDITION_DLCTITLE,
                os.GetRelativePath(),
                os.GetDataFolderPath(),
                CONST_PERPETUALEDITION_SEMPER_RELATIVE_PATH_TO_FILE);

            WriteSemperFile(semperPath);
        }

        private static void WriteSemperFile(string semperPath)
        {
            //semper.txt is a dumbfile that activates the PERPETUAL EDITION menu banner.
            //This needs to be created as a tiny piece of DLC, but also injected into the /edition folder of the Perpetual Edition build.

            string semperDirPath = Path.GetDirectoryName(semperPath);
            if (semperDirPath != null && !Directory.Exists(semperDirPath))
            {
                Directory.CreateDirectory(semperDirPath);
            }

            File.WriteAllText(semperPath, string.Empty);
        }



        private static void AddVersionNumber(string exeFolder)
        {
            string versionPath = NoonUtility.JoinPaths(exeFolder, "version.txt");
            Log("Writing version to " + versionPath);
            File.WriteAllText(versionPath, new VersionNumber(Application.version).ToString());
        }








        public static void Log(string message)
        {
            var oldStackTraceLogType = Application.GetStackTraceLogType(LogType.Log);
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Debug.Log("CS>>>>> " + message);
            Application.SetStackTraceLogType(LogType.Log, oldStackTraceLogType);
        }



        private static string GetParentDirectory(string path)
        {
            return Path.GetFullPath(Path.Combine(path, @"../"));
        }

 

    }
}
