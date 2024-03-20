using Godot;

namespace Blastula.Graphics
{
    public unsafe partial class MultimeshBullet : MultiMeshInstance2D
    {
        public int GetBufferCount()
        {
            return Multimesh.InstanceCount * 8;
        }

        // This memcpys your buffer to the multimesh buffer. Slow! But there's no other way yet.
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
