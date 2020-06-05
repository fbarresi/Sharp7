using System;
using System.Linq;
using System.Runtime.InteropServices;
#pragma warning disable 618

namespace Sharp7
{
	public class S7MultiVar
	{
		#region [MultiRead/Write Helper]
		private S7Client FClient;
		private GCHandle[] Handles = new GCHandle[S7Client.MaxVars];
		private int Count;
		private S7Client.S7DataItem[] Items = new S7Client.S7DataItem[S7Client.MaxVars];


		public int[] Results { get; } = new int[S7Client.MaxVars];

		private bool AdjustWordLength(int Area, ref int WordLen, ref int Amount, ref int Start)
		{
			// Calc Word size          
			int WordSize = WordLen.DataSizeByte();
			if (WordSize == 0)
				return false;

			if (Area == S7Consts.S7AreaCT)
				WordLen = S7Consts.S7WLCounter;
			if (Area == S7Consts.S7AreaTM)
				WordLen = S7Consts.S7WLTimer;

			if (WordLen == S7Consts.S7WLBit)
				Amount = 1;  // Only 1 bit can be transferred at time
			else
			{
				if ((WordLen != S7Consts.S7WLCounter) && (WordLen != S7Consts.S7WLTimer))
				{
					Amount = Amount * WordSize;
					Start = Start * 8;
					WordLen = S7Consts.S7WLByte;
				}
			}   
			return true;
		}

		public S7MultiVar(S7Client Client)
		{
			FClient = Client;
			for (int c = 0; c < S7Client.MaxVars; c++)
				Results[c] = S7Consts.errCliItemNotAvailable;
		}
		~S7MultiVar()
		{
			Clear();
		}

		public bool Add<T>(S7Consts.S7Tag Tag, ref T[] Buffer, int Offset)
		{
			return Add(Tag.Area, Tag.WordLen, Tag.DBNumber, Tag.Start, Tag.Elements, ref Buffer, Offset);
		}

		public bool Add<T>(S7Consts.S7Tag Tag, ref T[] Buffer)
		{
			return Add(Tag.Area, Tag.WordLen, Tag.DBNumber, Tag.Start, Tag.Elements, ref Buffer);
		}

		public bool Add<T>(Int32 Area, Int32 WordLen, Int32 DBNumber, Int32 Start, Int32 Amount, ref T[] Buffer)
		{
			return Add(Area, WordLen, DBNumber, Start, Amount, ref Buffer, 0);
		}

		public bool Add<T>(Int32 Area, Int32 WordLen, Int32 DBNumber, Int32 Start, Int32 Amount, ref T[] Buffer, int Offset)
		{
			if (Count < S7Client.MaxVars)
			{
				if (AdjustWordLength(Area, ref WordLen, ref Amount, ref Start))
				{
					Items[Count].Area = Area;
					Items[Count].WordLen = WordLen;
					Items[Count].Result = (int)S7Consts.errCliItemNotAvailable;
					Items[Count].DBNumber = DBNumber;
					Items[Count].Start = Start;
					Items[Count].Amount = Amount;
					GCHandle handle = GCHandle.Alloc(Buffer, GCHandleType.Pinned);

					if (IntPtr.Size == 4)
						Items[Count].pData = (IntPtr)(handle.AddrOfPinnedObject().ToInt32() + Offset * Marshal.SizeOf(typeof(T)));
					else
						Items[Count].pData = (IntPtr)(handle.AddrOfPinnedObject().ToInt64() + Offset * Marshal.SizeOf(typeof(T)));

					Handles[Count] = handle;
					Count++;
					return true;
				}

				return false;
			}

			return false;
		}

		public int Read()
		{
			int FunctionResult;
			int GlobalResult;
			try
			{
				if (Count > 0)
				{
					FunctionResult = FClient.ReadMultiVars(Items, Count);
					if (FunctionResult == 0)
						for (int c = 0; c < S7Client.MaxVars; c++)
							Results[c] = Items[c].Result;
					GlobalResult = FunctionResult;
				}
				else
					GlobalResult = S7Consts.errCliFunctionRefused;
			}
			finally
			{
				Clear(); // handles are no more needed and MUST be freed
			}
			return GlobalResult;
		}

		public int Write()
		{
			int FunctionResult;
			int GlobalResult;
			try
			{
				if (Count > 0)
				{
					FunctionResult = FClient.WriteMultiVars(Items, Count);
					if (FunctionResult == 0)
						for (int c = 0; c < S7Client.MaxVars; c++)
							Results[c] = Items[c].Result;
					GlobalResult = FunctionResult;
				}
				else
					GlobalResult = S7Consts.errCliFunctionRefused;
			}
			finally
			{
				Clear(); // handles are no more needed and MUST be freed
			}
			return GlobalResult;
		}

		public void Clear()
		{
			for (int c = 0; c < Count; c++)
			{
				if (Handles[c] != null)
					Handles[c].Free();
			}
			Count = 0;
		}
		#endregion
	}
}