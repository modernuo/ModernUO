using System;
using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    public class MapPatchesTests : IClassFixture<ServerFixture>
    {
        [Theory]
        [InlineData(ProtocolChanges.Version500a, ClientFlags.Malas | ClientFlags.Trammel | ClientFlags.Felucca)]
        [InlineData(ProtocolChanges.Version7090, ClientFlags.TerMur | ClientFlags.Trammel | ClientFlags.Felucca)]
        public void TestMapPatches(ProtocolChanges protocolChanges, ClientFlags flags)
        {
            var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges = protocolChanges;
            ns.Flags = flags;

            var expected = ns.ProtocolChanges >= ProtocolChanges.Version6000 ? Span<byte>.Empty : new MapPatches().Compile();

            ns.SendMapPatches();

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestInvalidMapEnable()
        {
            var expected = new InvalidMapEnable().Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendInvalidMap();

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData("Felucca")]
        [InlineData("Malas")]
        public void TestMapChange(string mapName)
        {
            var map = Map.Parse(mapName);
            var expected = new MapChange(map).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMapChange(map);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }
    }
}
