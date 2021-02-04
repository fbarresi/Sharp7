using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Sharp7
{
	public class S7Client
	{
		#region [Constants and TypeDefs]

		// Block type
		public const int Block_OB  = 0x38;
		public const int Block_DB  = 0x41;
		public const int Block_SDB = 0x42;
		public const int Block_FC  = 0x43;
		public const int Block_SFC = 0x44;
		public const int Block_FB  = 0x45;
		public const int Block_SFB = 0x46;

		// Sub Block Type 
		public const byte SubBlk_OB  = 0x08;
		public const byte SubBlk_DB  = 0x0A;
		public const byte SubBlk_SDB = 0x0B;
		public const byte SubBlk_FC  = 0x0C;
		public const byte SubBlk_SFC = 0x0D;
		public const byte SubBlk_FB  = 0x0E;
		public const byte SubBlk_SFB = 0x0F;

		// Block languages
		public const byte BlockLangAWL   = 0x01;
		public const byte BlockLangKOP   = 0x02;
		public const byte BlockLangFUP   = 0x03;
		public const byte BlockLangSCL   = 0x04;
		public const byte BlockLangDB    = 0x05;
		public const byte BlockLangGRAPH = 0x06;

		// Max number of vars (multiread/write)
		public static readonly int MaxVars = 20;
        
		// Result transport size
		const byte TS_ResBit   = 0x03;
		const byte TS_ResByte  = 0x04;
		const byte TS_ResInt   = 0x05;
		const byte TS_ResReal  = 0x07;
		const byte TS_ResOctet = 0x09;

		const ushort Code7Ok                    = 0x0000;
		const ushort Code7AddressOutOfRange     = 0x0005;
		const ushort Code7InvalidTransportSize  = 0x0006;
		const ushort Code7WriteDataSizeMismatch = 0x0007;
		const ushort Code7ResItemNotAvailable   = 0x000A;
		const ushort Code7ResItemNotAvailable1  = 0xD209;
		const ushort Code7InvalidValue          = 0xDC01;
		const ushort Code7NeedPassword          = 0xD241;
		const ushort Code7InvalidPassword       = 0xD602;
		const ushort Code7NoPasswordToClear     = 0xD604;
		const ushort Code7NoPasswordToSet       = 0xD605;
		const ushort Code7FunNotAvailable       = 0x8104;
		const ushort Code7DataOverPDU           = 0x8500;

		// Client Connection Type
		public static readonly UInt16 CONNTYPE_PG    = 0x01;  // Connect to the PLC as a PG
		public static readonly UInt16 CONNTYPE_OP    = 0x02;  // Connect to the PLC as an OP
		public static readonly UInt16 CONNTYPE_BASIC = 0x03;  // Basic connection 

		public int _LastError = 0;

		public struct S7DataItem
		{
			public int Area;
			public int WordLen;
			public int Result;
			public int DBNumber;
			public int Start;
			public int Amount;
			public IntPtr pData;
		}

		// Order Code + Version
		public struct S7OrderCode
		{
			public string Code; // such as "6ES7 151-8AB01-0AB0"
			public byte V1;     // Version 1st digit
			public byte V2;     // Version 2nd digit
			public byte V3;     // Version 3th digit
		};

		// CPU Info
		public struct S7CpuInfo
		{
			public string ModuleTypeName;
			public string SerialNumber;
			public string ASName;
			public string Copyright;
			public string ModuleName;
		}

		public struct S7CpInfo
		{
			public int MaxPduLength;
			public int MaxConnections;
			public int MaxMpiRate;
			public int MaxBusRate;
		};

		// Block List
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct S7BlocksList
		{
			public Int32 OBCount;
			public Int32 FBCount;
			public Int32 FCCount;
			public Int32 SFBCount;
			public Int32 SFCCount;
			public Int32 DBCount;
			public Int32 SDBCount;
		};

		// Managed Block Info
		public struct S7BlockInfo
		{
			public int BlkType;
			public int BlkNumber;
			public int BlkLang;
			public int BlkFlags;
			public int MC7Size;  // The real size in bytes
			public int LoadSize;
			public int LocalData;
			public int SBBLength;
			public int CheckSum;
			public int Version;
			// Chars info
			public string CodeDate;
			public string IntfDate;
			public string Author;
			public string Family;
			public string Header;
		};

		// See §33.1 of "System Software for S7-300/400 System and Standard Functions"
		// and see SFC51 description too
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct SZL_HEADER
		{
			public UInt16 LENTHDR;
			public UInt16 N_DR;
		};

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct S7SZL
		{
			public SZL_HEADER Header;
			[MarshalAs(UnmanagedType.ByValArray)]
			public byte[] Data;
		};

		// SZL List of available SZL IDs : same as SZL but List items are big-endian adjusted
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct S7SZLList
		{
			public SZL_HEADER Header;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x2000 - 2)]
			public UInt16[] Data;
		};

		// S7 Protection
		// See §33.19 of "System Software for S7-300/400 System and Standard Functions"
		public struct S7Protection  
		{
			public ushort sch_schal;
			public ushort sch_par;
			public ushort sch_rel;
			public ushort bart_sch;
			public ushort anl_sch;
		};

		#endregion

		#region [S7 Telegrams]

		// ISO Connection Request telegram (contains also ISO Header and COTP Header)
		byte[] ISO_CR = {
			// TPKT (RFC1006 Header)
			0x03, // RFC 1006 ID (3) 
			0x00, // Reserved, always 0
			0x00, // High part of packet lenght (entire frame, payload and TPDU included)
			0x16, // Low part of packet lenght (entire frame, payload and TPDU included)
			// COTP (ISO 8073 Header)
			0x11, // PDU Size Length
			0xE0, // CR - Connection Request ID
			0x00, // Dst Reference HI
			0x00, // Dst Reference LO
			0x00, // Src Reference HI
			0x01, // Src Reference LO
			0x00, // Class + Options Flags
			0xC0, // PDU Max Length ID
			0x01, // PDU Max Length HI
			0x0A, // PDU Max Length LO
			0xC1, // Src TSAP Identifier
			0x02, // Src TSAP Length (2 bytes)
			0x01, // Src TSAP HI (will be overwritten)
			0x00, // Src TSAP LO (will be overwritten)
			0xC2, // Dst TSAP Identifier
			0x02, // Dst TSAP Length (2 bytes)
			0x01, // Dst TSAP HI (will be overwritten)
			0x02  // Dst TSAP LO (will be overwritten)
		};

		// TPKT + ISO COTP Header (Connection Oriented Transport Protocol)
		byte[] TPKT_ISO = { // 7 bytes
			0x03,0x00,
			0x00,0x1f,      // Telegram Length (Data Size + 31 or 35)
			0x02,0xf0,0x80  // COTP (see above for info)
		};

		// S7 PDU Negotiation Telegram (contains also ISO Header and COTP Header)
		byte[] S7_PN = {
			0x03, 0x00, 0x00, 0x19, 
			0x02, 0xf0, 0x80, // TPKT + COTP (see above for info)
			0x32, 0x01, 0x00, 0x00, 
			0x04, 0x00, 0x00, 0x08, 
			0x00, 0x00, 0xf0, 0x00, 
			0x00, 0x01, 0x00, 0x01, 
			0x00, 0x1e        // PDU Length Requested = HI-LO Here Default 480 bytes
		};
        
		// S7 Read/Write Request Header (contains also ISO Header and COTP Header)
		byte[] S7_RW = { // 31-35 bytes
			0x03,0x00, 
			0x00,0x1f,       // Telegram Length (Data Size + 31 or 35)
			0x02,0xf0, 0x80, // COTP (see above for info)
			0x32,            // S7 Protocol ID 
			0x01,            // Job Type
			0x00,0x00,       // Redundancy identification
			0x05,0x00,       // PDU Reference
			0x00,0x0e,       // Parameters Length
			0x00,0x00,       // Data Length = Size(bytes) + 4      
			0x04,            // Function 4 Read Var, 5 Write Var  
			0x01,            // Items count
			0x12,            // Var spec.
			0x0a,            // Length of remaining bytes
			0x10,            // Syntax ID 
			(byte)S7WordLength.Byte,  // Transport Size idx=22                       
			0x00,0x00,       // Num Elements                          
			0x00,0x00,       // DB Number (if any, else 0)            
			0x84,            // Area Type                            
			0x00,0x00,0x00,  // Area Offset                     
			// WR area
			0x00,            // Reserved 
			0x04,            // Transport size
			0x00,0x00,       // Data Length * 8 (if not bit or timer or counter) 
		};
		private static int Size_RD = 31; // Header Size when Reading 
		private static int Size_WR = 35; // Header Size when Writing

		// S7 Variable MultiRead Header
		byte[] S7_MRD_HEADER = {
			0x03,0x00, 
			0x00,0x1f,       // Telegram Length 
			0x02,0xf0, 0x80, // COTP (see above for info)
			0x32,            // S7 Protocol ID 
			0x01,            // Job Type
			0x00,0x00,       // Redundancy identification
			0x05,0x00,       // PDU Reference
			0x00,0x0e,       // Parameters Length
			0x00,0x00,       // Data Length = Size(bytes) + 4      
			0x04,            // Function 4 Read Var, 5 Write Var  
			0x01             // Items count (idx 18)
		};

		// S7 Variable MultiRead Item
		byte[] S7_MRD_ITEM = {
			0x12,            // Var spec.
			0x0a,            // Length of remaining bytes
			0x10,            // Syntax ID 
			(byte)S7WordLength.Byte,  // Transport Size idx=3                   
			0x00,0x00,       // Num Elements                          
			0x00,0x00,       // DB Number (if any, else 0)            
			0x84,            // Area Type                            
			0x00,0x00,0x00   // Area Offset                     
		};

		// S7 Variable MultiWrite Header
		byte[] S7_MWR_HEADER = {
			0x03,0x00,
			0x00,0x1f,       // Telegram Length 
			0x02,0xf0, 0x80, // COTP (see above for info)
			0x32,            // S7 Protocol ID 
			0x01,            // Job Type
			0x00,0x00,       // Redundancy identification
			0x05,0x00,       // PDU Reference
			0x00,0x0e,       // Parameters Length (idx 13)
			0x00,0x00,       // Data Length = Size(bytes) + 4 (idx 15)     
			0x05,            // Function 5 Write Var  
			0x01             // Items count (idx 18)
		};

		// S7 Variable MultiWrite Item (Param)
		byte[] S7_MWR_PARAM = {
			0x12,            // Var spec.
			0x0a,            // Length of remaining bytes
			0x10,            // Syntax ID 
			(byte)S7WordLength.Byte,  // Transport Size idx=3                      
			0x00,0x00,       // Num Elements                          
			0x00,0x00,       // DB Number (if any, else 0)            
			0x84,            // Area Type                            
			0x00,0x00,0x00,  // Area Offset                     
		};

		// SZL First telegram request   
		byte[] S7_SZL_FIRST = {
			0x03, 0x00, 0x00, 0x21,
			0x02, 0xf0, 0x80, 0x32,
			0x07, 0x00, 0x00,
			0x05, 0x00, // Sequence out
			0x00, 0x08, 0x00,
			0x08, 0x00, 0x01, 0x12,
			0x04, 0x11, 0x44, 0x01,
			0x00, 0xff, 0x09, 0x00,
			0x04,
			0x00, 0x00, // ID (29)
			0x00, 0x00  // Index (31)
		};

		// SZL Next telegram request 
		byte[] S7_SZL_NEXT = {
			0x03, 0x00, 0x00, 0x21,
			0x02, 0xf0, 0x80, 0x32,
			0x07, 0x00, 0x00, 0x06,
			0x00, 0x00, 0x0c, 0x00,
			0x04, 0x00, 0x01, 0x12,
			0x08, 0x12, 0x44, 0x01,
			0x01, // Sequence
			0x00, 0x00, 0x00, 0x00,
			0x0a, 0x00, 0x00, 0x00
		};

		// Get Date/Time request
		byte[] S7_GET_DT = {
			0x03, 0x00, 0x00, 0x1d,
			0x02, 0xf0, 0x80, 0x32,
			0x07, 0x00, 0x00, 0x38,
			0x00, 0x00, 0x08, 0x00,
			0x04, 0x00, 0x01, 0x12,
			0x04, 0x11, 0x47, 0x01,
			0x00, 0x0a, 0x00, 0x00,
			0x00
		};

		// Set Date/Time command
		byte[] S7_SET_DT = {
			0x03, 0x00, 0x00, 0x27,
			0x02, 0xf0, 0x80, 0x32,
			0x07, 0x00, 0x00, 0x89,
			0x03, 0x00, 0x08, 0x00,
			0x0e, 0x00, 0x01, 0x12,
			0x04, 0x11, 0x47, 0x02,
			0x00, 0xff, 0x09, 0x00,
			0x0a, 0x00,
			0x19, // Hi part of Year (idx=30)
			0x13, // Lo part of Year
			0x12, // Month
			0x06, // Day
			0x17, // Hour
			0x37, // Min
			0x13, // Sec
			0x00, 0x01 // ms + Day of week   
		};

		// S7 Set Session Password 
		byte[] S7_SET_PWD = {
			0x03, 0x00, 0x00, 0x25,
			0x02, 0xf0, 0x80, 0x32,
			0x07, 0x00, 0x00, 0x27,
			0x00, 0x00, 0x08, 0x00,
			0x0c, 0x00, 0x01, 0x12,
			0x04, 0x11, 0x45, 0x01,
			0x00, 0xff, 0x09, 0x00,
			0x08, 
			// 8 Char Encoded Password
			0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00
		};

		// S7 Clear Session Password 
		byte[] S7_CLR_PWD = {
			0x03, 0x00, 0x00, 0x1d,
			0x02, 0xf0, 0x80, 0x32,
			0x07, 0x00, 0x00, 0x29,
			0x00, 0x00, 0x08, 0x00,
			0x04, 0x00, 0x01, 0x12,
			0x04, 0x11, 0x45, 0x02,
			0x00, 0x0a, 0x00, 0x00,
			0x00
		};

		// S7 STOP request
		byte[] S7_STOP = {
			0x03, 0x00, 0x00, 0x21,
			0x02, 0xf0, 0x80, 0x32,
			0x01, 0x00, 0x00, 0x0e,
			0x00, 0x00, 0x10, 0x00,
			0x00, 0x29, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x09,
			0x50, 0x5f, 0x50, 0x52,
			0x4f, 0x47, 0x52, 0x41,
			0x4d
		};

		// S7 HOT Start request
		byte[] S7_HOT_START = {
			0x03, 0x00, 0x00, 0x25,
			0x02, 0xf0, 0x80, 0x32,
			0x01, 0x00, 0x00, 0x0c,
			0x00, 0x00, 0x14, 0x00,
			0x00, 0x28, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00,
			0xfd, 0x00, 0x00, 0x09,
			0x50, 0x5f, 0x50, 0x52,
			0x4f, 0x47, 0x52, 0x41,
			0x4d
		};

		// S7 COLD Start request
		byte[] S7_COLD_START = {
			0x03, 0x00, 0x00, 0x27,
			0x02, 0xf0, 0x80, 0x32,
			0x01, 0x00, 0x00, 0x0f,
			0x00, 0x00, 0x16, 0x00,
			0x00, 0x28, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00,
			0xfd, 0x00, 0x02, 0x43,
			0x20, 0x09, 0x50, 0x5f,
			0x50, 0x52, 0x4f, 0x47,
			0x52, 0x41, 0x4d
		};
		const byte pduStart          = 0x28;   // CPU start
		const byte pduStop           = 0x29;   // CPU stop
		const byte pduAlreadyStarted = 0x02;   // CPU already in run mode
		const byte pduAlreadyStopped = 0x07;   // CPU already in stop mode

		// S7 Get PLC Status 
		byte[] S7_GET_STAT = {
			0x03, 0x00, 0x00, 0x21,
			0x02, 0xf0, 0x80, 0x32,
			0x07, 0x00, 0x00, 0x2c,
			0x00, 0x00, 0x08, 0x00,
			0x08, 0x00, 0x01, 0x12,
			0x04, 0x11, 0x44, 0x01,
			0x00, 0xff, 0x09, 0x00,
			0x04, 0x04, 0x24, 0x00,
			0x00
		};

		// S7 Get Block Info Request Header (contains also ISO Header and COTP Header)
		byte[] S7_BI = {
			0x03, 0x00, 0x00, 0x25, 
			0x02, 0xf0, 0x80, 0x32, 
			0x07, 0x00, 0x00, 0x05, 
			0x00, 0x00, 0x08, 0x00, 
			0x0c, 0x00, 0x01, 0x12, 
			0x04, 0x11, 0x43, 0x03, 
			0x00, 0xff, 0x09, 0x00, 
			0x08, 0x30, 
			0x41, // Block Type
			0x30, 0x30, 0x30, 0x30, 0x30, // ASCII Block Number
			0x41 
		};    

		#endregion

		#region [Internals]

		// Defaults
		private static int ISOTCP = 102; // ISOTCP Port
		private static int MinPduSize = 16;
		private static int MinPduSizeToRequest = 240;
		private static int MaxPduSizeToRequest = 960;
		private static int DefaultTimeout = 2000;
		private static int IsoHSize = 7; // TPKT+COTP Header Size

		// Properties
		private int _PDULength = 0;
		private int _PduSizeRequested = 480;
		private int _PLCPort = ISOTCP;

		// Privates
		private string IPAddress;
		private byte LocalTSAP_HI;
		private byte LocalTSAP_LO;
		private byte RemoteTSAP_HI;
		private byte RemoteTSAP_LO;
		private byte LastPDUType;
		private ushort ConnType = CONNTYPE_PG;
		private byte[] PDU = new byte[2048];
		private MsgSocket Socket = null;
		private int Time_ms = 0;

		private void CreateSocket()
		{
			Socket = new MsgSocket();
			Socket.ConnectTimeout = DefaultTimeout;
			Socket.ReadTimeout = DefaultTimeout;
			Socket.WriteTimeout = DefaultTimeout;
		}

		private int TCPConnect()
		{           
			if (_LastError==0)
			{
				try
				{
					_LastError=Socket.Connect(IPAddress, _PLCPort);
				}
				catch
				{
					_LastError = S7Consts.errTCPConnectionFailed;
				}
			}
			return _LastError;
		}
       
		private void RecvPacket(byte[] Buffer, int Start, int Size)
		{
			if (Connected)
				_LastError = Socket.Receive(Buffer, Start, Size);
			else
				_LastError = S7Consts.errTCPNotConnected;
		}

		private void SendPacket(byte[] Buffer, int Len)
		{
			if (Connected)
				_LastError = Socket.Send(Buffer, Len);
			else
				_LastError = S7Consts.errTCPNotConnected;
		}

		private void SendPacket(byte[] Buffer)
		{
			SendPacket(Buffer, Buffer.Length);
		}

		private int RecvIsoPacket()
		{
			Boolean Done = false;
			int Size = 0;
			while ((_LastError == 0) && !Done)
			{
				// Get TPKT (4 bytes)
				RecvPacket(PDU, 0, 4);
				if (_LastError == 0)
				{
					Size = PDU.GetWordAt(2);
					// Check 0 bytes Data Packet (only TPKT+COTP = 7 bytes)
					if (Size == IsoHSize)
						RecvPacket(PDU, 4, 3); // Skip remaining 3 bytes and Done is still false
					else
					{
						if ((Size > _PduSizeRequested + IsoHSize) || (Size < MinPduSize))
							_LastError = S7Consts.errIsoInvalidPDU;
						else
							Done = true; // a valid Length !=7 && >16 && <247
					}
				}
			}
			if (_LastError == 0)
			{
				RecvPacket(PDU, 4, 3); // Skip remaining 3 COTP bytes
				LastPDUType = PDU[5];   // Stores PDU Type, we need it 
				// Receives the S7 Payload          
				RecvPacket(PDU, 7, Size - IsoHSize);
			}

			if (_LastError == 0)
			{
				return Size;
			}
				
			return 0;
		}

		private int ISOConnect()
		{
			int Size;
			ISO_CR[16] = LocalTSAP_HI;
			ISO_CR[17] = LocalTSAP_LO;
			ISO_CR[20] = RemoteTSAP_HI;
			ISO_CR[21] = RemoteTSAP_LO;

			// Sends the connection request telegram      
			SendPacket(ISO_CR);
			if (_LastError == 0)
			{
				// Gets the reply (if any)
				Size = RecvIsoPacket();
				if (_LastError == 0)
				{
					if (Size == 22)
					{
						if (LastPDUType != (byte)0xD0) // 0xD0 = CC Connection confirm
							_LastError = S7Consts.errIsoConnect;
					}
					else
						_LastError = S7Consts.errIsoInvalidPDU;
				}
			}
			return _LastError;
		}

		private int NegotiatePduLength()
		{
			int Length;
			// Set PDU Size Requested
			S7_PN.SetWordAt(23, (ushort)_PduSizeRequested);
			// Sends the connection request telegram
			SendPacket(S7_PN);
			if (_LastError == 0)
			{
				Length = RecvIsoPacket();
				if (_LastError == 0)
				{
					// check S7 Error
					if ((Length == 27) && (PDU[17] == 0) && (PDU[18] == 0))  // 20 = size of Negotiate Answer
					{
						// Get PDU Size Negotiated
						_PDULength = PDU.GetWordAt(25);
						if (_PDULength <= 0)
							_LastError = S7Consts.errCliNegotiatingPDU;
					}
					else
						_LastError = S7Consts.errCliNegotiatingPDU;
				}
			}
			return _LastError;
		}

		private int CpuError(ushort Error)
		{
			switch(Error)
			{
				case 0                          : return 0;
				case Code7AddressOutOfRange     : return S7Consts.errCliAddressOutOfRange;
				case Code7InvalidTransportSize  : return S7Consts.errCliInvalidTransportSize;
				case Code7WriteDataSizeMismatch : return S7Consts.errCliWriteDataSizeMismatch;
				case Code7ResItemNotAvailable   :
				case Code7ResItemNotAvailable1  : return S7Consts.errCliItemNotAvailable;
				case Code7DataOverPDU           : return S7Consts.errCliSizeOverPDU;
				case Code7InvalidValue          : return S7Consts.errCliInvalidValue;
				case Code7FunNotAvailable       : return S7Consts.errCliFunNotAvailable;
				case Code7NeedPassword          : return S7Consts.errCliNeedPassword;
				case Code7InvalidPassword       : return S7Consts.errCliInvalidPassword;
				case Code7NoPasswordToSet       :
				case Code7NoPasswordToClear     : return S7Consts.errCliNoPasswordToSetOrClear;
				default:
					return S7Consts.errCliFunctionRefused;
			};
		}

		#endregion

		#region [Class Control]

		public S7Client(string name) : this()
		{
			Name = name;
		}

		public string Name { get; }

		public S7Client()
		{
			CreateSocket();
		}

		~S7Client()
		{
			Disconnect();
		}

		public override string ToString()
		{ 
			return $"PLC {Name ?? string.Empty}@{PLCIpAddress ?? "0.0.0.0"}";
		}

		public int Connect()
		{
			_LastError = 0;
			Time_ms = 0;
			int Elapsed = Environment.TickCount;
			if (!Connected)
			{
				TCPConnect(); // First stage : TCP Connection
				if (_LastError == 0) 
				{
					ISOConnect(); // Second stage : ISOTCP (ISO 8073) Connection
					if (_LastError == 0) 
					{
						_LastError = NegotiatePduLength(); // Third stage : S7 PDU negotiation
					}
				}
			}
			if (_LastError != 0)
				Disconnect();
			else
				Time_ms = Environment.TickCount - Elapsed;

			return _LastError;
		}

		public int ConnectTo(string Address, int Rack, int Slot)
		{
			UInt16 RemoteTSAP = (UInt16)((ConnType << 8) + (Rack * 0x20) + Slot);
			SetConnectionParams(Address, 0x0100, RemoteTSAP);
			return Connect();
		}

		public int SetConnectionParams(string Address, ushort LocalTSAP, ushort RemoteTSAP)
		{
			int LocTSAP = LocalTSAP & 0x0000FFFF;
			int RemTSAP = RemoteTSAP & 0x0000FFFF;
			IPAddress = Address;
			LocalTSAP_HI = (byte)(LocTSAP >> 8);
			LocalTSAP_LO = (byte)(LocTSAP & 0x00FF);
			RemoteTSAP_HI = (byte)(RemTSAP >> 8);
			RemoteTSAP_LO = (byte)(RemTSAP & 0x00FF);
			return 0;
		}

		public int SetConnectionType(ushort ConnectionType)
		{
			ConnType = ConnectionType;
			return 0;
		}

		public int Disconnect()
		{
			Socket?.Close();
			return 0;
		}

		public int GetParam(Int32 ParamNumber, ref int Value)
		{
			int Result = 0;
			switch (ParamNumber)
			{
				case S7Consts.p_u16_RemotePort:
				{
					Value = PLCPort;
					break;
				}
				case S7Consts.p_i32_PingTimeout:
				{
					Value = ConnTimeout;
					break;
				}
				case S7Consts.p_i32_SendTimeout:
				{
					Value = SendTimeout;
					break;
				}
				case S7Consts.p_i32_RecvTimeout:
				{
					Value = RecvTimeout;
					break;
				}
				case S7Consts.p_i32_PDURequest:
				{
					Value = PduSizeRequested;
					break;
				}
				default:
				{
					Result = S7Consts.errCliInvalidParamNumber;
					break;
				}
			}
			return Result;
		}

		// Set Properties for compatibility with Snap7.net.cs
		public int SetParam(Int32 ParamNumber, ref int Value)
		{
			int Result = 0;
			switch(ParamNumber)
			{
				case S7Consts.p_u16_RemotePort:
				{
					PLCPort = Value;
					break;
				}
				case S7Consts.p_i32_PingTimeout:
				{
					ConnTimeout = Value;
					break;
				}
				case S7Consts.p_i32_SendTimeout:
				{
					SendTimeout = Value;
					break;
				}
				case S7Consts.p_i32_RecvTimeout:
				{
					RecvTimeout = Value;
					break;
				}
				case S7Consts.p_i32_PDURequest:
				{
					PduSizeRequested = Value;
					break;
				}
				default:
				{
					Result = S7Consts.errCliInvalidParamNumber;
					break;
				}
			}
			return Result; 
		}

		public delegate void S7CliCompletion(IntPtr usrPtr, int opCode, int opResult);
		public int SetAsCallBack(S7CliCompletion Completion, IntPtr usrPtr)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		#endregion

		#region [Data I/O main functions]

		public int ReadArea(S7Area Area, int DBNumber, int Start, int Amount, S7WordLength WordLen, byte[] Buffer)
		{
			return ReadArea((int)Area, DBNumber, Start, Amount, (int)WordLen, Buffer);
		}

		public int ReadArea(S7Area Area, int DBNumber, int Start, int Amount, S7WordLength WordLen, byte[] Buffer, ref int BytesRead)
		{
			return ReadArea((int)Area, DBNumber, Start, Amount, (int)WordLen, Buffer, ref BytesRead);
		}
		public int ReadArea(int Area, int DBNumber, int Start, int Amount, int WordLen, byte[] Buffer)
		{
			int BytesRead = 0;
			return ReadArea(Area, DBNumber, Start, Amount, WordLen, Buffer, ref BytesRead);
		}

		public int ReadArea(int Area, int DBNumber, int Start, int Amount, int WordLen, byte[] Buffer, ref int BytesRead)
		{
			int Address;
			int NumElements;
			int MaxElements;
			int TotElements;
			int SizeRequested;
			int Length;
			int Offset = 0;
			int WordSize = 1;

			_LastError = 0;
			Time_ms = 0;
			int Elapsed = Environment.TickCount;
			// Some adjustment
			if (Area == (int)S7Area.CT)
				WordLen = (int)S7WordLength.Counter;
			if (Area == (int)S7Area.TM)
				WordLen = (int)S7WordLength.Timer;

			// Calc Word size          
			WordSize = WordLen.DataSizeByte();
			if (WordSize == 0)
				return S7Consts.errCliInvalidWordLen;

			if (WordLen == (int)S7WordLength.Bit)
				Amount = 1;  // Only 1 bit can be transferred at time
			else
			{
				if ((WordLen != (int)S7WordLength.Counter) && (WordLen != (int)S7WordLength.Timer))
				{
					Amount = Amount * WordSize;
					WordSize = 1;
					WordLen = (int)S7WordLength.Byte;
				}
			}        

			MaxElements = (_PDULength - 18) / WordSize; // 18 = Reply telegram header
			TotElements = Amount;

			while ((TotElements > 0) && (_LastError == 0))
			{
				NumElements = TotElements;
				if (NumElements > MaxElements)
					NumElements = MaxElements;

				SizeRequested = NumElements * WordSize;

				// Setup the telegram
				Array.Copy(S7_RW, 0, PDU, 0, Size_RD);
				// Set DB Number
				PDU[27] = (byte)Area;
				// Set Area
				if (Area == (int)S7Area.DB)
					PDU.SetWordAt(25, (ushort)DBNumber);

				// Adjusts Start and word length
				if ((WordLen == (int)S7WordLength.Bit) || (WordLen == (int)S7WordLength.Counter) || (WordLen == (int)S7WordLength.Timer))
				{
					Address = Start;
					PDU[22] = (byte)WordLen;
				}
				else
					Address = Start << 3;

				// Num elements
				PDU.SetWordAt(23, (ushort)NumElements);

				// Address into the PLC (only 3 bytes)           
				PDU[30] = (byte)(Address & 0x0FF);
				Address = Address >> 8;
				PDU[29] = (byte)(Address & 0x0FF);
				Address = Address >> 8;
				PDU[28] = (byte)(Address & 0x0FF);

				SendPacket(PDU, Size_RD);
				if (_LastError == 0)
				{
					Length = RecvIsoPacket();
					if (_LastError == 0)
					{
						if (Length<25)
							_LastError = S7Consts.errIsoInvalidDataSize;
						else
						{
							if (PDU[21] != 0xFF)
								_LastError = CpuError(PDU[21]);
							else
							{
								Array.Copy(PDU, 25, Buffer, Offset, SizeRequested);
								Offset += SizeRequested;
							}
						}
					}
				}
				TotElements -= NumElements;
				Start += NumElements * WordSize;
			}

			if (_LastError == 0)
			{
				BytesRead = Offset;
				Time_ms = Environment.TickCount - Elapsed;
			}
			else
				BytesRead = 0;
			return _LastError;
		}

		public int WriteArea(S7Area Area, int DBNumber, int Start, int Amount, S7WordLength WordLen, byte[] Buffer)
		{
			int BytesWritten = 0;
			return WriteArea((int) Area, DBNumber, Start, Amount, (int) WordLen, Buffer, ref BytesWritten);
		}

		public int WriteArea(S7Area Area, int DBNumber, int Start, int Amount, S7WordLength WordLen, byte[] Buffer, ref int BytesWritten)
		{
			return WriteArea((int) Area, DBNumber, Start, Amount, (int) WordLen, Buffer, ref BytesWritten);
		}		
		public int WriteArea(int Area, int DBNumber, int Start, int Amount, int WordLen, byte[] Buffer)
		{
			int BytesWritten = 0;
			return WriteArea(Area, DBNumber, Start, Amount, WordLen, Buffer, ref BytesWritten);
		}

		public int WriteArea(int Area, int DBNumber, int Start, int Amount, int WordLen, byte[] Buffer, ref int BytesWritten)
		{
			int Address;
			int NumElements;
			int MaxElements;
			int TotElements;
			int DataSize;
			int IsoSize;
			int Length;
			int Offset = 0;
			int WordSize = 1;

			_LastError = 0;
			Time_ms = 0;
			int Elapsed = Environment.TickCount;
			// Some adjustment
			if (Area == (int)S7Area.CT)
				WordLen = (int)S7WordLength.Counter;
			if (Area == (int)S7Area.TM)
				WordLen = (int)S7WordLength.Timer;

			// Calc Word size          
			WordSize = WordLen.DataSizeByte();
			if (WordSize == 0)
				return S7Consts.errCliInvalidWordLen;

			if (WordLen == (int)S7WordLength.Bit) // Only 1 bit can be transferred at time
				Amount = 1;
			else
			{
				if ((WordLen != (int)S7WordLength.Counter) && (WordLen != (int)S7WordLength.Timer))
				{
					Amount = Amount * WordSize;
					WordSize = 1;
					WordLen = (int)S7WordLength.Byte;
				}
			}        

			MaxElements = (_PDULength - 35) / WordSize; // 35 = Reply telegram header
			TotElements = Amount;

			while ((TotElements > 0) && (_LastError == 0))
			{
				NumElements = TotElements;
				if (NumElements > MaxElements)
					NumElements = MaxElements;

				DataSize = NumElements * WordSize;
				IsoSize = Size_WR + DataSize;

				// Setup the telegram
				Array.Copy(S7_RW, 0, PDU, 0, Size_WR);
				// Whole telegram Size
				PDU.SetWordAt(2, (ushort)IsoSize);
				// Data Length
				Length = DataSize + 4;
				PDU.SetWordAt(15, (ushort)Length);
				// Function
				PDU[17] = (byte)0x05;
				// Set DB Number
				PDU[27] = (byte)Area;
				if (Area == (int)S7Area.DB)
					PDU.SetWordAt(25, (ushort)DBNumber);


				// Adjusts Start and word length
				if ((WordLen == (int)S7WordLength.Bit) || (WordLen == (int)S7WordLength.Counter) || (WordLen == (int)S7WordLength.Timer))
				{
					Address = Start;
					Length = DataSize;
					PDU[22] = (byte)WordLen;
				}
				else
				{
					Address = Start << 3;
					Length = DataSize << 3;
				}

				// Num elements
				PDU.SetWordAt(23, (ushort)NumElements);
				// Address into the PLC
				PDU[30] = (byte)(Address & 0x0FF);
				Address = Address >> 8;
				PDU[29] = (byte)(Address & 0x0FF);
				Address = Address >> 8;
				PDU[28] = (byte)(Address & 0x0FF);

				// Transport Size
				switch (WordLen)
				{
					case (int)S7WordLength.Bit:
						PDU[32] = TS_ResBit;
						break;
					case (int)S7WordLength.Counter:
					case (int)S7WordLength.Timer:
						PDU[32] = TS_ResOctet;
						break;
					default:
						PDU[32] = TS_ResByte; // byte/word/dword etc.
						break;
				};
				// Length
				PDU.SetWordAt(33, (ushort)Length);

				// Copies the Data
				Array.Copy(Buffer, Offset, PDU, 35, DataSize);

				SendPacket(PDU, IsoSize);
				if (_LastError == 0)
				{
					Length = RecvIsoPacket();
					if (_LastError == 0)
					{
						if (Length == 22)
						{
							if (PDU[21] != (byte)0xFF)
								_LastError = CpuError(PDU[21]);
						}
						else
							_LastError = S7Consts.errIsoInvalidPDU;
					}
				}
				Offset += DataSize;
				TotElements -= NumElements;
				Start += NumElements * WordSize;
			}

			if (_LastError == 0)
			{
				BytesWritten = Offset;
				Time_ms = Environment.TickCount - Elapsed;
			}
			else
				BytesWritten = 0;

			return _LastError;
		}

		public int ReadMultiVars(S7DataItem[] Items, int ItemsCount)
		{
			int Offset;
			int Length;
			int ItemSize;
			byte[] S7Item = new byte[12];
			byte[] S7ItemRead = new byte[1024]; 
            
			_LastError = 0;
			Time_ms = 0;
			int Elapsed = Environment.TickCount;

			// Checks items
			if (ItemsCount > MaxVars)
				return S7Consts.errCliTooManyItems;
            
			// Fills Header
			Array.Copy(S7_MRD_HEADER, 0, PDU, 0, S7_MRD_HEADER.Length);
			PDU.SetWordAt(13, (ushort)(ItemsCount * S7Item.Length + 2));
			PDU[18] = (byte)ItemsCount;
			// Fills the Items
			Offset = 19;
			for (int c = 0; c < ItemsCount; c++)
			{
				Array.Copy(S7_MRD_ITEM, S7Item, S7Item.Length);
				S7Item[3] = (byte)Items[c].WordLen;
				S7Item.SetWordAt(4, (ushort)Items[c].Amount);
				if (Items[c].Area == (int)S7Area.DB)
					S7Item.SetWordAt(6, (ushort)Items[c].DBNumber);
				S7Item[8] = (byte)Items[c].Area;
                
				// Address into the PLC
				int Address = Items[c].Start;
				S7Item[11] = (byte)(Address & 0x0FF);
				Address = Address >> 8;
				S7Item[10] = (byte)(Address & 0x0FF);
				Address = Address >> 8;
				S7Item[09] = (byte)(Address & 0x0FF);

				Array.Copy(S7Item, 0, PDU, Offset, S7Item.Length);
				Offset += S7Item.Length;
			}

			if (Offset > _PDULength)
				return S7Consts.errCliSizeOverPDU;

			PDU.SetWordAt(2, (ushort)Offset); // Whole size
			SendPacket(PDU, Offset);
            
			if (_LastError != 0)
				return _LastError;
			// Get Answer
			Length = RecvIsoPacket();
			if (_LastError != 0)
				return _LastError;
			// Check ISO Length
			if (Length < 22)
			{
				_LastError = S7Consts.errIsoInvalidPDU; // PDU too Small
				return _LastError;
			}
			// Check Global Operation Result
			_LastError = CpuError(PDU.GetWordAt(17));
			if (_LastError != 0)
				return _LastError;
			// Get true ItemsCount
			int ItemsRead = PDU.GetByteAt(20);
			if ((ItemsRead != ItemsCount) || (ItemsRead>MaxVars))
			{
				_LastError = S7Consts.errCliInvalidPlcAnswer;
				return _LastError;
			}
			// Get Data
			Offset = 21;           
			for (int c = 0; c < ItemsCount; c++)
			{
				// Get the Item
				Array.Copy(PDU, Offset, S7ItemRead, 0, Length-Offset);
				if (S7ItemRead[0] == 0xff)
				{
					ItemSize = (int)S7ItemRead.GetWordAt(2);
					if ((S7ItemRead[1] != TS_ResOctet) && (S7ItemRead[1] != TS_ResReal) && (S7ItemRead[1] != TS_ResBit))
						ItemSize = ItemSize >> 3;
					Marshal.Copy(S7ItemRead, 4, Items[c].pData, ItemSize);
					Items[c].Result = 0;
					if (ItemSize % 2 != 0)
						ItemSize++; // Odd size are rounded
					Offset = Offset + 4 + ItemSize;
				}
				else
				{
					Items[c].Result = CpuError(S7ItemRead[0]);
					Offset += 4; // Skip the Item header                           
				}
			}         
			Time_ms = Environment.TickCount - Elapsed;
			return _LastError;
		}

		public int WriteMultiVars(S7DataItem[] Items, int ItemsCount)
		{
			int Offset;
			int ParLength;
			int DataLength;
			int ItemDataSize;
			byte[] S7ParItem = new byte[S7_MWR_PARAM.Length];
			byte[] S7DataItem = new byte[1024];

			_LastError = 0;
			Time_ms = 0;
			int Elapsed = Environment.TickCount;

			// Checks items
			if (ItemsCount > MaxVars)
				return S7Consts.errCliTooManyItems;
			// Fills Header
			Array.Copy(S7_MWR_HEADER, 0, PDU, 0, S7_MWR_HEADER.Length);
			ParLength = ItemsCount * S7_MWR_PARAM.Length + 2;
			PDU.SetWordAt(13, (ushort)ParLength);
			PDU[18] = (byte)ItemsCount;
			// Fills Params
			Offset = S7_MWR_HEADER.Length;
			for (int c=0; c<ItemsCount; c++)
			{
				Array.Copy(S7_MWR_PARAM, 0, S7ParItem, 0, S7_MWR_PARAM.Length);
				S7ParItem[3] = (byte)Items[c].WordLen;
				S7ParItem[8] = (byte)Items[c].Area;
				S7ParItem.SetWordAt(4, (ushort)Items[c].Amount);
				S7ParItem.SetWordAt(6, (ushort)Items[c].DBNumber);
				// Address into the PLC
				int Address = Items[c].Start;
				S7ParItem[11] = (byte)(Address & 0x0FF);
				Address = Address >> 8;
				S7ParItem[10] = (byte)(Address & 0x0FF);
				Address = Address >> 8;
				S7ParItem[09] = (byte)(Address & 0x0FF);
				Array.Copy(S7ParItem, 0, PDU, Offset, S7ParItem.Length);
				Offset += S7_MWR_PARAM.Length;
			}
			// Fills Data
			DataLength = 0;
			for (int c = 0; c < ItemsCount; c++)
			{
				S7DataItem[0] = 0x00;
				switch (Items[c].WordLen)
				{
					case (int)S7WordLength.Bit:
						S7DataItem[1] = TS_ResBit;
						break;
					case (int)S7WordLength.Counter:
					case (int)S7WordLength.Timer:
						S7DataItem[1] = TS_ResOctet;
						break;
					default:
						S7DataItem[1] = TS_ResByte; // byte/word/dword etc.
						break;
				};
				if ((Items[c].WordLen==(int)S7WordLength.Timer) || (Items[c].WordLen == (int)S7WordLength.Counter))
					ItemDataSize = Items[c].Amount * 2;
				else
					ItemDataSize = Items[c].Amount;

				if ((S7DataItem[1] != TS_ResOctet) && (S7DataItem[1] != TS_ResBit))
					S7DataItem.SetWordAt(2, (ushort)(ItemDataSize*8));
				else
					S7DataItem.SetWordAt(2, (ushort)ItemDataSize);

				Marshal.Copy(Items[c].pData, S7DataItem, 4, ItemDataSize);
				if (ItemDataSize % 2 != 0)
				{
					S7DataItem[ItemDataSize+4] = 0x00;
					ItemDataSize++;
				}
				Array.Copy(S7DataItem, 0, PDU, Offset, ItemDataSize+4);
				Offset = Offset + ItemDataSize + 4;
				DataLength = DataLength + ItemDataSize + 4;
			}

			// Checks the size
			if (Offset > _PDULength)
				return S7Consts.errCliSizeOverPDU;

			PDU.SetWordAt(2, (ushort)Offset); // Whole size
			PDU.SetWordAt(15, (ushort)DataLength); // Whole size
			SendPacket(PDU, Offset);

			RecvIsoPacket();
			if (_LastError==0)
			{
				// Check Global Operation Result
				_LastError = CpuError(PDU.GetWordAt(17));
				if (_LastError != 0)
					return _LastError;
				// Get true ItemsCount
				int ItemsWritten = PDU.GetByteAt(20);
				if ((ItemsWritten != ItemsCount) || (ItemsWritten > MaxVars))
				{
					_LastError = S7Consts.errCliInvalidPlcAnswer;
					return _LastError;
				}

				for (int c=0; c<ItemsCount; c++)
				{
					if (PDU[c + 21] == 0xFF)
						Items[c].Result = 0;
					else
						Items[c].Result = CpuError((ushort)PDU[c + 21]);
				}
				Time_ms = Environment.TickCount - Elapsed;
			}
			return _LastError;
		}

		#endregion

		#region [Data I/O lean functions]

		public int DBRead(int DBNumber, int Start, int Size, byte[] Buffer)
		{
			return ReadArea(S7Area.DB, DBNumber, Start, Size, S7WordLength.Byte, Buffer);
		}

		public int DBWrite(int DBNumber, int Start, int Size, byte[] Buffer)
		{
			return WriteArea(S7Area.DB, DBNumber, Start, Size, S7WordLength.Byte, Buffer);
		}

		public int MBRead(int Start, int Size, byte[] Buffer)
		{
			return ReadArea(S7Area.MK, 0, Start, Size, S7WordLength.Byte, Buffer);
		}

		public int MBWrite(int Start, int Size, byte[] Buffer)
		{
			return WriteArea(S7Area.MK, 0, Start, Size, S7WordLength.Byte, Buffer);
		}

		public int EBRead(int Start, int Size, byte[] Buffer)
		{
			return ReadArea(S7Area.PE, 0, Start, Size, S7WordLength.Byte, Buffer);
		}

		public int EBWrite(int Start, int Size, byte[] Buffer)
		{
			return WriteArea(S7Area.PE, 0, Start, Size, S7WordLength.Byte, Buffer);
		}

		public int ABRead(int Start, int Size, byte[] Buffer)
		{
			return ReadArea(S7Area.PA, 0, Start, Size, S7WordLength.Byte, Buffer);
		}

		public int ABWrite(int Start, int Size, byte[] Buffer)
		{
			return WriteArea(S7Area.PA, 0, Start, Size, S7WordLength.Byte, Buffer);
		}

		public int TMRead(int Start, int Amount, ushort[] Buffer)
		{
			byte[] sBuffer = new byte[Amount * 2];
			int Result = ReadArea(S7Area.TM, 0, Start, Amount, S7WordLength.Timer, sBuffer);
			if (Result == 0)
			{
				for (int c = 0; c < Amount; c++)
				{
					Buffer[c] = (ushort)((sBuffer[c * 2 + 1] << 8) + (sBuffer[c * 2]));
				}
			}
			return Result;
		}

		public int TMWrite(int Start, int Amount, ushort[] Buffer)
		{
			byte[] sBuffer = new byte[Amount * 2];
			for (int c = 0; c < Amount; c++)
			{
				sBuffer[c * 2 + 1] = (byte)((Buffer[c] & 0xFF00) >> 8);
				sBuffer[c * 2] = (byte)(Buffer[c] & 0x00FF);
			}
			return WriteArea(S7Area.TM, 0, Start, Amount, S7WordLength.Timer, sBuffer);
		}

		public int CTRead(int Start, int Amount, ushort[] Buffer)
		{
			byte[] sBuffer = new byte[Amount * 2];
			int Result = ReadArea(S7Area.CT, 0, Start, Amount, S7WordLength.Counter, sBuffer);
			if (Result==0)
			{
				for (int c=0; c<Amount; c++)
				{
					Buffer[c] = (ushort)((sBuffer[c * 2 + 1] << 8) + (sBuffer[c * 2]));
				}
			}
			return Result;
		}

		public int CTWrite(int Start, int Amount, ushort[] Buffer)
		{
			byte[] sBuffer = new byte[Amount * 2];
			for (int c = 0; c < Amount; c++)
			{
				sBuffer[c * 2 + 1] = (byte)((Buffer[c] & 0xFF00)>>8);
				sBuffer[c * 2]= (byte)(Buffer[c] & 0x00FF);
			}
			return WriteArea(S7Area.CT, 0, Start, Amount, S7WordLength.Counter, sBuffer);
		}

		#endregion

		#region [Directory functions]

		public int ListBlocks(ref S7BlocksList List)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		private string SiemensTimestamp(long EncodedDate)
		{
			DateTime DT = new DateTime(1984, 1, 1).AddSeconds(EncodedDate*86400);
#if WINDOWS_UWP || NETFX_CORE || CORE_CLR
            return DT.ToString(System.Globalization.DateTimeFormatInfo.CurrentInfo.ShortDatePattern);
#else
			return DT.ToShortDateString();
#endif
		}

		public int GetAgBlockInfo(int BlockType, int BlockNum, ref S7BlockInfo Info)
		{
			_LastError = 0;
			Time_ms = 0;
			int Elapsed = Environment.TickCount;

			S7_BI[30] = (byte)BlockType;
			// Block Number
			S7_BI[31] = (byte)((BlockNum / 10000) + 0x30);
			BlockNum = BlockNum % 10000;
			S7_BI[32] = (byte)((BlockNum / 1000) + 0x30);
			BlockNum = BlockNum % 1000;
			S7_BI[33] = (byte)((BlockNum / 100) + 0x30);
			BlockNum = BlockNum % 100;
			S7_BI[34] = (byte)((BlockNum / 10) + 0x30);
			BlockNum = BlockNum % 10;
			S7_BI[35] = (byte)((BlockNum / 1) + 0x30);

			SendPacket(S7_BI);

			if (_LastError == 0)
			{
				int Length = RecvIsoPacket();
				if (Length > 32) // the minimum expected
				{
					ushort Result = PDU.GetWordAt(27);
					if (Result == 0)
					{
						Info.BlkFlags= PDU[42];
						Info.BlkLang = PDU[43];
						Info.BlkType = PDU[44];
						Info.BlkNumber = PDU.GetWordAt(45);
						Info.LoadSize = PDU.GetDIntAt(47);
						Info.CodeDate = SiemensTimestamp(PDU.GetWordAt(59));
						Info.IntfDate = SiemensTimestamp(PDU.GetWordAt(65)); 
						Info.SBBLength = PDU.GetWordAt(67);
						Info.LocalData = PDU.GetWordAt(71);
						Info.MC7Size = PDU.GetWordAt(73);
						Info.Author = PDU.GetCharsAt(75, 8).Trim(new char[]{(char)0});
						Info.Family = PDU.GetCharsAt(83, 8).Trim(new char[]{(char)0});
						Info.Header = PDU.GetCharsAt(91, 8).Trim(new char[]{(char)0}); 
						Info.Version = PDU[99];
						Info.CheckSum = PDU.GetWordAt(101);
					}
					else
						_LastError = CpuError(Result);
				}
				else
					_LastError = S7Consts.errIsoInvalidPDU;
			}
			if (_LastError == 0)
				Time_ms = Environment.TickCount - Elapsed;

			return _LastError;
            
		}

		public int GetPgBlockInfo(ref S7BlockInfo Info, byte[] Buffer, int Size)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int ListBlocksOfType(int BlockType, ushort[] List, ref int ItemsCount)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		#endregion

		#region [Blocks functions]

		public int Upload(int BlockType, int BlockNum, byte[] UsrData, ref int Size)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int FullUpload(int BlockType, int BlockNum, byte[] UsrData, ref int Size)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int Download(int BlockNum, byte[] UsrData, int Size)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int Delete(int BlockType, int BlockNum)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int DBGet(int DBNumber, byte[] UsrData, ref int Size)
		{
			S7BlockInfo BI = new S7BlockInfo();
			int Elapsed = Environment.TickCount;
			Time_ms = 0;

			_LastError = GetAgBlockInfo(Block_DB, DBNumber, ref BI);

			if (_LastError==0)
			{
				int DBSize = BI.MC7Size;
				if (DBSize <= UsrData.Length)
				{
					Size = DBSize;
					_LastError = DBRead(DBNumber, 0, DBSize, UsrData);
					if (_LastError == 0)
						Size = DBSize;
				}
				else
					_LastError = S7Consts.errCliBufferTooSmall;                  
			}
			if (_LastError == 0)
				Time_ms = Environment.TickCount - Elapsed;
			return _LastError;
		}

		public int DBFill(int DBNumber, int FillChar)
		{
			S7BlockInfo BI = new S7BlockInfo();
			int Elapsed = Environment.TickCount;
			Time_ms = 0;
            
			_LastError = GetAgBlockInfo(Block_DB, DBNumber, ref BI);
            
			if (_LastError == 0)
			{
				byte[] Buffer = new byte[BI.MC7Size];
				for (int c = 0; c < BI.MC7Size; c++)
					Buffer[c] = (byte)FillChar;
				_LastError = DBWrite(DBNumber, 0, BI.MC7Size, Buffer);
			}
			if (_LastError == 0)
				Time_ms = Environment.TickCount - Elapsed;
			return _LastError;
		}

		#endregion

		#region [Date/Time functions]

		public int GetPlcDateTime(ref DateTime DT)
		{
			int Length;
			_LastError = 0;
			Time_ms = 0;
			int Elapsed = Environment.TickCount;

			SendPacket(S7_GET_DT);
			if (_LastError == 0)
			{
				Length = RecvIsoPacket();
				if (Length > 30) // the minimum expected
				{
					if ((PDU.GetWordAt(27) == 0) && (PDU[29] == 0xFF))
					{
						DT = PDU.GetDateTimeAt(35);
					}
					else
						_LastError = S7Consts.errCliInvalidPlcAnswer;
				}
				else
					_LastError = S7Consts.errIsoInvalidPDU;
			}

			if(_LastError==0)
				Time_ms = Environment.TickCount - Elapsed;

			return _LastError;
		}

		public int SetPlcDateTime(DateTime DT)
		{
			int Length;
			_LastError = 0;
			Time_ms = 0;
			int Elapsed = Environment.TickCount;

			S7_SET_DT.SetDateTimeAt(31, DT);
			SendPacket(S7_SET_DT);
			if (_LastError == 0)
			{
				Length = RecvIsoPacket();
				if (Length > 30) // the minimum expected
				{
					if (PDU.GetWordAt(27) != 0)
						_LastError = S7Consts.errCliInvalidPlcAnswer;
				}
				else
					_LastError = S7Consts.errIsoInvalidPDU;
			}
			if (_LastError == 0)
				Time_ms = Environment.TickCount - Elapsed;

			return _LastError;
		}

		public int SetPlcSystemDateTime()
		{
			return SetPlcDateTime(DateTime.Now);
		}

		#endregion

		#region [System Info functions]

		public int GetOrderCode(ref S7OrderCode Info)
		{
			S7SZL SZL = new S7SZL();
			int Size = 1024;
			SZL.Data = new byte[Size];
			int Elapsed = Environment.TickCount;
			_LastError = ReadSZL(0x0011, 0x000, ref SZL, ref Size);
			if (_LastError == 0)
			{
				Info.Code = SZL.Data.GetCharsAt(2, 20);
				Info.V1 = SZL.Data[Size - 3];
				Info.V2 = SZL.Data[Size - 2];
				Info.V3 = SZL.Data[Size - 1];
			}
			if (_LastError == 0)
				Time_ms = Environment.TickCount - Elapsed;
			return _LastError;
		}

		public int GetCpuInfo(ref S7CpuInfo Info)
		{
			S7SZL SZL = new S7SZL();
			int Size = 1024;
			SZL.Data = new byte[Size];
			int Elapsed = Environment.TickCount;
			_LastError = ReadSZL(0x001C, 0x000, ref SZL, ref Size);
			if (_LastError == 0)
			{
				Info.ModuleTypeName = SZL.Data.GetCharsAt(172, 32);
				Info.SerialNumber = SZL.Data.GetCharsAt(138, 24);
				Info.ASName = SZL.Data.GetCharsAt(2, 24);
				Info.Copyright = SZL.Data.GetCharsAt(104, 26);
				Info.ModuleName = SZL.Data.GetCharsAt(36, 24);
			}
			if (_LastError == 0)
				Time_ms = Environment.TickCount - Elapsed;
			return _LastError;
		}

		public int GetCpInfo(ref S7CpInfo Info)
		{
			S7SZL SZL = new S7SZL();
			int Size = 1024;
			SZL.Data = new byte[Size];
			int Elapsed = Environment.TickCount;
			_LastError = ReadSZL(0x0131, 0x001, ref SZL, ref Size);
			if (_LastError == 0)
			{
				Info.MaxPduLength = PDU.GetIntAt(2);
				Info.MaxConnections = PDU.GetIntAt(4);
				Info.MaxMpiRate = PDU.GetDIntAt(6);
				Info.MaxBusRate = PDU.GetDIntAt(10);
			}
			if (_LastError == 0)
				Time_ms = Environment.TickCount - Elapsed;
			return _LastError;
		}

		public int ReadSZL(int ID, int Index, ref S7SZL SZL, ref int Size)
		{
			int Length;
			int DataSZL;
			int Offset = 0;
			bool Done = false;
			bool First = true;
			byte Seq_in = 0x00;
			ushort Seq_out = 0x0000;

			_LastError = 0;
			Time_ms = 0;
			int Elapsed = Environment.TickCount;
			SZL.Header.LENTHDR = 0;

			do
			{
				if (First)
				{
					S7_SZL_FIRST.SetWordAt(11, ++Seq_out);
					S7_SZL_FIRST.SetWordAt(29, (ushort)ID);
					S7_SZL_FIRST.SetWordAt(31, (ushort)Index);
					SendPacket(S7_SZL_FIRST);
				}
				else
				{
					S7_SZL_NEXT.SetWordAt(11, ++Seq_out);
					PDU[24] = (byte)Seq_in;
					SendPacket(S7_SZL_NEXT);
				}
				if (_LastError != 0)
					return _LastError;

				Length = RecvIsoPacket();
				if (_LastError == 0)
				{
					if (First)
					{
						if (Length > 32) // the minimum expected
						{
							if ((PDU.GetWordAt(27) == 0) && (PDU[29] == (byte)0xFF))
							{
								// Gets Amount of this slice
								DataSZL = PDU.GetWordAt(31) - 8; // Skips extra params (ID, Index ...)
								Done = PDU[26] == 0x00;
								Seq_in = (byte)PDU[24]; // Slice sequence
								SZL.Header.LENTHDR = PDU.GetWordAt(37);
								SZL.Header.N_DR = PDU.GetWordAt(39);
								Array.Copy(PDU, 41, SZL.Data, Offset, DataSZL);
								//                                SZL.Copy(PDU, 41, Offset, DataSZL);
								Offset += DataSZL;
								SZL.Header.LENTHDR += SZL.Header.LENTHDR;
							}
							else
								_LastError = S7Consts.errCliInvalidPlcAnswer;
						}
						else
							_LastError = S7Consts.errIsoInvalidPDU;
					}
					else
					{
						if (Length > 32) // the minimum expected
						{
							if ((PDU.GetWordAt(27) == 0) && (PDU[29] == (byte)0xFF))
							{
								// Gets Amount of this slice
								DataSZL = PDU.GetWordAt(31);
								Done = PDU[26] == 0x00;
								Seq_in = (byte)PDU[24]; // Slice sequence
								Array.Copy(PDU, 37, SZL.Data, Offset, DataSZL);
								Offset += DataSZL;
								SZL.Header.LENTHDR += SZL.Header.LENTHDR;
							}
							else
								_LastError = S7Consts.errCliInvalidPlcAnswer;
						}
						else
							_LastError = S7Consts.errIsoInvalidPDU;
					}
				}
				First = false;
			}
			while (!Done && (_LastError == 0));
			if (_LastError==0)
			{
				Size = SZL.Header.LENTHDR;
				Time_ms = Environment.TickCount - Elapsed;
			}
			return _LastError;
		}

		public int ReadSZLList(ref S7SZLList List, ref Int32 ItemsCount)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		#endregion

		#region [Control functions]

		public int PlcHotStart()
		{
			_LastError = 0;
			int Elapsed = Environment.TickCount;

			SendPacket(S7_HOT_START);
			if (_LastError == 0)
			{
				int Length = RecvIsoPacket();
				if (Length > 18) // 18 is the minimum expected
				{
					if (PDU[19] != pduStart)
						_LastError = S7Consts.errCliCannotStartPLC;
					else
					{
						if (PDU[20] == pduAlreadyStarted)
							_LastError = S7Consts.errCliAlreadyRun;
						else
							_LastError = S7Consts.errCliCannotStartPLC;
					}
				}
				else
					_LastError = S7Consts.errIsoInvalidPDU;
			}
			if (_LastError == 0)
				Time_ms = Environment.TickCount - Elapsed;
			return _LastError;
		}

		public int PlcColdStart()
		{
			_LastError = 0;
			int Elapsed = Environment.TickCount;

			SendPacket(S7_COLD_START);
			if (_LastError == 0)
			{
				int Length = RecvIsoPacket();
				if (Length > 18) // 18 is the minimum expected
				{
					if (PDU[19] != pduStart)
						_LastError = S7Consts.errCliCannotStartPLC;
					else
					{
						if (PDU[20] == pduAlreadyStarted)
							_LastError = S7Consts.errCliAlreadyRun;
						else
							_LastError = S7Consts.errCliCannotStartPLC;
					}
				}
				else
					_LastError = S7Consts.errIsoInvalidPDU;
			}
			if (_LastError == 0)
				Time_ms = Environment.TickCount - Elapsed;
			return _LastError;
		}

		public int PlcStop()
		{
			_LastError = 0;
			int Elapsed = Environment.TickCount;

			SendPacket(S7_STOP);
			if (_LastError == 0)
			{
				int Length = RecvIsoPacket();
				if (Length > 18) // 18 is the minimum expected
				{
					if (PDU[19]!=pduStop)
						_LastError = S7Consts.errCliCannotStopPLC;
					else
					{ 
						if (PDU[20]== pduAlreadyStopped)
							_LastError = S7Consts.errCliAlreadyStop;
						else
							_LastError = S7Consts.errCliCannotStopPLC;
					}
				}
				else
					_LastError = S7Consts.errIsoInvalidPDU;
			}
			if (_LastError == 0)
				Time_ms = Environment.TickCount - Elapsed;
			return _LastError;
		}

		public int PlcCopyRamToRom(UInt32 Timeout)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int PlcCompress(UInt32 Timeout)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int PlcGetStatus(ref Int32 Status)
		{
			_LastError = 0;
			int Elapsed = Environment.TickCount;

			SendPacket(S7_GET_STAT);
			if (_LastError == 0)
			{
				int Length = RecvIsoPacket();
				if (Length > 30) // the minimum expected
				{
					ushort Result = PDU.GetWordAt(27);
					if (Result == 0)
					{
						switch (PDU[44])
						{
							case S7Consts.S7CpuStatusUnknown:
							case S7Consts.S7CpuStatusRun:
							case S7Consts.S7CpuStatusStop:
							{
								Status = PDU[44];
								break;
							}
							default:
							{
								// Since RUN status is always 0x08 for all CPUs and CPs, STOP status
								// sometime can be coded as 0x03 (especially for old cpu...)
								Status = S7Consts.S7CpuStatusStop;
								break;
							}
						}
					}
					else
						_LastError = CpuError(Result);
				}
				else
					_LastError = S7Consts.errIsoInvalidPDU;
			}
			if (_LastError == 0)
				Time_ms = Environment.TickCount - Elapsed;
			return _LastError;
		}

		#endregion

		#region [Security functions]
		public int SetSessionPassword(string Password)
		{
			byte[] pwd = { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
			int Length;
			_LastError = 0;
			int Elapsed = Environment.TickCount;
			// Encodes the Password
			pwd.SetCharsAt(0, Password);
			pwd[0] = (byte)(pwd[0] ^ 0x55);
			pwd[1] = (byte)(pwd[1] ^ 0x55);
			for (int c = 2; c < 8; c++)
			{
				pwd[c] = (byte)(pwd[c] ^ 0x55 ^ pwd[c - 2]);
			}
			Array.Copy(pwd, 0, S7_SET_PWD, 29, 8);
			// Sends the telegrem
			SendPacket(S7_SET_PWD);
			if (_LastError == 0)
			{
				Length = RecvIsoPacket();
				if (Length > 32) // the minimum expected
				{
					ushort Result = PDU.GetWordAt(27);
					if (Result != 0)
						_LastError = CpuError(Result);
				}
				else
					_LastError = S7Consts.errIsoInvalidPDU;
			}
			if (_LastError == 0)
				Time_ms = Environment.TickCount - Elapsed;
			return _LastError;
		}

		public int ClearSessionPassword()
		{
			int Length;
			_LastError = 0;
			int Elapsed = Environment.TickCount;
			SendPacket(S7_CLR_PWD);
			if (_LastError == 0)
			{
				Length = RecvIsoPacket();
				if (Length > 30) // the minimum expected
				{
					ushort Result = PDU.GetWordAt(27);
					if (Result != 0)
						_LastError = CpuError(Result);
				}
				else
					_LastError = S7Consts.errIsoInvalidPDU;
			}
			return _LastError;
		}

		public int GetProtection(ref S7Protection Protection)
		{
			S7Client.S7SZL SZL = new S7Client.S7SZL();
			int Size = 256;
			SZL.Data = new byte[Size];
			_LastError = ReadSZL(0x0232, 0x0004, ref SZL, ref Size);
			if (_LastError == 0)
			{
				Protection.sch_schal = SZL.Data.GetWordAt(2);
				Protection.sch_par = SZL.Data.GetWordAt(4);
				Protection.sch_rel = SZL.Data.GetWordAt(6);
				Protection.bart_sch = SZL.Data.GetWordAt(8);
				Protection.anl_sch = SZL.Data.GetWordAt(10);
			}
			return _LastError;
		}
		#endregion

		#region [Low Level]

		public int IsoExchangeBuffer(byte[] Buffer, ref Int32 Size)
		{
			_LastError = 0;
			Time_ms = 0;
			int Elapsed = Environment.TickCount;
			Array.Copy(TPKT_ISO, 0, PDU, 0, TPKT_ISO.Length);
			PDU.SetWordAt(2, (ushort)(Size + TPKT_ISO.Length));
			try
			{
				Array.Copy(Buffer, 0, PDU, TPKT_ISO.Length, Size);
			}
			catch
			{
				return S7Consts.errIsoInvalidPDU;
			}
			SendPacket(PDU, TPKT_ISO.Length + Size);
			if (_LastError==0)
			{
				int Length=RecvIsoPacket();
				if (_LastError==0)
				{
					Array.Copy(PDU, TPKT_ISO.Length, Buffer, 0, Length - TPKT_ISO.Length);
					Size = Length - TPKT_ISO.Length;
				}
			}
			if (_LastError == 0)
				Time_ms = Environment.TickCount - Elapsed;
			else
				Size = 0;
			return _LastError;
		}

		#endregion

		#region [Async functions (not implemented)]

		public int AsReadArea(int Area, int DBNumber, int Start, int Amount, int WordLen, byte[] Buffer)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int AsWriteArea(int Area, int DBNumber, int Start, int Amount, int WordLen, byte[] Buffer)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int AsDBRead(int DBNumber, int Start, int Size, byte[] Buffer)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int AsDBWrite(int DBNumber, int Start, int Size, byte[] Buffer)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int AsMBRead(int Start, int Size, byte[] Buffer)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int AsMBWrite(int Start, int Size, byte[] Buffer)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int AsEBRead(int Start, int Size, byte[] Buffer)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int AsEBWrite(int Start, int Size, byte[] Buffer)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int AsABRead(int Start, int Size, byte[] Buffer)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int AsABWrite(int Start, int Size, byte[] Buffer)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int AsTMRead(int Start, int Amount, ushort[] Buffer)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int AsTMWrite(int Start, int Amount, ushort[] Buffer)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int AsCTRead(int Start, int Amount, ushort[] Buffer)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int AsCTWrite(int Start, int Amount, ushort[] Buffer)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int AsListBlocksOfType(int BlockType, ushort[] List)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int AsReadSZL(int ID, int Index, ref S7SZL Data, ref Int32 Size)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int AsReadSZLList(ref S7SZLList List, ref Int32 ItemsCount)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int AsUpload(int BlockType, int BlockNum, byte[] UsrData, ref int Size)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int AsFullUpload(int BlockType, int BlockNum, byte[] UsrData, ref int Size)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int ASDownload(int BlockNum, byte[] UsrData, int Size)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int AsPlcCopyRamToRom(UInt32 Timeout)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int AsPlcCompress(UInt32 Timeout)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int AsDBGet(int DBNumber, byte[] UsrData, ref int Size)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public int AsDBFill(int DBNumber, int FillChar)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		public bool CheckAsCompletion(ref int opResult)
		{
			opResult = 0;
			return false;
		}

		public int WaitAsCompletion(int Timeout)
		{
			return S7Consts.errCliFunctionNotImplemented;
		}

		#endregion

		#region [Info Functions / Properties]

		public string ErrorText(int Error)
		{
			switch (Error)
			{
				case 0: return "OK";
				case S7Consts.errTCPSocketCreation: return "SYS: Error creating the Socket";
				case S7Consts.errTCPConnectionTimeout: return "TCP: Connection Timeout";
				case S7Consts.errTCPConnectionFailed: return "TCP: Connection Error";
				case S7Consts.errTCPReceiveTimeout: return "TCP: Data receive Timeout";
				case S7Consts.errTCPDataReceive: return "TCP: Error receiving Data";
				case S7Consts.errTCPSendTimeout: return "TCP: Data send Timeout";
				case S7Consts.errTCPDataSend: return "TCP: Error sending Data";
				case S7Consts.errTCPConnectionReset: return "TCP: Connection reset by the Peer";
				case S7Consts.errTCPNotConnected: return "CLI: Client not connected";
				case S7Consts.errTCPUnreachableHost: return "TCP: Unreachable host";
				case S7Consts.errIsoConnect: return "ISO: Connection Error";
				case S7Consts.errIsoInvalidPDU: return "ISO: Invalid PDU received";
				case S7Consts.errIsoInvalidDataSize: return "ISO: Invalid Buffer passed to Send/Receive";
				case S7Consts.errCliNegotiatingPDU: return "CLI: Error in PDU negotiation";
				case S7Consts.errCliInvalidParams: return "CLI: Invalid param(s) supplied";
				case S7Consts.errCliJobPending: return "CLI: Job pending";
				case S7Consts.errCliTooManyItems: return "CLI: Too many items (>20) in multi read/write";
				case S7Consts.errCliInvalidWordLen: return "CLI: Invalid WordLength";
				case S7Consts.errCliPartialDataWritten: return "CLI: Partial data written";
				case S7Consts.errCliSizeOverPDU: return "CPU: Total data exceeds the PDU size";
				case S7Consts.errCliInvalidPlcAnswer: return "CLI: Invalid CPU answer";
				case S7Consts.errCliAddressOutOfRange: return "CPU: Address out of range";
				case S7Consts.errCliInvalidTransportSize: return "CPU: Invalid Transport size";
				case S7Consts.errCliWriteDataSizeMismatch: return "CPU: Data size mismatch";
				case S7Consts.errCliItemNotAvailable: return "CPU: Item not available";
				case S7Consts.errCliInvalidValue: return "CPU: Invalid value supplied";
				case S7Consts.errCliCannotStartPLC: return "CPU: Cannot start PLC";
				case S7Consts.errCliAlreadyRun: return "CPU: PLC already RUN";
				case S7Consts.errCliCannotStopPLC: return "CPU: Cannot stop PLC";
				case S7Consts.errCliCannotCopyRamToRom: return "CPU: Cannot copy RAM to ROM";
				case S7Consts.errCliCannotCompress: return "CPU: Cannot compress";
				case S7Consts.errCliAlreadyStop: return "CPU: PLC already STOP";
				case S7Consts.errCliFunNotAvailable: return "CPU: Function not available";
				case S7Consts.errCliUploadSequenceFailed: return "CPU: Upload sequence failed";
				case S7Consts.errCliInvalidDataSizeRecvd: return "CLI: Invalid data size received";
				case S7Consts.errCliInvalidBlockType: return "CLI: Invalid block type";
				case S7Consts.errCliInvalidBlockNumber: return "CLI: Invalid block number";
				case S7Consts.errCliInvalidBlockSize: return "CLI: Invalid block size";
				case S7Consts.errCliNeedPassword: return "CPU: Function not authorized for current protection level";
				case S7Consts.errCliInvalidPassword: return "CPU: Invalid password";
				case S7Consts.errCliNoPasswordToSetOrClear: return "CPU: No password to set or clear";
				case S7Consts.errCliJobTimeout: return "CLI: Job Timeout";
				case S7Consts.errCliFunctionRefused: return "CLI: Function refused by CPU (Unknown error)";
				case S7Consts.errCliPartialDataRead: return "CLI: Partial data read";
				case S7Consts.errCliBufferTooSmall: return "CLI: The buffer supplied is too small to accomplish the operation";
				case S7Consts.errCliDestroying: return "CLI: Cannot perform (destroying)";
				case S7Consts.errCliInvalidParamNumber: return "CLI: Invalid Param Number";
				case S7Consts.errCliCannotChangeParam: return "CLI: Cannot change this param now";
				case S7Consts.errCliFunctionNotImplemented: return "CLI: Function not implemented";
				default: return "CLI: Unknown error (0x" + Convert.ToString(Error, 16) + ")";
			};
		}

		public int LastError()
		{
			return _LastError;
		}

		public int RequestedPduLength()
		{
			return _PduSizeRequested;
		}

		public int NegotiatedPduLength()
		{
			return _PDULength;
		}

		public int ExecTime()
		{
			return Time_ms;
		}

		public int ExecutionTime => Time_ms;

		public int PduSizeNegotiated => _PDULength;

		public int PduSizeRequested
		{
			get => _PduSizeRequested;
			set
			{
				if (value < MinPduSizeToRequest)
					value = MinPduSizeToRequest;
				if (value > MaxPduSizeToRequest)
					value = MaxPduSizeToRequest;
				_PduSizeRequested = value;       
			}
		}

		public string PLCIpAddress => IPAddress;

		public int PLCPort
		{
			get => _PLCPort;
			set => _PLCPort = value;
		}

		public int ConnTimeout
		{
			get => Socket.ConnectTimeout;
			set => Socket.ConnectTimeout = value;
		}

		public int RecvTimeout
		{
			get => Socket.ReadTimeout;
			set => Socket.ReadTimeout = value;
		}

		public int SendTimeout
		{
			get => Socket.WriteTimeout;
			set => Socket.WriteTimeout = value;
		}

		public bool Connected => (Socket != null) && (Socket.Connected);

		#endregion
	}
}