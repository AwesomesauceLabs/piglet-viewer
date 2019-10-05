
namespace UnityGLTF {
    public class GLTFRuntimeImporter : GLTFImporter
    {
        public GLTFRuntimeImporter(ProgressCallback progressCallback,
            RefreshWindow finishCallback=null)
            : base(progressCallback, finishCallback)
        {}
    }
}