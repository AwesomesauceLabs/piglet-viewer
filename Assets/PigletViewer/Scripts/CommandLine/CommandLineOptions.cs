namespace PigletViewer
{
    public class CommandLineOptions
    {
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
    }
}