#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.IO;
using NDesk.Options;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
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

            // Configure the name of the app on iPhone/iPad home screen.

            PlayerSettings.productName = "PigletViewer";

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

        /// <summary>
        /// <para>
        /// Postprocess the Unity-generated Xcode project by
        /// editing the `Info.plist` file and adding file associations for
        /// .gltf/.glb/.zip files.
        /// </para>
        /// <para>
        /// NOTE 1: Configuration of file associations in `Info.plist` is
        /// analogous to configuration of Android file associations in
        /// `Plugins/Android/AndroidManifest.xml`.
        /// </para>
        /// <para>
        /// NOTE 2: This method is based on the code example at:
        /// https://forum.unity.com/threads/how-can-you-add-items-to-the-xcode-project-targets-info-plist-using-the-xcodeapi.330574/#post-2143867
        /// </para>
        /// </summary>
        [PostProcessBuild]
        public static void ConfigureXcodePlist(BuildTarget buildTarget, string pathToBuiltProject)
        {
            if (buildTarget != BuildTarget.iOS)
                return;

            // Read contents of Xcode project's `Info.plist` file into memory.

            var plistPath = pathToBuiltProject + "/Info.plist";
            var plist = new PlistDocument();

            plist.ReadFromString(File.ReadAllText(plistPath));

            // Reference to the root element in `Info.plist`.

            var root = plist.root;

            // Tells the App Store team that our app doesn't use encryption.

            root.SetBoolean("ITSAppUsesNonExemptEncryption", false);

            // Makes the app's Documents folder visible/accessible in Finder.
            // (Select "iPhone" in left sidebar, then "Files" tab on right.)

            root.SetBoolean("UIFileSharingEnabled", true);

            // I don't know what this does, but some forum posts say it
            // is necessary in order for the app's Documents folders to
            // be visible in Finder.

            root.SetBoolean("LSSupportsOpeningDocumentsInPlace", true);

            // Specify the "Document Types" (file associations) that this
            // app can handle. This is needed for opening glTF/zip files
            // with AirDrop or from other iOS apps (e.g. Apple Mail).
            //
            // Note: Regardless of these settings, any type of file can
            // be copied to the PigletViewer iOS app in Finder, by:
            //
            // (1) Selecting "iPhone" in the left sidebar
            // (2) Selecting the "Files" tab on the right side
            // (3) Dragging-and-dropping the file(s) over the app
            // (The name of the app will be determined by the value
            // of Project Settings

            var documentTypes = root.CreateArray("CFBundleDocumentTypes");

            var gltfType = documentTypes.AddDict();
            gltfType.SetString("CFBundleTypeName", "glTF");
            gltfType.SetString("LSHandlerRank", "Default");
            gltfType.SetString("CFBundleTypeRole", "Viewer");

            var gltfContentTypes = gltfType.CreateArray("LSItemContentTypes");
            gltfContentTypes.AddString("public.gltf");

            var zipType = documentTypes.AddDict();
            zipType.SetString("CFBundleTypeName", "zip");
            zipType.SetString("LSHandlerRank", "Default");
            zipType.SetString("CFBundleTypeRole", "Viewer");

            var zipContentTypes = zipType.CreateArray("LSItemContentTypes");
            zipContentTypes.AddString("com.pkware.zip-archive");

            // Declare an "Imported Document Type" for glTF, which
            // specifies some metadata about the glTF format
            // (e.g. possible file extensions). We need to explicitly
            // define the glTF file type here because it is not one
            // of the standard file types recognized by Apple
            // (as listed at [1]).
            //
            // For further info about the settings configured below, see
            // "Defining File and Data Types for Your App" from the Apple
            // Developer Documentation [2].
            //
            // Side note: The only difference between "Imported Document Types"
            // and "Exported Document Types" is that "Exported Document Types"
            // are intended for document types that are invented by the
            // app creator, whereas "Imported Document Types" are intended
            // for document types that invented by third parties (e.g. public
            // standards like glTF). See [3] for further discussion.
            //
            // [1]: https://developer.apple.com/library/archive/documentation/Miscellaneous/Reference/UTIRef/Articles/System-DeclaredUniformTypeIdentifiers.html
            // [2]: https://developer.apple.com/documentation/uniformtypeidentifiers/defining_file_and_data_types_for_your_app
            // [3]: https://stackoverflow.com/questions/24958021/document-types-vs-exported-and-imported-utis

            var importedTypes = root.CreateArray("UTImportedTypeDeclarations");
            var importedType = importedTypes.AddDict();

            importedType.SetString("UTTypeIdentifier", "public.gltf");

            var conformsTo = importedType.CreateArray("UTTypeConformsTo");
            conformsTo.AddString("public.data");
            conformsTo.AddString("public.3d-content");

            importedType.SetString("UTTypeDescription", "glTF");

            var typeTags = importedType.CreateDict("UTTypeTagSpecification");
            var extensions = typeTags.CreateArray("public.filename-extension");
            extensions.AddString("glb");
            extensions.AddString("gltf");

            // Write changes back to `Info.plist`.

            File.WriteAllText(plistPath, plist.WriteToString());
        }
    }
}
#endif