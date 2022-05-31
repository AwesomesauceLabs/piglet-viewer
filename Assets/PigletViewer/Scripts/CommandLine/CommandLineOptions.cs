namespace PigletViewer
{
    public class CommandLineOptions
    {
        /// <summary>
        /// <para>
        /// This option is true by default, and causes Piglet to call
        /// AnimationClip.EnsureQuaternionContinuity [1] after loading each
        /// animation clip.
        /// </para>
        /// <para>
        /// Most of the time `EnsureQuaternionContinuity` does the right thing,
        /// but in some circumstances it introduces unwanted wobble
        /// in rotation animations.
        /// </para>
        /// <para>
        /// This option is only relevant for glTF files that contain animations with
        /// rotations.
        /// </para>
        /// <para>
        /// [1]: https://docs.unity3d.com/ScriptReference/AnimationClip.EnsureQuaternionContinuity.html
        /// </para>
        /// </summary>
        public bool EnsureQuaternionContinuity;

        /// <summary>
        /// Create mipmaps during texture loading.
        /// </summary>
        public bool Mipmaps;

        /// <summary>
        /// If true, print a TSV table of profiling data
        /// to the debug log after each glTF import.
        /// </summary>
        public bool Profile;

        /// <summary>
        /// If true, exit the application immediately
        /// after running all tasks specified on
        /// the command line (e.g. --import).
        /// </summary>
        public bool Quit;

        /// <summary>
        /// Constructor. Initializes the command-line options
        /// to their default values.
        /// </summary>
        public CommandLineOptions()
        {
            EnsureQuaternionContinuity = true;
            Mipmaps = false;
            Profile = false;
            Quit = false;
        }
    }
}