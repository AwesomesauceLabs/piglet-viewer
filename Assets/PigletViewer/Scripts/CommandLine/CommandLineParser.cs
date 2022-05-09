using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NDesk.Options;
using Piglet;
using UnityEngine;
using UnityEngine.Networking;

namespace PigletViewer
{
    public class CommandLineParser
    {
        /// <summary>
        /// <para>
        /// Parse command-line options using NDesk.Options library.
        /// </para>
        /// <para>
        /// The user may specify default command-line options
        /// in a file called `Resources/piglet-viewer-args.txt`,
        /// and then any options specified on the command line
        /// will be appended to these.
        /// </para>
        /// </summary>
        /// <param name="options">
        /// Class that holds the state of the command-line options
        /// as they parsed. Note: This parameter is effectively a
        /// `ref` parameter, but we cannot annotate as such
        /// because coroutines are not allowed to have `ref` parameters.
        /// For further explanation, see:
        /// https://stackoverflow.com/questions/999020/why-cant-iterator-methods-take-either-ref-or-out-parameters
        /// </param>
        public static IEnumerator ParseCommandLineOptions(CommandLineOptions options)
        {
            var optionSet = new OptionSet
            {
                {
                    "b|button=",
                    "show button with {LABEL} and continue when user clicks it",
                     label => GameManager.Instance.Tasks.Add(GameManager.ShowPromptButton(label))
                },
                {
                    "i|import=",
                    "import glTF file from {URI} (filename or HTTP URL)",
                    uri => GameManager.Instance.QueueImport(uri)
                },
                {
                    "I|import-streaming-asset=",
                    "import glTF file using path relative to StreamingAssets",
                    uri =>
                    {
                        uri = Path.Combine(Application.streamingAssetsPath, uri);
                        GameManager.Instance.QueueImport(uri);
                    }
                },
                {
                    "m|log-message=",
                    "print a message to the Unity log",
                    message =>  GameManager.Instance.Tasks.Add(
                        GameManager.LogMessage(message))
                },
                {
                    "M|mipmaps",
                    "create mipmaps during texture loading",
                    enable => options.Mipmaps = enable != null
                },
                {
                    "p|profile",
                    "profile glTF imports and log results in TSV format",
                    enable => options.Profile = enable != null
                },
                {
                    "q|quit",
                    "exit program after performing all command-line actions",
                    enable => options.Quit = enable != null
                },
                {
                    "s|sleep=",
                    "sleep for {SECONDS} seconds",
                    seconds => GameManager.Instance.Tasks.Add(
                        GameManager.Sleep(float.Parse(seconds)))
                }
            };

            // Read default command-line options from
            // `StreamingAssets/piglet-viewer-args.txt`, if that file exists.
            // This is useful on platforms where invoking the Unity player
            // with custom command-line options is either inconvenient or
            // impossible (e.g. Android, WebGL).

            var args = new List<string>();

            IEnumerable<string> defaultArgs = null;

            foreach (var result in ReadDefaultCommandLineArgs())
            {
                defaultArgs = result;
                yield return null;
            }

            if (defaultArgs != null)
                args.AddRange(defaultArgs);

            // Read options specified on the command-line.
            //
            // We remove all command-line args before "--"
            // separator, because those are built-in options for the Unity
            // Editor/Player (e.g. -projectPath), whereas the args
            // after "--" are PigletViewer options.

            var pigletViewerArgs = new Queue<string>(Environment.GetCommandLineArgs());

            while (pigletViewerArgs.Count > 0
                && pigletViewerArgs.Dequeue() != "--") {}

            args.AddRange(pigletViewerArgs);

            // Parse command-line options and invoke handlers.

            optionSet.Parse(args);

            // If no command line options were specified,
            // load the default "Sir Piggleston" model.

            if (args.Count == 0)
            {
                GameManager.Instance.QueueImport(Path.Combine(
                    Application.streamingAssetsPath, "piggleston.glb"));
            }
        }

        /// <summary>
        /// Read default command-line args from
        /// StreamingAssets/piglet-viewer-args.txt, or
        /// return null if that file does not exist.
        /// </summary>
        private static IEnumerable<IEnumerable<string>> ReadDefaultCommandLineArgs()
        {
            var uri = new Uri(Path.Combine(
                Application.streamingAssetsPath, "piglet-viewer-args.txt"));

            var request = UnityWebRequest.Get(uri);
            request.SendWebRequest();

            while (!request.isDone)
                yield return null;

            // Note: The existence of `StreamingAssets/piglet-viewer-args.txt`
            // to specify default command-line args is optional, so HTTP 404
            // is not a problem. We return null to indicate that the file/URI
            // does not exist.

            if (request.responseCode == 404)
            {
                yield return null;
                yield break;
            }

			if (request.HasError())
			{
				throw new Exception(string.Format(
					"failed to read from {0}: {1}",
					uri, request.error));
			}

            // Get the text from the response and split on whitespace.

            yield return request.downloadHandler.text.Split(null);
        }

    }
}
