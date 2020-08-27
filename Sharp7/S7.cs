using System;
using System.Collections.Generic;
using System.Text;

namespace Sharp7
{
    public static class S7
    {
        #region [Help Functions]

        private static Int64
            bias = 621355968000000000; // "decimicros" between 0001-01-01 00:00:00 and 1970-01-01 00:00:00

        private static int BCDtoByte(byte B)
        {
            return ((B >> 4) * 10) + (B & 0x0F);
        }

        private static byte ByteToBCD(int value)
        {
            return (byte) (((value / 10) << 4) | (value % 10));
        }

        public static int DataSizeByte(this int wordLength)
        {
            switch (wordLength)
            {
                case (int)S7WordLength.Bit: return 1; // S7 sends 1 byte per bit
                case (int)S7WordLength.Byte: return 1;
                case (int)S7WordLength.Char: return 1;
                case (int)S7WordLength.Word: return 2;
                case (int)S7WordLength.DWord: return 4;
                case (int)S7WordLength.Int: return 2;
                case (int)S7WordLength.DInt: return 4;
                case (int)S7WordLength.Real: return 4;
                case (int)S7WordLength.Counter: return 2;
                case (int)S7WordLength.Timer: return 2;
                default: return 0;
            }
        }

        #region Get/Set the bit at Pos.Bit

        public static bool GetBitAt(this byte[] buffer, int pos, int bit)
        {
            byte[] Mask = {0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80};
            if (bit < 0) bit = 0;
            if (bit > 7) bit = 7;
            return (buffer[pos] & Mask[bit]) != 0;
        }

        [Obsolete("Use SetBitAt as extension method")]
        public static void SetBitAt(ref byte[] buffer, int pos, int bit, bool value)
        {
            buffer.SetBitAt(pos, bit, value);
        }
        
        public static void SetBitAt(this byte[] buffer, int pos, int bit, bool value)
        {
            byte[] Mask = {0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80};
            if (bit < 0)
            {
                bit = 0;
            }

            if (bit > 7)
            {
                bit = 7;
            }

            if (value)
            {
                buffer[pos] = (byte) (buffer[pos] | Mask[bit]);
            }
            else
            {
                buffer[pos] = (byte) (buffer[pos] & ~Mask[bit]);
            }
            
        }

        #endregion

        #region Get/Set 8 bit signed value (S7 SInt) -128..127

        public static int GetSIntAt(this byte[] buffer, int pos)
        {
            int value = buffer[pos];
            if (value < 128)
                return value;
                
            return (value - 256);
        }

        public static void SetSIntAt(this byte[] buffer, int pos, int value)
        {
            if (value < -128) value = -128;
            if (value > 127) value = 127;
            buffer[pos] = (byte) value;
        }

        #endregion

        #region Get/Set 16 bit signed value (S7 int) -32768..32767

        public static int GetIntAt(this byte[] buffer, int pos)
        {
            return (buffer[pos] << 8) | buffer[pos + 1];
        }

        public static void SetIntAt(this byte[] buffer, int pos, Int16 value)
        {
            buffer[pos] = (byte) (value >> 8);
            buffer[pos + 1] = (byte) (value & 0x00FF);
        }

        #endregion

        #region Get/Set 32 bit signed value (S7 DInt) -2147483648..2147483647

        public static int GetDIntAt(this byte[] buffer, int pos)
        {
            int result;
            result = buffer[pos];
            result <<= 8;
            result += buffer[pos + 1];
            result <<= 8;
            result += buffer[pos + 2];
            result <<= 8;
            result += buffer[pos + 3];
            return result;
        }

        public static void SetDIntAt(this byte[] buffer, int pos, int value)
        {
            buffer[pos + 3] = (byte) (value & 0xFF);
            buffer[pos + 2] = (byte) ((value >> 8) & 0xFF);
            buffer[pos + 1] = (byte) ((value >> 16) & 0xFF);
            buffer[pos] = (byte) ((value >> 24) & 0xFF);
        }

        #endregion

        #region Get/Set 64 bit signed value (S7 LInt) -9223372036854775808..9223372036854775807

        public static Int64 GetLIntAt(this byte[] buffer, int pos)
        {
            Int64 result;
            result = buffer[pos];
            result <<= 8;
            result += buffer[pos + 1];
            result <<= 8;
            result += buffer[pos + 2];
            result <<= 8;
            result += buffer[pos + 3];
            result <<= 8;
            result += buffer[pos + 4];
            result <<= 8;
            result += buffer[pos + 5];
            result <<= 8;
            result += buffer[pos + 6];
            result <<= 8;
            result += buffer[pos + 7];
            return result;
        }

        public static void SetLIntAt(this byte[] buffer, int pos, Int64 value)
        {
            buffer[pos + 7] = (byte) (value & 0xFF);
            buffer[pos + 6] = (byte) ((value >> 8) & 0xFF);
            buffer[pos + 5] = (byte) ((value >> 16) & 0xFF);
            buffer[pos + 4] = (byte) ((value >> 24) & 0xFF);
            buffer[pos + 3] = (byte) ((value >> 32) & 0xFF);
            buffer[pos + 2] = (byte) ((value >> 40) & 0xFF);
            buffer[pos + 1] = (byte) ((value >> 48) & 0xFF);
            buffer[pos] = (byte) ((value >> 56) & 0xFF);
        }

        #endregion

        #region Get/Set 8 bit unsigned value (S7 USInt) 0..255

        public static byte GetUSIntAt(this byte[] buffer, int pos)
        {
            return buffer[pos];
        }

        public static void SetUSIntAt(this byte[] buffer, int pos, byte value)
        {
            buffer[pos] = value;
        }

        #endregion

        #region Get/Set 16 bit unsigned value (S7 UInt) 0..65535

        public static UInt16 GetUIntAt(this byte[] buffer, int pos)
        {
            return (UInt16) ((buffer[pos] << 8) | buffer[pos + 1]);
        }

        public static void SetUIntAt(this byte[] buffer, int pos, UInt16 value)
        {
            buffer[pos] = (byte) (value >> 8);
            buffer[pos + 1] = (byte) (value & 0x00FF);
        }

        #endregion

        #region Get/Set 32 bit unsigned value (S7 UDInt) 0..4294967296

        public static UInt32 GetUDIntAt(this byte[] buffer, int pos)
        {
            UInt32 result;
            result = buffer[pos];
            result <<= 8;
            result |= buffer[pos + 1];
            result <<= 8;
            result |= buffer[pos + 2];
            result <<= 8;
            result |= buffer[pos + 3];
            return result;
        }

        public static void SetUDIntAt(this byte[] buffer, int pos, UInt32 value)
        {
            buffer[pos + 3] = (byte) (value & 0xFF);
            buffer[pos + 2] = (byte) ((value >> 8) & 0xFF);
            buffer[pos + 1] = (byte) ((value >> 16) & 0xFF);
            buffer[pos] = (byte) ((value >> 24) & 0xFF);
        }

        #endregion

        #region Get/Set 64 bit unsigned value (S7 ULint) 0..18446744073709551616

        public static UInt64 GetULIntAt(this byte[] buffer, int pos)
        {
            UInt64 result;
            result = buffer[pos];
            result <<= 8;
            result |= buffer[pos + 1];
            result <<= 8;
            result |= buffer[pos + 2];
            result <<= 8;
            result |= buffer[pos + 3];
            result <<= 8;
            result |= buffer[pos + 4];
            result <<= 8;
            result |= buffer[pos + 5];
            result <<= 8;
            result |= buffer[pos + 6];
            result <<= 8;
            result |= buffer[pos + 7];
            return result;
        }

        public static void SetULintAt(this byte[] buffer, int pos, UInt64 value)
        {
            buffer[pos + 7] = (byte) (value & 0xFF);
            buffer[pos + 6] = (byte) ((value >> 8) & 0xFF);
            buffer[pos + 5] = (byte) ((value >> 16) & 0xFF);
            buffer[pos + 4] = (byte) ((value >> 24) & 0xFF);
            buffer[pos + 3] = (byte) ((value >> 32) & 0xFF);
            buffer[pos + 2] = (byte) ((value >> 40) & 0xFF);
            buffer[pos + 1] = (byte) ((value >> 48) & 0xFF);
            buffer[pos] = (byte) ((value >> 56) & 0xFF);
        }

        #endregion

        #region Get/Set 8 bit word (S7 Byte) 16#00..16#FF

        public static byte GetByteAt(this byte[] buffer, int pos)
        {
            return buffer[pos];
        }

        public static void SetByteAt(this byte[] buffer, int pos, byte value)
        {
            buffer[pos] = value;
        }

        #endregion

        #region Get/Set 16 bit word (S7 Word) 16#0000..16#FFFF

        public static UInt16 GetWordAt(this byte[] buffer, int pos)
        {
            return GetUIntAt(buffer, pos);
        }

        public static void SetWordAt(this byte[] buffer, int pos, UInt16 value)
        {
            SetUIntAt(buffer, pos, value);
        }

        #endregion

        #region Get/Set 32 bit word (S7 DWord) 16#00000000..16#FFFFFFFF

        public static UInt32 GetDWordAt(this byte[] buffer, int pos)
        {
            return GetUDIntAt(buffer, pos);
        }

        public static void SetDWordAt(this byte[] buffer, int pos, UInt32 value)
        {
            SetUDIntAt(buffer, pos, value);
        }

        #endregion

        #region Get/Set 64 bit word (S7 LWord) 16#0000000000000000..16#FFFFFFFFFFFFFFFF

        public static UInt64 GetLWordAt(this byte[] buffer, int pos)
        {
            return GetULIntAt(buffer, pos);
        }

        public static void SetLWordAt(this byte[] buffer, int pos, UInt64 value)
        {
            SetULintAt(buffer, pos, value);
        }

        #endregion

        #region Get/Set 32 bit floating point number (S7 Real) (Range of Single)

        public static Single GetRealAt(this byte[] buffer, int pos)
        {
            UInt32 value = GetUDIntAt(buffer, pos);
            byte[] bytes = BitConverter.GetBytes(value);
            return BitConverter.ToSingle(bytes, 0);
        }

        public static void SetRealAt(this byte[] buffer, int pos, Single value)
        {
            byte[] FloatArray = BitConverter.GetBytes(value);
            buffer[pos] = FloatArray[3];
            buffer[pos + 1] = FloatArray[2];
            buffer[pos + 2] = FloatArray[1];
            buffer[pos + 3] = FloatArray[0];
        }

        #endregion

        #region Get/Set 64 bit floating point number (S7 LReal) (Range of Double)

        public static Double GetLRealAt(this byte[] buffer, int pos)
        {
            UInt64 value = GetULIntAt(buffer, pos);
            byte[] bytes = BitConverter.GetBytes(value);
            return BitConverter.ToDouble(bytes, 0);
        }

        public static void SetLRealAt(this byte[] buffer, int pos, Double value)
        {
            byte[] FloatArray = BitConverter.GetBytes(value);
            buffer[pos] = FloatArray[7];
            buffer[pos + 1] = FloatArray[6];
            buffer[pos + 2] = FloatArray[5];
            buffer[pos + 3] = FloatArray[4];
            buffer[pos + 4] = FloatArray[3];
            buffer[pos + 5] = FloatArray[2];
            buffer[pos + 6] = FloatArray[1];
            buffer[pos + 7] = FloatArray[0];
        }

        #endregion

        #region Get/Set DateTime (S7 DATE_AND_TIME)

        public static DateTime GetDateTimeAt(this byte[] buffer, int pos)
        {
            int Year, Month, Day, Hour, Min, Sec, MSec;

            Year = BCDtoByte(buffer[pos]);
            if (Year < 90)
                Year += 2000;
            else
                Year += 1900;

            Month = BCDtoByte(buffer[pos + 1]);
            Day = BCDtoByte(buffer[pos + 2]);
            Hour = BCDtoByte(buffer[pos + 3]);
            Min = BCDtoByte(buffer[pos + 4]);
            Sec = BCDtoByte(buffer[pos + 5]);
            MSec = (BCDtoByte(buffer[pos + 6]) * 10) + (BCDtoByte(buffer[pos + 7]) / 10);
            try
            {
                return new DateTime(Year, Month, Day, Hour, Min, Sec, MSec);
            }
            catch (System.ArgumentOutOfRangeException)
            {
                return new DateTime(0);
            }
        }

        public static void SetDateTimeAt(this byte[] buffer, int pos, DateTime value)
        {
            int Year = value.Year;
            int Month = value.Month;
            int Day = value.Day;
            int Hour = value.Hour;
            int Min = value.Minute;
            int Sec = value.Second;
            int Dow = (int) value.DayOfWeek + 1;
            // MSecH = First two digits of miliseconds 
            int MsecH = value.Millisecond / 10;
            // MSecL = Last digit of miliseconds
            int MsecL = value.Millisecond % 10;
            if (Year > 1999)
                Year -= 2000;

            buffer[pos] = ByteToBCD(Year);
            buffer[pos + 1] = ByteToBCD(Month);
            buffer[pos + 2] = ByteToBCD(Day);
            buffer[pos + 3] = ByteToBCD(Hour);
            buffer[pos + 4] = ByteToBCD(Min);
            buffer[pos + 5] = ByteToBCD(Sec);
            buffer[pos + 6] = ByteToBCD(MsecH);
            buffer[pos + 7] = ByteToBCD(MsecL * 10 + Dow);
        }

        #endregion

        #region Get/Set DATE (S7 DATE)

        public static DateTime GetDateAt(this byte[] buffer, int pos)
        {
            try
            {
                return new DateTime(1990, 1, 1).AddDays(GetIntAt(buffer, pos));
            }
            catch (System.ArgumentOutOfRangeException)
            {
                return new DateTime(0);
            }
        }

        public static void SetDateAt(this byte[] buffer, int pos, DateTime value)
        {
            SetIntAt(buffer, pos, (Int16) (value - new DateTime(1990, 1, 1)).Days);
        }

        #endregion

        #region Get/Set TOD (S7 TIME_OF_DAY)

        [Obsolete("Use GetTODAsDateTimeAt or GetTODAsTimeSpanAt instead")]
        public static DateTime GetTODAt(this byte[] buffer, int pos)
        {
            return buffer.GetTODAsDateTimeAt(pos);
        }
        
        public static DateTime GetTODAsDateTimeAt(this byte[] buffer, int pos)
        {
            try
            {
                return new DateTime(0).AddMilliseconds(buffer.GetDIntAt(pos));
            }
            catch (ArgumentOutOfRangeException)
            {
                return new DateTime(0);
            }
        }

        public static void SetTODAt(this byte[] buffer, int pos, DateTime value)
        {
            TimeSpan Time = value.TimeOfDay;
            SetDIntAt(buffer, pos, (Int32) Math.Round(Time.TotalMilliseconds));
        }
        
        public static TimeSpan GetTODAsTimeSpanAt(this byte[] buffer, int pos)
        {
            try
            {
                return TimeSpan.FromMilliseconds(buffer.GetDIntAt(pos));
            }
            catch (ArgumentOutOfRangeException)
            {
                return TimeSpan.Zero;
            }
        }

        public static void SetTODAt(this byte[] buffer, int pos, TimeSpan value)
        {
            SetDIntAt(buffer, pos, (Int32) Math.Round(value.TotalMilliseconds));
        }

        #endregion

        #region Get/Set LTOD (S7 1500 LONG TIME_OF_DAY)

        [Obsolete("Use GetLTODAsDateTimeAt or GetLTODAsTimeSpanAt instead")]
        public static DateTime GetLTODAt(this byte[] buffer, int pos)
        {
            return buffer.GetLTODAsDateTimeAt(pos);
        }
        
        public static DateTime GetLTODAsDateTimeAt(this byte[] buffer, int pos)
        {
            // .NET Tick = 100 ns, S71500 Tick = 1 ns
            try
            {
                return new DateTime(Math.Abs(GetLIntAt(buffer, pos) / 100));
            }
            catch (System.ArgumentOutOfRangeException)
            {
                return new DateTime(0);
            }
        }

        public static void SetLTODAt(this byte[] buffer, int pos, DateTime value)
        {
            TimeSpan Time = value.TimeOfDay;
            SetLIntAt(buffer, pos, (Int64) Time.Ticks * 100);
        }

        public static TimeSpan GetLTODAsTimeSpanAt(this byte[] buffer, int pos)
        {
            try
            {
                return TimeSpan.FromTicks(Math.Abs(GetLIntAt(buffer, pos) / 100));
            }
            catch (System.ArgumentOutOfRangeException)
            {
                return TimeSpan.Zero;
            }
        }

        public static void SetLTODAt(this byte[] buffer, int pos, TimeSpan value)
        {
            SetLIntAt(buffer, pos, (Int64) value.Ticks * 100);
        }
        
        #endregion

        #region GET/SET LDT (S7 1500 Long Date and Time)

        public static DateTime GetLDTAt(this byte[] buffer, int pos)
        {
            try
            {
                return new DateTime((GetLIntAt(buffer, pos) / 100) + bias);
            }
            catch (System.ArgumentOutOfRangeException)
            {
                return new DateTime(0);
            }
        }

        public static void SetLDTAt(this byte[] buffer, int pos, DateTime value)
        {
            SetLIntAt(buffer, pos, (value.Ticks - bias) * 100);
        }

        #endregion

        #region Get/Set DTL (S71200/1500 Date and Time)

        // Thanks to Johan Cardoen for GetDTLAt
        public static DateTime GetDTLAt(this byte[] buffer, int pos)
        {
            int Year, Month, Day, Hour, Min, Sec, MSec;

            Year = buffer[pos] * 256 + buffer[pos + 1];
            Month = buffer[pos + 2];
            Day = buffer[pos + 3];
            Hour = buffer[pos + 5];
            Min = buffer[pos + 6];
            Sec = buffer[pos + 7];
            MSec = (int) GetUDIntAt(buffer, pos + 8) / 1000000;

            try
            {
                return new DateTime(Year, Month, Day, Hour, Min, Sec, MSec);
            }
            catch (System.ArgumentOutOfRangeException)
            {
                return new DateTime(0);
            }
        }

        public static void SetDTLAt(this byte[] buffer, int pos, DateTime value)
        {
            short Year = (short) value.Year;
            byte Month = (byte) value.Month;
            byte Day = (byte) value.Day;
            byte Hour = (byte) value.Hour;
            byte Min = (byte) value.Minute;
            byte Sec = (byte) value.Second;
            byte Dow = (byte) (value.DayOfWeek + 1);

            Int32 NanoSecs = value.Millisecond * 1000000;

            var bytes_short = BitConverter.GetBytes(Year);

            buffer[pos] = bytes_short[1];
            buffer[pos + 1] = bytes_short[0];
            buffer[pos + 2] = Month;
            buffer[pos + 3] = Day;
            buffer[pos + 4] = Dow;
            buffer[pos + 5] = Hour;
            buffer[pos + 6] = Min;
            buffer[pos + 7] = Sec;
            SetDIntAt(buffer, pos + 8, NanoSecs);
        }

        #endregion

        #region Get/Set String (S7 String)

        // Thanks to Pablo Agirre 
        public static string GetStringAt(this byte[] buffer, int pos)
        {
            int size = (int) buffer[pos + 1];
            return Encoding.UTF8.GetString(buffer, pos + 2, size);
        }

        public static void SetStringAt(this byte[] buffer, int pos, int MaxLen, string value)
        {
            int size = value.Length;
            buffer[pos] = (byte) MaxLen;
            buffer[pos + 1] = (byte) size;
            Encoding.UTF8.GetBytes(value, 0, size, buffer, pos + 2);
        }

        #endregion

        #region Get/Set Array of char (S7 ARRAY OF CHARS)

        public static string GetCharsAt(this byte[] buffer, int pos, int Size)
        {
            return Encoding.UTF8.GetString(buffer, pos, Size);
        }

        public static void SetCharsAt(this byte[] buffer, int pos, string value)
        {
            int MaxLen = buffer.Length - pos;
            // Truncs the string if there's no room enough        
            if (MaxLen > value.Length) MaxLen = value.Length;
            Encoding.UTF8.GetBytes(value, 0, MaxLen, buffer, pos);
        }

        #endregion

        #region Get/Set Counter

        public static int GetCounter(this ushort value)
        {
            return BCDtoByte((byte) value) * 100 + BCDtoByte((byte) (value >> 8));
        }

        public static int GetCounterAt(this ushort[] buffer, int Index)
        {
            return GetCounter(buffer[Index]);
        }

        public static ushort ToCounter(this int value)
        {
            return (ushort) (ByteToBCD(value / 100) + (ByteToBCD(value % 100) << 8));
        }

        public static void SetCounterAt(this ushort[] buffer, int pos, int value)
        {
            buffer[pos] = ToCounter(value);
        }

        #endregion

        #region Get/Set Timer

        public static S7Timer GetS7TimerAt(this byte[] buffer, int pos)
        {
            return new S7Timer(new List<byte>(buffer).GetRange(pos, 12).ToArray());
        }

        public static void SetS7TimespanAt(this byte[] buffer, int pos, TimeSpan value)
        {
            SetDIntAt(buffer, pos, (Int32) value.TotalMilliseconds);
        }

        public static TimeSpan GetS7TimespanAt(this byte[] buffer, int pos)
        {
            if (buffer.Length < pos + 4)
            {
                return new TimeSpan();
            }

            Int32 a;
            a = buffer[pos + 0];
            a <<= 8;
            a += buffer[pos + 1];
            a <<= 8;
            a += buffer[pos + 2];
            a <<= 8;
            a += buffer[pos + 3];
            TimeSpan sp = new TimeSpan(0, 0, 0, 0, a);

            return sp;
        }

        #endregion

        #endregion [Help Functions]
    }
}