using System;
using System.IO;

namespace PitfallARCTool.Utils
{
    public class CRC32
    {
        Byte reflect8(Byte val)
        {
            Byte res = 0;
            for (var i = 0; i < 8; i++)
            {
                if ((val & (1 << i)) != 0)
                {
                    res |= (Byte)(1 << (7 - i));
                }
            }
            return res;
        }

        UInt16 reflect16(UInt16 val)
        {
            UInt16 res = 0;
            for (var i = 0; i < 16; i++)
            {
                if (((Int16)val & (1 << i)) != 0)
                {
                    res |= (UInt16)(1 << (15 - i));
                }
            }
            return res;
        }

        UInt32 reflect32(UInt32 val)
        {
            UInt32 res = 0;
            for (var i = 0; i < 32; i++)
            {
                if (((Int32)val & (1 << i)) != 0)
                {
                    res |= (UInt32)(1 << (31 - i));
                }
            }
            return res;
        }

        UInt64 reflect64(UInt64 val)
        {
            UInt64 res = 0;
            for (var i = 0; i < 64; i++)
            {
                if (((Int64)val & (1L << i)) != 0)
                {
                    res = (UInt64)((Int64)res | 1L << (63 - i));
                }
            }
            return res;
        }

        UInt32[] Table = null;
        UInt32 InitialValue;
        UInt32 XORValue;

        public static CRC32 Instance = null;

        public CRC32(UInt32 initialValue, UInt32 polynomial = 0x04c11db7, UInt32 xor=UInt32.MaxValue, bool reflectedTable=false)
        {
            int i, ref_i, j;
            UInt32 k;

            InitialValue = initialValue;
            XORValue = xor;
            Table = new UInt32[256];
            for (i=0;i<Table.Length;i++)
            {
                if(reflectedTable)
                {
                    ref_i = reflect8((Byte)i);
                    k = (UInt32)ref_i << 24;
                }
                else
                    k = (UInt32)i << 24;

                for (j=0;j<8;j++)
                {
                    if ((k & (1 << 31)) != 0)
                        k = (((k << 1) & 0xffffffff) ^ polynomial);
                    else
                        k = (k << 1);
                }

                if (reflectedTable)
                    k = reflect32(k);

                Table[i] = k;
            }
        }

        public CRC32() : this(UInt32.MaxValue) { }

        public UInt32 ComputeHash(byte[] datas, bool reflectedInput=false, bool reflectedResult=false)
        {
            UInt32 curByte = 0;
            UInt32 crc = InitialValue;
            Int32 pos = 0;
            foreach(var data in datas)
            {
                curByte = data;

                if (reflectedInput)
                    curByte = reflect8((Byte)curByte);

                crc ^= curByte << 24;
                pos = (Int32)(crc >> 24) & 0xff;
                crc = ((crc << 8) ^ Table[pos]);
            }

            crc ^= XORValue;
            if (reflectedResult)
                crc = reflect32(crc);
            return crc;
        }
    }
}
