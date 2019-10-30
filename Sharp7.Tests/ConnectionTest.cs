using System;
using Shouldly;
using Xunit;

namespace Sharp7.Tests
{
    public class ConnectionTest : ServerClientTestBase
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

    }
}
