namespace Server.Items
{
    [EmbeddedSerializable(0)]
    public partial class BBPost
    {
        [SerializableParent]
        private BulletinBoard _bulletinBoard;

        [SerializableField(0)]
        private BBPost _thread; // Parent post

        [SerializableField(1)]
        private BBPost _prev;

        [SerializableField(2)]
        private BBPost _next;

        [SerializableField(3)]
        private BBPost _child;
    }
}
