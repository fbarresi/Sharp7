using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shouldly;
using Xunit;

namespace Sharp7.Tests
{
    public class TestUtilities
    {
        [Theory]
        [InlineData(1,1)] 
        [InlineData(2,1)] 
        [InlineData(3,1)] 
        [InlineData(4,2)] 
        [InlineData(5,2)] 
        [InlineData(6,4)] 
        [InlineData(7,4)] 
        [InlineData(8,4)] 
        [InlineData(0x1D,2)] 
        [InlineData(0x1C,2)] 
        [InlineData(0,0)] 
        public void TestDataSizeByte(int wordLength, int expected) { S7.DataSizeByte(wordLength).ShouldBe(expected); }
        [Fact] public void TestGetBitAt() { S7.GetBitAt(new byte[] {1,2,3,4}, 0, 0).ShouldBe(true); }
        [Fact] public void TestSetBitAt() {
            var buffer = new byte[] {1,2,3,4};
            S7.SetBitAt(ref buffer, 0, 1, true);
            buffer.ShouldBe(new byte[] {3, 2, 3, 4});
        }
        [Fact] public void TestSetBitAtAsExtensionMethod() {
            var buffer = new byte[] {1,2,3,4};
            buffer.SetBitAt(0, 1, true);
            buffer.ShouldBe(new byte[] {3, 2, 3, 4});
        }

        //unsigned

        [Theory]
        [InlineData(new byte[] { 1, 2, 3, 4 }, 0, 1)]
        [InlineData(new byte[] { 129, 2, 3, 4 }, 0, 129)]
        public void TestGetUSIntAt(byte[] buffer, int pos, byte expected) { S7.GetUSIntAt(buffer, pos).ShouldBe(expected); }
        [Theory]
        [InlineData(new byte[] { 0, 2, 3, 4 }, 0, 1, new byte[] { 1, 2, 3, 4 })]
        [InlineData(new byte[] { 0, 2, 3, 4 }, 0, 127, new byte[] { 127, 2, 3, 4 })] 
        public void TestSetUSIntAt(byte[] buffer, int pos, byte value, byte[] expected)
        {
            S7.SetUSIntAt(buffer, pos, value);
            buffer.ShouldBe(expected);
        }

        [Theory]
        [InlineData( new byte[] { 1, 1, 0, 0 }, 0, 257)]
        public void TestGetUIntAt(byte[] buffer, int pos, ushort expected) { S7.GetUIntAt(buffer, pos).ShouldBe(expected); }

        [Theory]
        [InlineData(new byte[]{0,0,0,0}, 0, 1, new byte[] {0,1,0,0})]
        public void TestSetUIntAt(byte[] buffer, int pos, ushort value, byte[] expected)
        {
            S7.SetUIntAt(buffer, pos, value);
            buffer.ShouldBe(expected);
        }
        [Theory]
        [InlineData(new byte[] { 0, 0, 0, 2 }, 0, 2)] 
        public void TestGetUDIntAt(byte[] buffer, int pos, uint expected) { S7.GetUDIntAt(buffer, pos).ShouldBe(expected); }

        [Theory]
        [InlineData(new byte[] {0, 0, 0, 0}, 0, 1, new byte[] {0, 0, 0, 1})]
        public void TestSetUDIntAt(byte[] buffer, int pos, uint value, byte[] expected)
        {
            S7.SetUDIntAt(buffer, pos, value);
            buffer.ShouldBe(expected);
        }
        [Theory]
        [InlineData(new byte[] { 0, 0, 0, 0, 0, 0, 0, 2 }, 0, 2L)] 
        public void TestGetULIntAt(byte[] buffer, int pos, ulong expected) { S7.GetULIntAt(buffer, pos).ShouldBe(expected); }

        [Theory]
        [InlineData(new byte[] {0, 0, 0, 0, 0, 0, 0, 0}, 0, 1L, new byte[] { 0, 0, 0, 0, 0, 0, 0, 1})]
        public void TestSetULIntAt(byte[] buffer, int pos, ulong value, byte[] expected)
        {
            S7.SetULintAt(buffer, pos, value);
            buffer.ShouldBe(expected);
        }

        // signed

        [Theory]
        [InlineData(new byte[] { 1, 2, 3, 4 }, 0, 1)]
        [InlineData(new byte[] { 129, 2, 3, 4 }, 0, -127)]
        public void TestGetSIntAt(byte[] buffer, int pos, int expected) { S7.GetSIntAt(buffer, pos).ShouldBe(expected); }
        [Theory]
        [InlineData(new byte[] { 0, 2, 3, 4 }, 0, 1, new byte[] { 1, 2, 3, 4 })]
        [InlineData(new byte[] { 0, 2, 3, 4 }, 0, -127, new byte[] { 129, 2, 3, 4 })]
        public void TestSetSIntAt(byte[] buffer, int pos, int value, byte[] expected)
        {
            S7.SetSIntAt(buffer, pos, value);
            buffer.ShouldBe(expected);
        }

        [Theory]
        [InlineData(new byte[] { 1, 1, 0, 0 }, 0, 257)]
        public void TestGetIntAt(byte[] buffer, int pos, short expected) { S7.GetIntAt(buffer, pos).ShouldBe(expected); }

        [Theory]
        [InlineData(new byte[] { 0, 0, 0, 0 }, 0, 1, new byte[] { 0, 1, 0, 0 })]
        public void TestSetIntAt(byte[] buffer, int pos, Int16 value, byte[] expected)
        {
            S7.SetIntAt(buffer, pos, value);
            buffer.ShouldBe(expected);
        }
        [Theory]
        [InlineData(new byte[] { 0, 0, 0, 2 }, 0, 2)]
        public void TestGetDIntAt(byte[] buffer, int pos, int expected) { S7.GetDIntAt(buffer, pos).ShouldBe(expected); }

        [Theory]
        [InlineData(new byte[] { 0, 0, 0, 0 }, 0, 1, new byte[] { 0, 0, 0, 1 })]
        public void TestSetDIntAt(byte[] buffer, int pos, int value, byte[] expected)
        {
            S7.SetDIntAt(buffer, pos, value);
            buffer.ShouldBe(expected);
        }
        [Theory]
        [InlineData(new byte[] { 0, 0, 0, 0, 0, 0, 0, 2 }, 0, 2L)] 
        public void TestGetLIntAt(byte[] buffer, int pos, long expected) { S7.GetLIntAt(buffer, pos).ShouldBe(expected); }

        [Theory]
        [InlineData(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 0, 1L, new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 })]
        public void TestSetLIntAt(byte[] buffer, int pos, long value, byte[] expected)
        {
            S7.SetLIntAt(buffer, pos, value);
            buffer.ShouldBe(expected);
        }


        [Fact] public void TestGetByteAt() { S7.GetByteAt(new byte[] {1,2,3,4}, 1).ShouldBe((byte)2); }

        [Fact]
        public void TestSetByteAt()
        {
            var buffer = new byte[] { 1, 2, 3, 4 };
            S7.SetByteAt(buffer, 0, (byte)5);
            buffer.ShouldBe(new byte[] { 5, 2, 3, 4 });
        }

        [Theory]
        [InlineData(new byte[] {0, 2, 0, 0, 0, 0, 0, 0}, 0, 2)]
        public void TestGetWordAt(byte[] buffer, int pos, ushort expected)
        {
            S7.GetWordAt(buffer, pos).ShouldBe(expected);
        }

        [Theory]
        [InlineData(new byte[] {0, 0, 0, 0, 0, 0, 0, 0}, 0, 1, new byte[] {0, 1, 0, 0, 0, 0, 0, 0})]
        public void TestSetWordAt(byte[] buffer, int pos, ushort value, byte[] expected)
        {
            S7.SetWordAt(buffer, pos, value);
            buffer.ShouldBe(expected);
        }

        [Theory]
        [InlineData(new byte[] { 0, 0, 0, 3, 0, 0, 0, 0 }, 0, 3)]
        public void TestGetDWordAt(byte[] buffer, int pos, uint expected)
        {
            S7.GetDWordAt(buffer, pos).ShouldBe(expected);
        }

        [Theory]
        [InlineData(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 0, 1, new byte[] { 0, 0, 0, 1, 0, 0, 0, 0 })]
        public void TestSetDWordAt(byte[] buffer, int pos, uint value, byte[] expected)
        {
            S7.SetDWordAt(buffer, pos, value);
            buffer.ShouldBe(expected);
        }

        [Theory]
        [InlineData(new byte[] { 0, 0, 0, 0, 0, 0, 0, 4 }, 0, 4L)]
        public void TestGetLWordAt(byte[] buffer, int pos, ulong expected)
        {
            S7.GetLWordAt(buffer, pos).ShouldBe(expected);
        }

        [Theory]
        [InlineData(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 0, 1L, new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 })]
        public void TestSetLWordAt(byte[] buffer, int pos, ulong value, byte[] expected)
        {
            S7.SetLWordAt(buffer, pos, value);
            buffer.ShouldBe(expected);
        }

        [Theory]
        [InlineData(new byte[] { 1, 2, 3, 0, 0, 11, 5, 4 }, 0, 2.387938E-38f)]
        public void TestGetRealAt(byte[] buffer, int pos, float expected)
        {
            S7.GetRealAt(buffer, pos).ShouldBe(expected);
        }

        [Theory]
        [InlineData(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 0, 4f, new byte[] { 64, 128, 0, 0, 0, 0, 0, 0 })]
        public void TestSetRealAt(byte[] buffer, int pos, float value, byte[] expected)
        {
            S7.SetRealAt(buffer, pos, value);
            buffer.ShouldBe(expected);
        }

        [Theory]
        [InlineData(new byte[] { 64, 128, 0, 0, 0, 0, 0, 0}, 0, 512d)]
        public void TestGetLRealAt(byte[] buffer, int pos, double expected)
        {
            S7.GetLRealAt(buffer, pos).ShouldBe(expected);
        }

        [Theory]
        [InlineData(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 0, 4d, new byte[] { 64, 16, 0, 0, 0, 0, 0, 0 })]
        public void TestSetLRealAt(byte[] buffer, int pos, double value, byte[] expected)
        {
            S7.SetLRealAt(buffer, pos, value);
            buffer.ShouldBe(expected);
        }


        [Theory]
        [InlineData(new byte[] {16,17,18,19,20,21,0,6})]
        public void TestGetDateTimeAt(byte[] buffer)
        {
            var time = new DateTime(2010, 11, 12, 13, 14, 15);
            S7.GetDateTimeAt(buffer, 0).ShouldBe(time);
        }

        [Theory]
        [InlineData(new byte[] {16, 17, 18, 19, 20, 21, 0, 6})]
        public void TestSetDateTimeAt(byte[] expected)
        {
            var time = new DateTime(2010, 11, 12, 13, 14, 15);
            var buffer = new byte[8];
            S7.SetDateTimeAt(buffer, 0, time);
            buffer.ShouldBe(expected);
        }

        [Theory]
        [InlineData(new byte[] {0,2})]
        public void TestGetDateAt(byte[] buffer)
        {
            var date = new DateTime(1990, 1, 3);
            S7.GetDateAt(buffer, 0).ShouldBe(date);
        }
        [Theory]
        [InlineData(new byte[] { 0,3 })]
        public void TestSetDateAt(byte[] expected)
        {
            var buffer = new byte[2];
            var date = new DateTime(1990,1,4);
            S7.SetDateAt(buffer, 0, date);
            buffer.ShouldBe(expected);
        }

        [Theory]
        [InlineData(new byte[] {0, 0,0,2}, 2)]
        public void TestGetTODAt(byte[] buffer, int milliseconds)
        {
            S7.GetTODAt(buffer, 0).ShouldBe(new DateTime(0).AddMilliseconds(milliseconds));
            S7.GetTODAsDateTimeAt(buffer, 0).ShouldBe(new DateTime(0).AddMilliseconds(milliseconds));
            S7.GetTODAsTimeSpanAt(buffer, 0).ShouldBe(TimeSpan.FromMilliseconds(milliseconds));
        }

        [Theory]
        [InlineData(new byte[] {0, 0,0,2}, 2)]
        public void TestSetTODAt(byte[] expected, int milliseconds)
        {
            var buffer = new byte[4];
            S7.SetTODAt(buffer, 0, new DateTime(0).AddMilliseconds(milliseconds));
            buffer.ShouldBe(expected);
            buffer = new byte[4];
            S7.SetTODAt(buffer, 0, TimeSpan.FromMilliseconds(milliseconds));
            buffer.ShouldBe(expected);
        }
        [Theory]
        [InlineData(new byte[] { 0, 0, 0, 0,0,0,0,200 }, 2)]
        public void TestGetLTODAt(byte[] buffer, int ticks)
        {
            S7.GetLTODAt(buffer, 0).ShouldBe(new DateTime(ticks));
            S7.GetLTODAsDateTimeAt(buffer, 0).ShouldBe(new DateTime(ticks));
            S7.GetLTODAsTimeSpanAt(buffer, 0).ShouldBe(TimeSpan.FromTicks(ticks));
        }

        [Theory]
        [InlineData(new byte[] { 0, 0, 0, 0,0,0,0,200 }, 2)]
        public void TestSetLTODAt(byte[] expected, int ticks)
        {
            var buffer = new byte[8];
            S7.SetLTODAt(buffer, 0, new DateTime(ticks));
            buffer.ShouldBe(expected);
            buffer = new byte[8];
            S7.SetLTODAt(buffer, 0, TimeSpan.FromTicks(ticks));
            buffer.ShouldBe(expected);
        }

        [Theory]
        [InlineData(new byte[] { 0, 0, 0, 0, 0, 0, 0, 200 }, 2)]
        public void TestGetLDTAt(byte[] buffer, int ticks)
        {
            S7.GetLDTAt(buffer, 0).ShouldBe(new DateTime(1970, 1, 1).AddTicks(ticks));
        }

        [Theory]
        [InlineData(new byte[] { 0, 0, 0, 0, 0, 0, 0, 200 }, 2)]
        public void TestSetLDTAt(byte[] expected, int ticks)
        {
            var buffer = new byte[8];
            S7.SetLDTAt(buffer, 0, new DateTime(1970,1,1).AddTicks(ticks));
            buffer.ShouldBe(expected);
        }

        [Theory]
        [InlineData(new byte[] { 10, 10, 10, 10, 10, 10, 10, 0, 0, 0, 0, 0, 0, 0, 0, 0 })]
        public void TestGetDTLAt(byte[] buffer)
        {
            S7.GetDTLAt(buffer, 0).ShouldBe(new DateTime(2570, 10, 10,10,10,0));
        }

        [Theory]
        [InlineData(new byte[] { 10, 10, 10, 10, 4, 10, 10, 0, 0, 0, 0, 0, 0, 0, 0, 0 })]
        public void TestSetDTLAt(byte[] expected)
        {
            var buffer = new byte[16];

            S7.SetDTLAt(buffer, 0, new DateTime(2570, 10, 10, 10, 10, 0));
            buffer.ShouldBe(expected);
        }


        [Theory]
        [InlineData(new byte[] {0, 3, 55,55,55,0,0,0,0,0}, "777")]
        public void TestGetStringAt(byte[] buffer, string expected)
        {
            S7.GetStringAt(buffer, 0).ShouldBe(expected);
        }

        [Theory]
        [InlineData("888", new byte[] {200, 3, 56,56,56})]
        public void TestSetStringAt(string test, byte[] expected)
        {
            var buffer = new byte[200];
            S7.SetStringAt(buffer, 0, buffer.Length, test);
            buffer.Take(expected.Length).ToArray().ShouldBe(expected);
        }
        
        [Theory]
        [InlineData(new byte[] { 55, 55, 55 }, "777")]
        [InlineData(new byte[] { 56, 56, 56 }, "888")]
        public void TestGetCharsAt(byte[] buffer, string expected) { S7.GetCharsAt(buffer, 0, buffer.Length).ShouldBe(expected); }

        [Theory]
        [InlineData("777", new byte[] {55, 55, 55})]
        [InlineData("888", new byte[] {56, 56, 56})]
        public void TestSetCharsAt(string chars, byte[] expected)
        {
            var buffer = new byte[chars.Length];
            S7.SetCharsAt(buffer, 0, chars); 
            buffer.ShouldBe(expected);
        }

        [Theory]
        [InlineData(12, 1200)] 
        [InlineData(13, 1300)] 
        public void TestGetCounter(ushort value, int expected) { S7.GetCounter(value).ShouldBe(expected); }

        [Theory]
        [InlineData(new ushort[]{12},0, 1200)]
        [InlineData(new ushort[]{0,12},1, 1200)]
        public void TestGetCounterAt(ushort[] buffer, int index, int expected) { S7.GetCounterAt(buffer, index).ShouldBe(expected); }

        [Theory]
        [InlineData(1200, 18)]
        [InlineData(1300, 19)] 
        public void TestToCounter(int value, ushort expected) { S7.ToCounter(value).ShouldBe(expected); }

        [Theory]
        [InlineData(0, 1200, new ushort[] {18,0})]
        [InlineData(1, 1200, new ushort[] {0, 18})]
        public void TestSetCounterAt(int index, int counter, ushort[] expected)
        {
            var buffer = new ushort[2];
            S7.SetCounterAt(buffer, index, counter); 
            buffer.ShouldBe(expected);
        }

        [Theory]
        [InlineData(new byte[] { 18, 0,0,0,0,4,5,6,7,8,0,0,0,0,0,0,0,0,0 })] 
        public void TestGetS7TimerAt(byte[] buffer) { new S7TimerEqualityComparer().Equals(S7.GetS7TimerAt(buffer, 0), new S7Timer(buffer.Take(12).ToArray())).ShouldBe(true); }

        [Theory]
        [InlineData(10,new byte[] { 0, 0, 0, 10 })]
        public void TestSetS7TimespanAt(int milliseconds, byte[] expected)
        {
            var buffer = new byte[8];
            S7.SetS7TimespanAt(buffer, 0, TimeSpan.FromMilliseconds(milliseconds));
            buffer.Take(expected.Length).ToArray().ShouldBe(expected);
        }

        [Theory]
        [InlineData(new byte[] { 0, 0, 0, 10 }, 10)]
        public void TestGetS7TimespanAt(byte[] buffer, int milliseconds)
        {
            S7.GetS7TimespanAt(buffer, 0).ShouldBe(TimeSpan.FromMilliseconds(milliseconds));
        }
    }

    internal sealed class S7TimerEqualityComparer : IEqualityComparer<S7Timer>
    {
        public bool Equals(S7Timer x, S7Timer y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.PT.Equals(y.PT) && x.ET.Equals(y.ET) && x.IN == y.IN && x.Q == y.Q;
        }

        public int GetHashCode(S7Timer obj)
        {
            unchecked
            {
                var hashCode = obj.PT.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.ET.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.IN.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.Q.GetHashCode();
                return hashCode;
            }
        }
    }
}