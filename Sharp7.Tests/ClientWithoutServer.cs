using System;
using System.Linq;
using Shouldly;
using Xunit;

namespace Sharp7.Tests
{
    public class ClientWithoutServer
    {
        private S7Client client;

        public ClientWithoutServer()
        {
            client = new S7Client();
        }

        public new void Dispose()
        {
            client.Disconnect();
        }

        [Fact]
        public void CannotConnectTest()
        {
            var rc = client.ConnectTo("127.0.1.2", 0, 2);
            rc.ShouldBe(Sharp7.S7Consts.errTCPConnectionFailed);
        }
    }
}