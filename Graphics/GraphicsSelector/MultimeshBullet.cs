using Godot;

namespace Blastula.Graphics
{
    /// <summary>
    /// MultiMeshInstance2D used to render bullets.
    /// </summary>
    public unsafe partial class MultimeshBullet : MultiMeshInstance2D
    {
        /// <summary>
        /// Setting the buffer indirectly for the multimesh is pretty slow! But at the moment I don't see a way around it.
        /// </summary>
        public void SetBuffer(float[] buf, int visibleLength, int stride)
        {
            Multimesh.InstanceCount = buf.Length / stride;
            Multimesh.VisibleInstanceCount = visibleLength;
            Multimesh.Buffer = buf;
        }

        public override void _EnterTree()
        {
            base._EnterTree();
            Multimesh = Multimesh.Duplicate() as MultiMesh;
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
        }
    }
}
