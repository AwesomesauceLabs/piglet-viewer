using System;
using System.Collections.Generic;
using NDesk.Options;
using UnityEditor;
using UnityEngine;

namespace PigletViewer
{
    /// <summary>
    /// Provides methods for performing automated iOS builds.
    /// </summary>
    public class iOSBuilder
    {
        /// <summary>
        /// <para>
        /// Method to run an automated iOS build from the command line.
        /// The output of the build is an Xcode project located
        /// in `build/ios` under the project root.
        /// </para>
        /// <para>
        /// To run the build from the command line, switch to the
        /// project root folder and run:
        /// </para>
        /// <para>
        /// Unity.exe -logFile /dev/stdout -executeMethod
        /// PigletViewer.iOSBuilder.Build -batchmode -quit
        /// -- --apple-developer-team-id AB123C45DE
        /// </para>
        /// <para>
        /// NOTE 1: The bare `--` separates standard Unity command line
        /// options from options that are passed to this method.
        /// </para>
        /// <para>
        /// NOTE 2: You can get the value for `--apple-developer-team-id`
        /// by: (1) logging into your Apple Developer account, (2)
        /// selecting "Certificates, Identifiers and Profiles" in the left sidebar,
        /// and (3) copying the alphanumeric ID shown in the top right corner
        /// of the screen.
        /// </para>
        /// </summary>
        [MenuItem("PigletViewer/Build/iOS")]
        public static void Build()
        {
            const string buildPath = "build/ios";

            // Parse command line options using NDesk.Options library.

            var commandLineOptions = new OptionSet()
            {
                {
                    "t|apple-developer-team-id=",
                    "alphanumeric team id from Apple Developer account page",
                    id =>
                    {
                        PlayerSettings.iOS.appleDeveloperTeamID = id;
                        PlayerSettings.iOS.appleEnableAutomaticSigning = true;
                    }
                }
            };

            // Remove all command-line args before "--" separator.
            //
            // Args before "--" are standard Unity options
            // (e.g. -projectPath), while following "--" (if any)
            // are options for the iOS build (e.g. --apple-developer-team-id).

            var args = new Queue<string>(Environment.GetCommandLineArgs());
            while (args.Count > 0 && args.Dequeue() != "--") { }

            commandLineOptions.Parse(args);

            // Run the build. (Generates Xcode project in `build/ios` under project root).

            var buildOptions = new BuildPlayerOptions
            {
                target = BuildTarget.iOS,
                locationPathName = buildPath,
                scenes = new [] { "Assets/PigletViewer/Scenes/MainScene.unity" }
            };

            BuildPipeline.BuildPlayer(buildOptions);

            Debug.Log($"Successfully built Xcode project in {buildPath}!");
        }
    }
}
