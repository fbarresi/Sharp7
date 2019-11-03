using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;

namespace Sharp7.Tests
{
    public class ServerClientTestBase : ServerTestBase, IDisposable
    {
        private S7Client client;
        public S7Client Client => client;

        public ServerClientTestBase() : base()
        {
            client = new S7Client();
            var rc = client.ConnectTo(Localhost, 0, 2);
            rc.ShouldBe(0);
        }


        public new void Dispose()
        {
            client.Disconnect();
            base.Dispose();
        }
    }
}