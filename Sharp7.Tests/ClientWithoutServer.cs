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

        [Fact]
        public void GetLastErrorTest()
        {
            var rc = client.LastError();
            rc.ShouldBe(0);
        }

        [Fact]
        public void GetRequestedPduTest()
        {
            var rc = client.RequestedPduLength();
            rc.ShouldBe(480);
            client.PduSizeRequested.ShouldBe(480);
        }

        [Fact]
        public void GetNegotiatedPduTest()
        {
            var rc = client.NegotiatedPduLength();
            rc.ShouldBe(0);
            client.PduSizeNegotiated.ShouldBe(0);
        }

        [Fact]
        public void SetPlcPortTest()
        {
            client.PLCPort = 104;
            client.PLCPort.ShouldBe(104);
        }

        [Fact]
        public void SetPduRequestedTest()
        {
            client.PduSizeRequested = 239;
            client.PduSizeRequested.ShouldBe(240);
            client.PduSizeRequested = 961;
            client.PduSizeRequested.ShouldBe(960);
            client.PduSizeRequested = 481;
            client.PduSizeRequested.ShouldBe(481);
        }

        [Fact]
        public void SetTimeoutTest()
        {
            client.ConnTimeout = 239;
            client.ConnTimeout.ShouldBe(239);

            client.RecvTimeout = 239;
            client.RecvTimeout.ShouldBe(239);

            client.SendTimeout = 239;
            client.SendTimeout.ShouldBe(239);
        }

        [Fact]
        public void GetExecTimeTest()
        {
            client.ExecutionTime.ShouldBe(client.ExecutionTime);
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(2, 102)]
        [InlineData(3, 2000)]
        [InlineData(4, 2000)]
        [InlineData(5, 2000)]
        [InlineData(6, 0)]
        [InlineData(7, 0)]
        [InlineData(8, 0)]
        [InlineData(9, 0)]
        [InlineData(10, 480)]
        [InlineData(11, 0)]
        [InlineData(12, 0)]
        [InlineData(13, 0)]
        [InlineData(14, 0)]
        [InlineData(15, 0)]
        public void GetParameterTest(int parameterNumber, int expected)
        {
            int value = -1;
            var result = client.GetParam(parameterNumber, ref value);
            if(result == 0)
                value.ShouldBe(expected);
            else
                result.ShouldBe(0x02500000);
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(2, 103)]
        [InlineData(3, 2001)]
        [InlineData(4, 2001)]
        [InlineData(5, 2001)]
        [InlineData(6, 0)]
        [InlineData(7, 0)]
        [InlineData(8, 0)]
        [InlineData(9, 0)]
        [InlineData(10, 482)]
        [InlineData(11, 0)]
        [InlineData(12, 0)]
        [InlineData(13, 0)]
        [InlineData(14, 0)]
        [InlineData(15, 0)]
        public void SetParameterTest(int parameterNumber, int newValue)
        {
            var result = client.SetParam(parameterNumber, ref newValue);
            if (result == 0)
            {
                int readValue = -1;
                client.GetParam(parameterNumber, ref readValue);
                readValue.ShouldBe(newValue);
            }
            else
                result.ShouldBe(0x02500000);
        }
    }
}