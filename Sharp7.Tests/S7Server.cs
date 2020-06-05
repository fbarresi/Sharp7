using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Sharp7.Tests
{
    internal class S7Consts
    {
        public const string S7LibName = "snap7.dll";
    }
    public class S7Server
    {
        #region [Constants, private vars and TypeDefs]

        private const int MsgTextLen = 1024;
        private const int MkEvent = 0;
        private const int MkLog = 1;

        // Server Area ID  (use with Register/unregister - Lock/unlock Area)
        public static readonly int SrvAreaPe = 0;
        public static readonly int SrvAreaPa = 1;
        public static readonly int SrvAreaMk = 2;
        public static readonly int SrvAreaCt = 3;
        public static readonly int SrvAreaTm = 4;
        public static readonly int SrvAreaDB = 5;

        // S7 Server Event Code
        public static readonly uint EvcPdUincoming = 0x00010000;
        public static readonly uint EvcDataRead = 0x00020000;
        public static readonly uint EvcDataWrite = 0x00040000;
        public static readonly uint EvcNegotiatePdu = 0x00080000;
        public static readonly uint EvcReadSzl = 0x00100000;
        public static readonly uint EvcClock = 0x00200000;
        public static readonly uint EvcUpload = 0x00400000;
        public static readonly uint EvcDownload = 0x00800000;
        public static readonly uint EvcDirectory = 0x01000000;
        public static readonly uint EvcSecurity = 0x02000000;
        public static readonly uint EvcControl = 0x04000000;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct USrvEvent
        {
            public IntPtr EvtTime;   // It's platform dependent (32 or 64 bit)
            public Int32 EvtSender;
            public UInt32 EvtCode;
            public ushort EvtRetCode;
            public ushort EvtParam1;
            public ushort EvtParam2;
            public ushort EvtParam3;
            public ushort EvtParam4;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7Tag
        {
            public Int32 Area;
            public Int32 DBNumber;
            public Int32 Start;
            public Int32 Elements;
            public Int32 WordLen;
        }

        private Dictionary<Int32, GCHandle> hArea;

        private IntPtr server;

        #endregion

        #region [Class Control]

        [DllImport(S7Consts.S7LibName)]
        private static extern IntPtr Srv_Create();
        /// <summary>
        /// Create an instace of S7Server
        /// </summary>
        public S7Server()
        {
            server = Srv_Create();
            hArea = new Dictionary<int, GCHandle>();
        }

        [DllImport(S7Consts.S7LibName)]
        private static extern int Srv_Destroy(ref IntPtr server);
        /// <summary>
        /// Destroy the S7Server and free the memory
        /// </summary>
        ~S7Server()
        {
            foreach (var item in hArea)
            {
                GCHandle handle = item.Value;
                if (handle != null)
                    handle.Free();
            }
            Srv_Destroy(ref server);
        }

        [DllImport(S7Consts.S7LibName)]
        private static extern int Srv_StartTo(IntPtr server, [MarshalAs(UnmanagedType.LPStr)] string address);
        /// <summary>
        /// Start the server to a specific Address
        /// </summary>
        /// <param name="address">Address for adapter selection</param>
        /// <returns>0: No errors. Otherwise see errorcodes</returns>
        public int StartTo(string address)
        {
            return Srv_StartTo(server, address);
        }

        [DllImport(S7Consts.S7LibName)]
        private static extern int Srv_Start(IntPtr server);
        /// <summary>
        /// start the server
        /// </summary>
        /// <returns>0: No errors. Otherwise see errorcodes</returns>
        public int Start()
        {
            return Srv_Start(server);
        }

        [DllImport(S7Consts.S7LibName)]
        private static extern int Srv_Stop(IntPtr server);
        /// <summary>
        /// Stop the server
        /// </summary>
        /// <returns>0: No errors. Otherwise see errorcodes</returns>
        public int Stop()
        {
            return Srv_Stop(server);
        }

        #endregion

        #region [Data Areas functions]

        [DllImport(S7Consts.S7LibName)]
        private static extern int Srv_RegisterArea(IntPtr server, Int32 areaCode, Int32 index, IntPtr pUsrData, Int32 size);
        /// <summary>
        /// Register a PLC Area
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="areaCode">Code for area identification (e.g. S7Server.SrvAreaDB)</param>
        /// <param name="index">Area index</param>
        /// <param name="pUsrData">Content of the area by reference</param>
        /// <param name="size">Allocation size</param>
        /// <returns>0: No errors. Otherwise see errorcodes</returns>
        public int RegisterArea<T>(Int32 areaCode, Int32 index, ref T pUsrData, Int32 size)
        {
            Int32 areaUid = (areaCode << 16) + index;
            GCHandle handle = GCHandle.Alloc(pUsrData, GCHandleType.Pinned);
            int result = Srv_RegisterArea(server, areaCode, index, handle.AddrOfPinnedObject(), size);
            if (result == 0)
                hArea.Add(areaUid, handle);
            else
                handle.Free();
            return result;
        }

        [DllImport(S7Consts.S7LibName)]
        private static extern int Srv_UnregisterArea(IntPtr server, Int32 areaCode, Int32 index);
        /// <summary>
        /// Unregister a PLC area
        /// </summary>
        /// <param name="areaCode">Code for area identification (e.g. S7Server.SrvAreaDB)</param>
        /// <param name="index">Area index</param>
        /// <returns>0: No errors. Otherwise see errorcodes</returns>
        public int UnregisterArea(Int32 areaCode, Int32 index)
        {
            int result = Srv_UnregisterArea(server, areaCode, index);
            if (result == 0)
            {
                Int32 areaUid = (areaCode << 16) + index;
                if (hArea.ContainsKey(areaUid)) // should be always true
                {
                    GCHandle handle = hArea[areaUid];
                    if (handle != null) // should be always true
                        handle.Free();
                    hArea.Remove(areaUid);
                }
            }
            return result;
        }

        [DllImport(S7Consts.S7LibName)]
        private static extern int Srv_LockArea(IntPtr server, Int32 areaCode, Int32 index);
        /// <summary>
        /// Lock a memory area
        /// </summary>
        /// <param name="areaCode">Code for area identification (e.g. S7Server.SrvAreaDB)</param>
        /// <param name="index">Area index</param>
        /// <returns>0: No errors. Otherwise see errorcodes</returns>
        public int LockArea(Int32 areaCode, Int32 index)
        {
            return Srv_LockArea(server, areaCode, index);
        }

        [DllImport(S7Consts.S7LibName)]
        private static extern int Srv_UnlockArea(IntPtr server, Int32 areaCode, Int32 index);
        /// <summary>
        /// Unlock a memory area
        /// </summary>
        /// <param name="areaCode">Code for area identification (e.g. S7Server.SrvAreaDB)</param>
        /// <param name="index">Area index</param>
        /// <returns>0: No errors. Otherwise see errorcodes</returns>
        public int UnlockArea(Int32 areaCode, Int32 index)
        {
            return Srv_UnlockArea(server, areaCode, index);
        }

        #endregion

        #region [Notification functions (Events)]

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct RwBuffer
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)] // A telegram cannot exceed PDU size (960 bytes)
            public byte[] Data;
        }
        /// <summary>
        /// Callback delegate
        /// </summary>
        /// <param name="usrPtr">User pointer passed back</param>
        /// <param name="Event">Event information structure</param>
        /// <param name="size">Size</param>
        public delegate void SrvCallback(IntPtr usrPtr, ref USrvEvent Event, int size);
        /// <summary>
        /// Callback delegate for RW operation
        /// </summary>
        /// <param name="usrPtr">User pointer passed back</param>
        /// <param name="sender">Sender</param>
        /// <param name="operation">Operation type</param>
        /// <param name="tag">Operation Tag</param>
        /// <param name="buffer">RW Buffer</param>
        /// <returns>0: No errors. Otherwise see errorcodes</returns>
        public delegate int SrvRwAreaCallback(IntPtr usrPtr, int sender, int operation, ref S7Tag tag, ref RwBuffer buffer);

        [DllImport(S7Consts.S7LibName)]
        private static extern int Srv_SetEventsCallback(IntPtr server, SrvCallback callback, IntPtr usrPtr);
        /// <summary>
        /// Set a function callback
        /// </summary>
        /// <param name="callback">Callback delegate</param>
        /// <param name="usrPtr">User pointer passed back</param>
        /// <returns>0: No errors. Otherwise see errorcodes</returns>
        public int SetEventsCallBack(SrvCallback callback, IntPtr usrPtr)
        {
            return Srv_SetEventsCallback(server, callback, usrPtr);
        }

        [DllImport(S7Consts.S7LibName)]
        private static extern int Srv_SetReadEventsCallback(IntPtr server, SrvCallback callback, IntPtr usrPtr);
        /// <summary>
        /// Set a function callback for read events
        /// </summary>
        /// <param name="callback">Callback delegate</param>
        /// <param name="usrPtr">User pointer passed back</param>
        /// <returns>0: No errors. Otherwise see errorcodes</returns>
        public int SetReadEventsCallBack(SrvCallback callback, IntPtr usrPtr)
        {
            return Srv_SetReadEventsCallback(server, callback, usrPtr);
        }

        [DllImport(S7Consts.S7LibName)]
        private static extern int Srv_SetRWAreaCallback(IntPtr server, SrvRwAreaCallback callback, IntPtr usrPtr);
        /// <summary>
        /// Set a function callback for read-write events
        /// </summary>
        /// <param name="callback">Callback delegate</param>
        /// <param name="usrPtr">User pointer passed back</param>
        /// <returns>0: No errors. Otherwise see errorcodes</returns>
        public int SetRwAreaCallBack(SrvRwAreaCallback callback, IntPtr usrPtr)
        {
            return Srv_SetRWAreaCallback(server, callback, usrPtr);
        }

        [DllImport(S7Consts.S7LibName)]
        private static extern int Srv_PickEvent(IntPtr server, ref USrvEvent Event, ref Int32 evtReady);
        /// <summary>
        /// Extracts an event (if available) from the Events queue.
        /// </summary>
        /// <param name="Event">Reference of User event</param>
        /// <returns>0: No errors. Otherwise see errorcodes</returns>
        public bool PickEvent(ref USrvEvent Event)
        {
            Int32 evtReady = new Int32();
            if (Srv_PickEvent(server, ref Event, ref evtReady) == 0)
                return evtReady != 0;
            else
                return false;
        }

        [DllImport(S7Consts.S7LibName)]
        private static extern int Srv_ClearEvents(IntPtr server);
        /// <summary>
        /// clear the event queue
        /// </summary>
        /// <returns>0: No errors. Otherwise see errorcodes</returns>
        public int ClearEvents()
        {
            return Srv_ClearEvents(server);
        }

        [DllImport(S7Consts.S7LibName, CharSet = CharSet.Ansi)]
        private static extern int Srv_EventText(ref USrvEvent Event, StringBuilder evtMsg, int textSize);
        /// <summary>
        /// retrieve a message from an event
        /// </summary>
        /// <param name="Event">Reference to Event</param>
        /// <returns>The message for an event</returns>
        public string EventText(ref USrvEvent Event)
        {
            StringBuilder message = new StringBuilder(MsgTextLen);
            Srv_EventText(ref Event, message, MsgTextLen);
            return message.ToString();
        }
        /// <summary>
        /// Convet an the event time to datetime object
        /// </summary>
        /// <param name="timeStamp">Event Time pointer</param>
        /// <returns>the datetime for the timestamp</returns>
        public DateTime EvtTimeToDateTime(IntPtr timeStamp)
        {
            DateTime unixStartEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return unixStartEpoch.AddSeconds(Convert.ToDouble(timeStamp));
        }

        [DllImport(S7Consts.S7LibName)]
        private static extern int Srv_GetMask(IntPtr server, Int32 maskKind, ref UInt32 mask);
        [DllImport(S7Consts.S7LibName)]
        private static extern int Srv_SetMask(IntPtr server, Int32 maskKind, UInt32 mask);

        // Property LogMask R/W
        /// <summary>
        /// Activate o deactivate the LogMask
        /// </summary>
        public UInt32 LogMask
        {
            get
            {
                UInt32 mask = new UInt32();
                if (Srv_GetMask(server, S7Server.MkLog, ref mask) == 0)
                    return mask;
                else
                    return 0;
            }
            set => Srv_SetMask(server, S7Server.MkLog, value);
        }

        // Property EventMask R/W
        /// <summary>
        /// Activate o deactivate the EventMask
        /// </summary>
        public UInt32 EventMask
        {
            get
            {
                UInt32 mask = new UInt32();
                if (Srv_GetMask(server, S7Server.MkEvent, ref mask) == 0)
                    return mask;
                else
                    return 0;
            }
            set => Srv_SetMask(server, S7Server.MkEvent, value);
        }


        #endregion

        #region [Info functions]

        [DllImport(S7Consts.S7LibName)]
        private static extern int Srv_GetStatus(IntPtr server, ref Int32 serverStatus, ref Int32 cpuStatus, ref Int32 clientsCount);
        [DllImport(S7Consts.S7LibName)]
        private static extern int Srv_SetCpuStatus(IntPtr server, Int32 cpuStatus);

        // Property Virtual CPU status R/W
        public int CpuStatus
        {
            get
            {
                Int32 cStatus = new Int32();
                Int32 sStatus = new Int32();
                Int32 cCount = new Int32();

                if (Srv_GetStatus(server, ref sStatus, ref cStatus, ref cCount) == 0)
                    return cStatus;
                else
                    return -1;
            }
            set => Srv_SetCpuStatus(server, value);
        }

        // Property Server Status Read Only
        public int ServerStatus
        {
            get
            {
                Int32 cStatus = new Int32();
                Int32 sStatus = new Int32();
                Int32 cCount = new Int32();
                if (Srv_GetStatus(server, ref sStatus, ref cStatus, ref cCount) == 0)
                    return sStatus;
                else
                    return -1;
            }
        }

        // Property Clients Count Read Only
        public int ClientsCount
        {
            get
            {
                Int32 cStatus = new Int32();
                Int32 sStatus = new Int32();
                Int32 cCount = new Int32();
                if (Srv_GetStatus(server, ref cStatus, ref sStatus, ref cCount) == 0)
                    return cCount;
                else
                    return -1;
            }
        }

        [DllImport(S7Consts.S7LibName, CharSet = CharSet.Ansi)]
        private static extern int Srv_ErrorText(int error, StringBuilder errMsg, int textSize);
        /// <summary>
        /// Retrieve the error message for an error code
        /// </summary>
        /// <param name="error">Error code</param>
        /// <returns>Message for the error code</returns>
        public string ErrorText(int error)
        {
            StringBuilder message = new StringBuilder(MsgTextLen);
            Srv_ErrorText(error, message, MsgTextLen);
            return message.ToString();
        }

        #endregion
    }
}