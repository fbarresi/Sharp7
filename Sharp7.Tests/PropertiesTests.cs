using Shouldly;
using Xunit;

namespace Sharp7.Tests
{
    public class PropertiesTests
    {
        [Fact]
        public void PlcHasNoName()
        {
            var client = new S7Client();
            client.Name.ShouldBeNull();
            string.IsNullOrEmpty(client.Name).ShouldBeTrue();
        }

        [Fact]
        public void PlcHasAName()
        {
            var name = "test";
            var client = new S7Client(name);
            client.Name.ShouldNotBeNull();
            string.IsNullOrEmpty(client.Name).ShouldBeFalse();
            client.Name.ShouldBe(name);
        }
        
        [Fact]
        public void PlcHasIp()
        {
            var ip = "10.10.10.10";
            var client = new S7Client();
            client.SetConnectionParams(ip, 0, 0);
            client.PLCIpAddress.ShouldNotBeNull();
            string.IsNullOrEmpty(client.PLCIpAddress).ShouldBeFalse();
            client.PLCIpAddress.ShouldBe(ip);
        }
        
        [Fact]
        public void PlcHasNoIp()
        {
            var client = new S7Client();
            client.PLCIpAddress.ShouldBeNull();
            string.IsNullOrEmpty(client.PLCIpAddress).ShouldBeTrue();
        }
        
        [Fact]
        public void PlcToString()
        {
            var client = new S7Client();
            client.ToString().ShouldBe("PLC @0.0.0.0");
        }
        
        [Fact]
        public void PlcToStringWithName()
        {
            var client = new S7Client("Test");
            client.ToString().ShouldBe("PLC Test@0.0.0.0");
        }
        
        [Fact]
        public void PlcToStringWithNameAndIp()
        {
            var client = new S7Client("Test");
            client.SetConnectionParams("1.2.3.4", 0, 0);
            client.ToString().ShouldBe("PLC Test@1.2.3.4");
        }
    }
}