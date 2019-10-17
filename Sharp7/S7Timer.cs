using System;
using System.Collections.Generic;
using System.Linq;

namespace Sharp7
{
    public class S7Timer
    {
        #region S7Timer
        private TimeSpan pt;
        private TimeSpan et;
        private bool input = false;
        private bool q = false;

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
                pt = new TimeSpan(0);
                et = new TimeSpan(0);
            }
            else
            {
                int resPT = buff[0]; resPT <<= 8;
                resPT += buff[1]; resPT <<= 8;
                resPT += buff[2]; resPT <<= 8;
                resPT += buff[3];
                pt = new TimeSpan(0, 0, 0, 0, resPT);

                int resET = buff[4]; resET <<= 8;
                resET += buff[5]; resET <<= 8;
                resET += buff[6]; resET <<= 8;
                resET += buff[7];
                et = new TimeSpan(0, 0, 0, 0, resET);

                input = (buff[8] & 0x01) == 0x01;
                q = (buff[8] & 0x02) == 0x02;
            }
        }

        public TimeSpan PT => pt;
        public TimeSpan ET => et;
        public bool IN => input;
        public bool Q => q;
        #endregion
    }
}