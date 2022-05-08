using System.Linq;
using System.Text;

namespace System.IO
{
    public class BinaryReaderBE : BinaryReader
    {
        public BinaryReaderBE(Stream input) : base(input)
        {
        }

        public BinaryReaderBE(Stream input, Encoding encoding) : base(input, encoding)
        {
        }

        public BinaryReaderBE(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen)
        {
        }

        public override Stream BaseStream => base.BaseStream;

        public override void Close()
        {
            base.Close();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override int PeekChar()
        {
            return base.PeekChar();
        }

        public override int Read()
        {
            return base.Read();
        }

        public override int Read(char[] buffer, int index, int count)
        {
            return base.Read(buffer, index, count);
        }

        public override int Read(byte[] buffer, int index, int count)
        {
            return base.Read(buffer, index, count);
        }

        public override bool ReadBoolean()
        {
            return base.ReadBoolean();
        }

        public override byte ReadByte()
        {
            return base.ReadByte();
        }

        public override byte[] ReadBytes(int count)
        {
            return base.ReadBytes(count);
        }

        public override char ReadChar()
        {
            return (char)ReadByte();
        }

        public char ReadWChar()
        {
            return Encoding.Unicode.GetString(ReadBytes(2).Reverse().ToArray()).First();
        }

        public override char[] ReadChars(int count)
        {
            String str = "";
            for (int i = 0; i < count; i++) str += ReadChar();
            return str.ToCharArray();
        }

        public char[] ReadWChars(int count)
        {
            String str = "";
            for (int i = 0; i < count; i++) str += ReadWChar();
            return str.ToCharArray();
        }

        public override decimal ReadDecimal()
        {
            throw new NotImplementedException();
        }

        public override double ReadDouble()
        {
            return BitConverter.ToDouble(ReadBytes(8).Reverse().ToArray(), 0);
        }

        public override short ReadInt16()
        {
            return BitConverter.ToInt16(ReadBytes(2).Reverse().ToArray(), 0);
        }

        public override int ReadInt32()
        {
            return BitConverter.ToInt32(ReadBytes(4).Reverse().ToArray(), 0);
        }

        public override long ReadInt64()
        {
            return BitConverter.ToInt64(ReadBytes(8).Reverse().ToArray(), 0);
        }

        public override sbyte ReadSByte()
        {
            return base.ReadSByte();
        }

        public override float ReadSingle()
        {
            return BitConverter.ToSingle(ReadBytes(4).Reverse().ToArray(), 0);
        }

        public float ReadHalfSingle()
        {
            var intVal = BitConverter.ToInt32(new byte[] { this.ReadByte(), this.ReadByte(), 0, 0 }, 0);

            int mant = intVal & 0x03ff;
            int exp = intVal & 0x7c00;
            if (exp == 0x7c00) exp = 0x3fc00;
            else if (exp != 0)
            {
                exp += 0x1c000;
                if (mant == 0 && exp > 0x1c400)
                    return BitConverter.ToSingle(BitConverter.GetBytes((intVal & 0x8000) << 16 | exp << 13 | 0x3ff), 0);
            }
            else if (mant != 0)
            {
                exp = 0x1c400;
                do
                {
                    mant <<= 1;
                    exp -= 0x400;
                } while ((mant & 0x400) == 0);
                mant &= 0x3ff;
            }
            return BitConverter.ToSingle(BitConverter.GetBytes((intVal & 0x8000) << 16 | (exp | mant) << 13), 0);
        }

        public override string ReadString()
        {
            char c = '\0';
            String str = "";
            while ((c = ReadChar()) != 0) str += c;
            return str;
        }

        public string ReadWString()
        {
            char c = '\0';
            String str = "";
            while ((c = ReadWChar()) != 0) str += c;
            return str;
        }

        public override ushort ReadUInt16()
        {
            return BitConverter.ToUInt16(ReadBytes(2).Reverse().ToArray(), 0);
        }

        public override uint ReadUInt32()
        {
            return BitConverter.ToUInt32(ReadBytes(4).Reverse().ToArray(), 0);
        }

        public override ulong ReadUInt64()
        {
            return BitConverter.ToUInt64(ReadBytes(8).Reverse().ToArray(), 0);
        }

        public override string ToString()
        {
            return base.ToString();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        protected override void FillBuffer(int numBytes)
        {
            base.FillBuffer(numBytes);
        }
    }
}
