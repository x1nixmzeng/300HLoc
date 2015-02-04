using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace THHLoc
{
    using u32 = UInt32;
    using u8 = Byte;

    class HeroLocale : HeroHelper
    {
        class Entry
        {
	        public string name { get; private set; }
	        public string value { get; private set; }

	        public Entry()
	        {
		        name = "";
		        value = "";
	        }

            public Entry(string _name, string _value)
            {
                name = _name;
                value = _value;
            }
        	
	        public bool Read(BinaryReader br)
	        {
		        bool valid = true;

                u32 true_total_len = 0;
                u32 total_size = GetTrueTotalSize(br, ref true_total_len);
        		
		        u8 name_len = br.ReadByte();
		        name = DecodeString(br, name_len);
        		
                u32 rem_size = total_size - name_len - true_total_len;

                if (rem_size > 0)
                {
                    u32 value_len = GetTrueValueSize(br);

                    if (value_len > 0)
                    {
                        value = DecodeString(br, value_len);
                    }
                    else
                    {
                        valid = false;
                    }
                }

                // total bytes read is
                // total_size + true_total_len
        		
		        return valid;
	        }

            struct PackedSize
            {
                public u32 raw;
                public u8 size;
                public u8 hint;
            }

            static PackedSize PackSize(u32 size)
            {
                PackedSize ret = new PackedSize();
                ret.raw = size;
                ret.size = HeroHelper.PackSize(ret.raw, ref ret.hint);
                return ret;
            }

            struct SizeInfo
            {
                public PackedSize name;
                public PackedSize value;
                public PackedSize total_length;
            }

            static SizeInfo GetSizeInfo(byte[] name, byte[] val)
            {
                SizeInfo info; // = new SizeInfo();

                info.name = PackSize((u32)name.Length);
                info.value = PackSize((u32)val.Length);

                u32 total_len = 0;

                // name:
                // marker + size (+ hint if used)
                total_len += 1 + 1 + info.name.raw;
                if (info.name.hint != 0)
                {
                    total_len += 1;
                }

                // value:
                // (marker + size (+ hint if used) if used)
                if (info.value.raw > 0)
                {
                    total_len += 1 + 1 + info.value.raw;
                    if (info.value.hint != 0)
                    {
                        total_len += 1;
                    }
                }

                info.total_length = PackSize(total_len);

                return info;
            }

	        public bool Write(BinaryWriter bw)
	        {
                // 0xa <total size> 0xa <name size> <name> 0x12 <value size> <value>
                // sizes are encoded into 1 size byte, or 1 size byte and 1 hint byte

                byte[] name_encoded = EncodeString(name);
                byte[] value_encoded = EncodeString(value);
                
                SizeInfo sizes = GetSizeInfo(name_encoded, value_encoded);

                // total size
                bw.Write((u8)0xa);
                bw.Write(sizes.total_length.size);
                if (sizes.total_length.hint != 0)
                {
                    bw.Write(sizes.total_length.hint);
                }

                // name
                bw.Write((u8)0xa);
                bw.Write(sizes.name.size);
                if (sizes.name.hint!= 0)
                {
                    bw.Write(sizes.name.hint);
                }
                bw.Write(name_encoded, 0, name_encoded.Length);

                // value
                if (sizes.value.raw > 0)
                {
                    bw.Write((u8)0x12);
                    bw.Write(sizes.value.size);
                    if (sizes.value.hint!= 0)
                    {
                        bw.Write(sizes.value.hint);
                    }

                    bw.Write(value_encoded, 0, value_encoded.Length);
                }
        	
		        return true;
	        }
        }

        public class LocManager
        {
	        List<Entry> localeDb;

            public LocManager()
	        {
		        localeDb = new List<Entry>();
	        }

            // Text file with edits into database
            public bool ReadSource(string file_name)
            {
                bool valid = true;

                localeDb.Clear();

                valid &= File.Exists(file_name);

                if( valid )
                {
                    StreamReader src = new StreamReader(file_name);

                    string name;
                    while ((name = src.ReadLine()) != null)
                    {
                        string value = src.ReadLine();

                        if (value == null)
                        {
                            valid = false;
                            break;
                        }

                        localeDb.Add(new Entry(name, value));
                    }

                    src.Close();
                }

                return valid;
            }

            // Binary data used by game into database
	        public bool ReadBinary(string file_name)
	        {
		        bool valid = true;

                localeDb.Clear();

                valid &= File.Exists(file_name);

                if (valid)
                {
                    Stream file_handle = File.OpenRead(file_name);
                    BinaryReader br = new BinaryReader(file_handle);

                    valid &= br.BaseStream.Length > 0;

                    while (valid && (br.BaseStream.Position < br.BaseStream.Length))
                    {
                        Entry e = new Entry();
                        valid = e.Read(br);

                        if (valid)
                        {
                            localeDb.Add(e);
                        }
                    }

                    if (!valid)
                    {
                        localeDb.Clear();
                    }

                    file_handle.Close();
                }
        	
		        return valid;
	        }

            // Database as binary data for the game
            public bool WriteBinary(string file_name)
            {
                bool valid = true;

                valid &= (localeDb.Count > 0);
                if( valid )
                {
                    Stream file = File.Create(file_name);

                    valid &= file.CanWrite;
                    if (valid)
                    {
                        BinaryWriter bw = new BinaryWriter(file);

                        foreach (Entry e in localeDb)
                        {
                            valid &= e.Write(bw);
                        }
                    }

                    file.Close();
                }

                return valid;
            }

            // Database as text file for editing
            public bool WriteSource(string file_name)
            {
                bool valid = true;

                valid &= (localeDb.Count > 0);
                if( valid )
                {
                    StreamWriter dst = new StreamWriter(file_name);

                    foreach (Entry e in localeDb)
                    {
                        dst.WriteLine(e.name);
                        dst.WriteLine(e.value);
                    }

                    dst.Close();
                }

                return valid;
            }
        }
    }
}
