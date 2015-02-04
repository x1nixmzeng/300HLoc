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

        public static string DecodeString(BinaryReader br, u32 size)
        {
            byte[] b = br.ReadBytes((int)size);

            return chEnc.GetString(b);
        }

        public static byte[] EncodeString(string src)
        {
            return chEnc.GetBytes(src);
        }

        public static u32 UnpackSize(u8 packed, u8 hint)
        {
            u32 result = (u32)packed;

            if (hint == 1)
            {
                result |= 0x80;
            }
            else if (hint > 1)
            {
                // xxxx refactor this stuff:
                if ((hint & 1) == 0)
                {
                    result ^= 0x80;
                }

                result |= (u32)(hint >> 1) << 8;
            }

            return result;
        }

        public static u8 PackSize(u32 size, ref u8 hint)
        {
            u8 result = (u8)(size & 0xFF);

            // xxxx refactor this function

            if (size > 0xFF)
            {
                hint = (u8)((size >> 7) & 0xF);

                if ((size & 0x80) == 0)
                {
                    result ^= 0x80;
                }
            }
            else
            {
                if( (size&0x80) != 0 )
                {
                    hint = 1;
                }
                else
                {
                    hint = 0;
                }
            }

            return result;
        }

        public static u32 GetTrueTotalSize(BinaryReader br, ref u32 len)
        {
            u32 size = 0;

            u8 prefix = br.ReadByte();

#if DEBUG
            if (prefix != 0xA) throw new Exception("Unsupported size prefix");
#endif

            u8 packed_size = br.ReadByte();
            u8 possible_hint = br.ReadByte();

            if (possible_hint < 0xa)
            {
                u8 real_postfix = br.ReadByte();
#if DEBUG
                if (real_postfix != 0xA) throw new Exception("Unsupported size postfix");
#endif
                len = 3;

                size = UnpackSize(packed_size, possible_hint);
            }
            else
            {
#if DEBUG
                if (possible_hint != 0xA) throw new Exception("Unsupported size postfix");
#endif
                len = 2;

                // nothing to do
                size = (u32)packed_size;
            }

            return size;
        }

        public static u32 GetTrueValueSize(BinaryReader br)
        {
            u32 size = 0;

            u8 prefix = br.ReadByte();
#if DEBUG
            if (prefix != 0x12) throw new Exception("Unsupported size prefix");
#endif

            u8 packed_size = br.ReadByte();
            u8 possible_hint = br.ReadByte(); // really need a single peek

            if (possible_hint < 0xa)
            {
                size = UnpackSize(packed_size, possible_hint);
            }
            else
            {
                // Assuming this isn't a hint, so ignoring

                br.BaseStream.Position -= 1;
                size = (u32)packed_size;
            }

            return size;
        }
    }
}
