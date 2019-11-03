using System;
using Shouldly;
using Xunit;

namespace Sharp7.Tests
{
    public class ClientTest : ServerClientTestBase
    {

        [Fact]
        public void ClientIsNotNull()
        {
            Client.ShouldNotBeNull();
        }

        [Fact]
        public void ServerIsNotNull()
        {
            Server.ShouldNotBeNull();
        }

        [Fact]
        public void TestTimeout()
        {
            Client.ConnTimeout.ShouldBe(2000);
        }

        [Fact]
        public void TestConnected()
        {
            Client.Connected.ShouldBe(true);
        }

        [Fact]
        public void TestPort()
        {
            Client.PLCPort.ShouldBe(102);
        }

        [Fact]
        public void TestReadDb()
        {
            var bytes = new byte[] { 1, 2, 3 };
            var index = 3;
            Server.RegisterArea(S7Server.SrvAreaDB, index, ref bytes, bytes.Length);

            var buffer = new byte[bytes.Length];
            var rc = Client.DBRead(index, 0, bytes.Length, buffer);

            //test
            rc.ShouldBe(0);
            buffer.ShouldBe(bytes);
        }

        [Fact]
        public void TestWriteDb()
        {
            var bytes = new byte[3];
            var index = 3;
            Server.RegisterArea(S7Server.SrvAreaDB, index, ref bytes, bytes.Length);

            var buffer = new byte[] { 1, 2, 3 };
            var rc = Client.DBWrite(index, 0, bytes.Length, buffer);

            //test
            rc.ShouldBe(0);
            buffer.ShouldBe(bytes);
        }
    }
}
