using ProtoBuf;

namespace SailboatHotkeys.Networking
{
    [ProtoContract]
    public class SwitchSeatPacket
    {
        [ProtoMember(1)]
        public long BoatEntityId;

        [ProtoMember(2)]
        public string TargetSeatId;
    }
}
