/*=============================================================================|
|  PROJECT Sharp7                                                        1.0.3 |
|==============================================================================|
|  Copyright (C) 2016 Davide Nardella                                          |
|  All rights reserved.                                                        |
|==============================================================================|
|  Sharp7 is free software: you can redistribute it and/or modify              |
|  it under the terms of the Lesser GNU General Public License as published by |
|  the Free Software Foundation, either version 3 of the License, or           |
|  (at your option) any later version.                                         |
|                                                                              |
|  It means that you can distribute your commercial software which includes    |
|  Sharp7 without the requirement to distribute the source code of your        |
|  application and without the requirement that your application be itself     |
|  distributed under LGPL.                                                     |
|                                                                              |
|  Sharp7 is distributed in the hope that it will be useful,                   |
|  but WITHOUT ANY WARRANTY; without even the implied warranty of              |
|  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the               |
|  Lesser GNU General Public License for more details.                         |
|                                                                              |
|  You should have received a copy of the GNU General Public License and a     |
|  copy of Lesser GNU General Public License along with Sharp7.                |
|  If not, see  http://www.gnu.org/licenses/                                   |
|==============================================================================|
History:
 * 1.0.0 2016/10/09 First Release
 * 1.0.1 2016/10/22 Added CoreCLR compatibility (CORE_CLR symbol must be 
                    defined in Build options).
                    Thanks to Dirk-Jan Wassink.
 * 1.0.2 2016/11/13 Fixed a bug in CLR compatibility
 * 1.0.3 2017/01/25 Fixed a bug in S7.GetIntAt(). Thanks to lupal1
                    Added S7Timer Read/Write. Thanks to Lukas Palkovic 

*/
using System;
using System.Threading;
//------------------------------------------------------------------------------
// If you are compiling for UWP verify that WINDOWS_UWP or NETFX_CORE are 
// defined into Project Properties->Build->Conditional compilation symbols
//------------------------------------------------------------------------------
#if WINDOWS_UWP || NETFX_CORE
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
#else // <-- Including MONO
using System.Net.Sockets;
#endif

namespace Sharp7
{

    #region [Async Sockets UWP(W10,IoT,Phone)/Windows 8/Windows 8 Phone]
#if WINDOWS_UWP || NETFX_CORE
    class MsgSocket
    {
        private DataReader Reader = null;
        private DataWriter Writer = null;
        private StreamSocket TCPSocket;

        private bool _Connected;

        private int _ReadTimeout = 2000;
        private int _WriteTimeout = 2000;
        private int _ConnectTimeout = 1000;

        public static int LastError = 0;


        private void CreateSocket()
        {
            TCPSocket = new StreamSocket();
            TCPSocket.Control.NoDelay = true;
            _Connected = false;
        }

        public MsgSocket()
        {
        }

        public void Close()
        {
            if (Reader != null)
            {
                Reader.Dispose();
                Reader = null;
            }
            if (Writer != null)
            {
                Writer.Dispose();
                Writer = null;
            }
            if (TCPSocket != null)
            {
                TCPSocket.Dispose();
                TCPSocket = null;
            }
            _Connected = false;
        }

        private async Task AsConnect(string Host, string port, CancellationTokenSource cts)
        {
            HostName ServerHost = new HostName(Host);
            try
            {
                await TCPSocket.ConnectAsync(ServerHost, port).AsTask(cts.Token);
                _Connected = true;
            }
            catch (TaskCanceledException)
            {
                LastError = S7Consts.errTCPConnectionTimeout;
            }
            catch
            {
                LastError = S7Consts.errTCPConnectionFailed; // Maybe unreachable peer
            }
        }

        public int Connect(string Host, int Port)
        {
            LastError = 0;
            if (!Connected)
            {
                CreateSocket();
                CancellationTokenSource cts = new CancellationTokenSource();
                try
                {
                    try
                    {
                        cts.CancelAfter(_ConnectTimeout);
                        Task.WaitAny(Task.Run(async () => await AsConnect(Host, Port.ToString(), cts)));
                    }
                    catch
                    {
                        LastError = S7Consts.errTCPConnectionFailed;
                    }
                }
                finally
                {
                    if (cts != null)
                    {
                        try
                        {
                            cts.Cancel();
                            cts.Dispose();
                            cts = null;
                        }
                        catch { }
                    }

                }
                if (LastError == 0)
                {
                    Reader = new DataReader(TCPSocket.InputStream);
                    Reader.InputStreamOptions = InputStreamOptions.Partial;
                    Writer = new DataWriter(TCPSocket.OutputStream);
                    _Connected = true;
                }
                else
                    Close();
            }
            return LastError;
        }

        private async Task AsReadBuffer(byte[] Buffer, int Size, CancellationTokenSource cts)
        {
            try
            {
                await Reader.LoadAsync((uint)Size).AsTask(cts.Token);
                Reader.ReadBytes(Buffer);
            }
            catch
            {
                LastError = S7Consts.errTCPDataReceive;
            }
        }

        public int Receive(byte[] Buffer, int Start, int Size)
        {
            byte[] InBuffer = new byte[Size];
            CancellationTokenSource cts = new CancellationTokenSource();
            LastError = 0;
            try
            {
                try
                {
                    cts.CancelAfter(_ReadTimeout);
                    Task.WaitAny(Task.Run(async () => await AsReadBuffer(InBuffer, Size, cts)));
                }
                catch
                {
                    LastError = S7Consts.errTCPDataReceive;
                }
            }
            finally
            {
                if (cts != null)
                {
                    try
                    {
                        cts.Cancel();
                        cts.Dispose();
                        cts = null;
                    }
                    catch { }
                }
            }
            if (LastError == 0)
                Array.Copy(InBuffer, 0, Buffer, Start, Size);
            else
                Close();
            return LastError;
        }

        private async Task WriteBuffer(byte[] Buffer, CancellationTokenSource cts)
        {
            try
            {
                Writer.WriteBytes(Buffer);
                await Writer.StoreAsync().AsTask(cts.Token);
            }
            catch
            {
                LastError = S7Consts.errTCPDataSend;
            }
        }

        public int Send(byte[] Buffer, int Size)
        {
            byte[] OutBuffer = new byte[Size];
            CancellationTokenSource cts = new CancellationTokenSource();
            Array.Copy(Buffer, 0, OutBuffer, 0, Size);
            LastError = 0;
            try
            {
                try
                {
                    cts.CancelAfter(_WriteTimeout);
                    Task.WaitAny(Task.Run(async () => await WriteBuffer(OutBuffer, cts)));
                }
                catch
                {
                    LastError = S7Consts.errTCPDataSend;
                }
            }
            finally
            {
                if (cts != null)
                {
                    try
                    {
                        cts.Cancel();
                        cts.Dispose();
                        cts = null;
                    }
                    catch { }
                }
            }
            if (LastError != 0)
                Close();
            return LastError;
        }

        ~MsgSocket()
        {
            Close();
        }

        public bool Connected
        {
            get
            {
                return (TCPSocket != null) && _Connected;
            }
        }

        public int ReadTimeout
        {
            get
            {
                return _ReadTimeout;
            }
            set
            {
                _ReadTimeout = value;
            }
        }

        public int WriteTimeout
        {
            get
            {
                return _WriteTimeout;
            }
            set
            {
                _WriteTimeout = value;
            }
        }
        public int ConnectTimeout
        {
            get
            {
                return _ConnectTimeout;
            }
            set
            {
                _ConnectTimeout = value;
            }
        }
    }
#endif
    #endregion

    #region [Sync Sockets Win32/Win64 Desktop Application]
#if !WINDOWS_UWP && !NETFX_CORE
    class MsgSocket
    {
        private Socket TCPSocket;
        private int _ReadTimeout = 2000;
        private int _WriteTimeout = 2000;
        private int _ConnectTimeout = 1000;
        public int LastError = 0;

        public MsgSocket()
        {
        }

        ~MsgSocket()
        {
            Close();
        }

        public void Close()
        {
            if (TCPSocket != null)
            {
                TCPSocket.Dispose();
                TCPSocket = null;
            }
        }

        private void CreateSocket()
        {
            TCPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            TCPSocket.NoDelay = true;
        }

        private void TCPPing(string Host, int Port)
        {
            // To Ping the PLC an Asynchronous socket is used rather then an ICMP packet.
            // This allows the use also across Internet and Firewalls (obviously the port must be opened)           
            LastError = 0;
            Socket PingSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {

#if CORE_CLR
                var task = PingSocket.ConnectAsync(Host, Port);
                task.Wait(_ConnectTimeout);
                bool success = task.IsCompleted;
#else
                IAsyncResult result = PingSocket.BeginConnect(Host, Port, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(_ConnectTimeout, true);
#endif
                if (!success)
                {
                    LastError = S7Consts.errTCPConnectionFailed;
                }
            }
            catch
            {
                LastError = S7Consts.errTCPConnectionFailed;
            };
#if CORE_CLR
            PingSocket.Dispose();
#else
            PingSocket.Close();
#endif
        }

        public int Connect(string Host, int Port)
        {
            LastError = 0;
            if (!Connected)
            { 
                TCPPing(Host, Port);
                if (LastError == 0)
                try
                {
                        CreateSocket();
                        TCPSocket.Connect(Host, Port);
                }
                catch
                {
                        LastError = S7Consts.errTCPConnectionFailed;
                }
            }
            return LastError;
        }

        private int WaitForData(int Size, int Timeout)
        {
            bool Expired = false;
            int SizeAvail;
            int Elapsed = Environment.TickCount;
            LastError = 0;
            try
            {
                SizeAvail = TCPSocket.Available;
                while ((SizeAvail < Size) && (!Expired))
                {
                    Thread.Sleep(2);
                    SizeAvail = TCPSocket.Available;
                    Expired = Environment.TickCount - Elapsed > Timeout;
                    // If timeout we clean the buffer
                    if (Expired && (SizeAvail > 0))
                    try
                    {
                        byte[] Flush = new byte[SizeAvail];
                        TCPSocket.Receive(Flush, 0, SizeAvail, SocketFlags.None);
                    }
                    catch { }
                }
            }
            catch
            {
                LastError = S7Consts.errTCPDataReceive;
            }
            if (Expired)
            {
                LastError = S7Consts.errTCPDataReceive;
            }
            return LastError;
        }

        public int Receive(byte[] Buffer, int Start, int Size)
        {

            int BytesRead = 0;
            LastError = WaitForData(Size, _ReadTimeout);
            if (LastError == 0)
            {
                try
                {
                    BytesRead = TCPSocket.Receive(Buffer, Start, Size, SocketFlags.None);
                }
                catch
                {
                    LastError = S7Consts.errTCPDataReceive;
                }
                if (BytesRead == 0) // Connection Reset by the peer
                {
                    LastError = S7Consts.errTCPDataReceive;
                    Close();
                }
            }
            return LastError;
        }

        public int Send(byte[] Buffer, int Size)
        {
            LastError = 0;
            try
            {
                int BytesSent = TCPSocket.Send(Buffer, Size, SocketFlags.None);
            }
            catch
            {
                LastError = S7Consts.errTCPDataSend;
                Close();
            }
            return LastError;
        }

        public bool Connected
        {
            get
            {
                return (TCPSocket != null) && (TCPSocket.Connected); 
            }
        }

        public int ReadTimeout
        {
            get
            {
                return _ReadTimeout;
            }
            set
            {
                _ReadTimeout = value;
            }
        }

        public int WriteTimeout
        {
            get
            {
                return _WriteTimeout;
            }
            set
            {
                _WriteTimeout = value;
            }

        }
        public int ConnectTimeout
        {
            get
            {
                return _ConnectTimeout;
            }
            set
            {
                _ConnectTimeout = value;
            }
        }
    }   
#endif
#endregion
}
