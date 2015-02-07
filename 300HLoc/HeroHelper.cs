using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace THHLoc
{
    using u32 = UInt32;
    using u8 = Byte;

    class HeroHelper
    {
        // Treating encoding as Chinese Simplified (ISO-2022)

        // See list of supported encodings:
        // https://msdn.microsoft.com/en-us/library/system.text.encoding%28v=vs.110%29.aspx

        static Encoding chEnc = Encoding.GetEncoding("x-cp50227");

        static string SourceNewLineMarker = "<NEWLINE>";

        public static string DecodeString(BinaryReader br, u32 size)
        {
            byte[] b = br.ReadBytes((int)size);

            string str = chEnc.GetString(b);
            return str.Replace("\n", SourceNewLineMarker);
        }

        public static byte[] EncodeString(string src)
        {
            string str = src.Replace(SourceNewLineMarker, "\n");
            return chEnc.GetBytes(str);
        }

        // based on http://andrewfwang.com/2013/01/15/7bitints/
        public class EncodedInt
        {
            public u32 Value { get; private set; }

            public EncodedInt()
            {
                Value = 0;
            }

            public EncodedInt(u32 val)
            {
                Value = val;
            }

            public u32 Length()
            {
                u32 val = Value;
                u32 len = 1;

                while (val >= 0x80)
                {
                    u8 b = (u8)(val | 0x80);
                    val >>= 7;
                    len++;
                }

                return len;
            }

            public void Write(BinaryWriter bw)
            {
                u32 val = Value;

                while (val >= 0x80)
                {
                    u8 b = (u8)(val | 0x80);
                    
                    bw.Write(b);
                    val >>= 7;
                }

                bw.Write((u8)(val & 0xFF));
            }

            public void Read(BinaryReader br)
            {
                int val = 0;
                int shift = 0;

                while (shift < (5 * 7))
                {
                    u8 b = br.ReadByte();
                    
                    val |= ((b & 0x7F) << shift);
                    shift += 7;

                    if ((b & 0x80) == 0)
                    {
                        break;
                    }
                }

                Value = (u32)val;
            }
        }

        public static bool GetNumber(string val, ref u32 result)
        {
            bool valid = true;

            try
            {
                result = 0;
                result = Convert.ToUInt32(val);
            }
            catch
            {
                valid = false;
            }

            return valid;
        }

        public const u8 PREFIX_SIZE = 0xA;

        // client
        public const u8 PREFIX_CLIENT_KEY = 0xA;
        public const u8 PREFIX_CLIENT_VAL = 0x12;

        // item_lan
        public const u8 PREFIX_ITEM_ID = 0x8;
        public const u8 PREFIX_ITEM_NAME = 0x12;
        public const u8 PREFIX_ITEM_VALUE = 0x1A;
    }
}
