using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;

namespace Sharp7.Tests
{
    public class ServerTestBase : IDisposable
    {
        private readonly S7Server server;
        protected readonly string Localhost = "127.0.0.1";
        public ServerTestBase()
        {
            server = new S7Server();
            var rc = server.StartTo(Localhost);
            rc.ShouldBe(0);
        }

        public S7Server Server => server;

        public void Dispose()
        {
            server.Stop();
        }
    }
}