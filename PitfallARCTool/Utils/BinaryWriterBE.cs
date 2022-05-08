using System.Linq;
using System.Text;

namespace System.IO
{
    public class BinaryWriterBE : BinaryWriter
    {
        byte[] I2B(int input)
        {
            var bytes = BitConverter.GetBytes(input);
            return new byte[] { bytes[0], bytes[1] };
        }

        public BinaryWriterBE(Stream input) : base(input)
        {
        }

        public BinaryWriterBE(Stream input, Encoding encoding) : base(input, encoding)
        {
        }

        public BinaryWriterBE(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen)
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

        public override void Write(char c)
        {
            Write(c, false);
        }

        public void Write(char c, bool isUnicode)
        {
            if(isUnicode)
                Write((UInt16)c);
            else
                Write((Byte)c);
        }

        public override void Write(char[] ch)
        {
            Write(ch, false);
        }

        public void Write(char[] ch, bool isUnicode)
        {
            for (int i = 0; i < ch.Length; i++)
            {
                if (isUnicode)
                    Write(i < ch.Length ? ch[i] : (char)0, true);
                else
                    Write(i < ch.Length ? ch[i] : (char)0);
            }
        }

        public override void Write(decimal d)
        {
            throw new NotImplementedException();
        }

        public override void Write(double v)
        {
            Write(BitConverter.GetBytes(v).Reverse().ToArray());
        }

        public override void Write(short v)
        {
            Write(BitConverter.GetBytes(v).Reverse().ToArray());
        }

        public override void Write(int v)
        {
            Write(BitConverter.GetBytes(v).Reverse().ToArray());
        }

        public override void Write(long v)
        {
            Write(BitConverter.GetBytes(v).Reverse().ToArray());
        }

        public override void Write(sbyte v)
        {
            base.Write(v);
        }

        public override void Write(float v)
        {
            Write(v, false);
        }

        public void Write(float v, bool half)
        {
            if(half)
            {
                int fbits = BitConverter.ToInt32(BitConverter.GetBytes(v), 0);
                int sign = fbits >> 16 & 0x8000;
                int val = (fbits & 0x7fffffff) + 0x1000;
                if (val >= 0x47800000)
                {
                    if ((fbits & 0x7fffffff) >= 0x47800000)
                    {
                        if (val < 0x7f800000)
                            Write(I2B(sign | 0x7c00));
                        else
                            Write(I2B(sign | 0x7c00 | (fbits & 0x007fffff) >> 13));
                    }
                    else
                        Write(I2B(sign | 0x7bff));
                }
                else
                {
                    if (val >= 0x38800000)
                        Write(I2B(sign | val - 0x38000000 >> 13));
                    else if (val < 0x33000000)
                        Write(I2B(sign));
                    else
                    {
                        val = (fbits & 0x7fffffff) >> 23;
                        Write(I2B(sign | ((fbits & 0x7fffff | 0x800000) + (0x800000 >> val - 102) >> 126 - val)));
                    }
                }
            }
            else
                Write(BitConverter.GetBytes(v).Reverse().ToArray());
        }

        public override void Write(String str)
        {
            Write(str, false);
        }

        public void Write(String str, bool isUnicode)
        {
            foreach (var c in str)
            {
                if (isUnicode)
                    Write(c, true);
                else
                    Write(c);
            }
        }

        public override void Write(ushort v)
        {
            Write(BitConverter.GetBytes(v).Reverse().ToArray());
        }

        public override void Write(uint v)
        {
            Write(BitConverter.GetBytes(v).Reverse().ToArray());
        }

        public override void Write(ulong v)
        {
            Write(BitConverter.GetBytes(v).Reverse().ToArray());
        }

        public override string ToString()
        {
            return base.ToString();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
