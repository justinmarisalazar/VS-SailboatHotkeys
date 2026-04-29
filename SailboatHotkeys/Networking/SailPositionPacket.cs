using ProtoBuf;

namespace SailboatHotkeys.Networking
{
    [ProtoContract]
    public class SailPositionPacket
    {
        [ProtoMember(1)]
        public long BoatEntityId;

        [ProtoMember(2)]
        public int TargetSailPosition;
    }
}
