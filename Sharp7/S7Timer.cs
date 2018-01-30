using System;
using System.Collections.Generic;
using System.Linq;

namespace Sharp7
{
	public class S7Timer
	{
		#region S7Timer
		TimeSpan pt;
		TimeSpan et;
		bool input = false;
		bool q = false;
		public S7Timer(byte[] buff, int position)
		{
			if (position + 12 < buff.Length)
			{
				return;
			}
			else
			{
				SetTimer(new List<byte>(buff).GetRange(position, 16).ToArray());
			}
		}

		public S7Timer(byte[] buff)
		{
			SetTimer(buff);
		}

		private void SetTimer(byte[] buff)
		{
			if (buff.Length != 12)
			{
				this.pt = new TimeSpan(0);
				this.et = new TimeSpan(0);
			}
			else
			{
				Int32 resPT;
				resPT = buff[0]; resPT <<= 8;
				resPT += buff[1]; resPT <<= 8;
				resPT += buff[2]; resPT <<= 8;
				resPT += buff[3];
				this.pt = new TimeSpan(0, 0, 0, 0, resPT);

				Int32 resET;
				resET = buff[4]; resET <<= 8;
				resET += buff[5]; resET <<= 8;
				resET += buff[6]; resET <<= 8;
				resET += buff[7];
				this.et = new TimeSpan(0, 0, 0, 0, resET);

				this.input = (buff[8] & 0x01) == 0x01;
				this.q = (buff[8] & 0x02) == 0x02;
			}
		}
		public TimeSpan PT
		{
			get
			{
				return pt;
			}
		}
		public TimeSpan ET
		{
			get
			{
				return et;
			}
		}
		public bool IN
		{
			get
			{
				return input;
			}
		}
		public bool Q
		{
			get
			{
				return q;
			}
		}
		#endregion
	}
}