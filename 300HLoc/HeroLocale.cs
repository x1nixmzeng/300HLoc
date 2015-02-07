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
            // wasteful. likely serialization - see the PREFIX_ bytes in HeroHelper
            public string name { get; private set; }
            public string value { get; private set; }
            public u32 item_id { get; private set; }

            public Entry()
            {
                name = "";
                value = "";
                item_id = 0;
            }

            public Entry(string _name, string _value)
            {
                name = _name;
                value = _value;
                item_id = 0;
            }

            public Entry(string _name, string _value, u32 _item_id)
            {
                name = _name;
                value = _value;
                item_id = _item_id;
            }
            
            public bool Read(BinaryReader br)
            {
                u8 prefix = br.ReadByte();
                if(prefix != PREFIX_SIZE)
                {
                    return false;
                }

                EncodedInt encSize = new EncodedInt();
                encSize.Read(br);

                u32 size_expected = encSize.Value;
                u32 size_read = 0;

                bool reading_items = false;
                prefix = br.ReadByte();
                size_read++;

                switch( prefix )
                {
                    case PREFIX_ITEM_ID:
                        reading_items = true;
                        break;
                    case PREFIX_CLIENT_KEY:
                        break;
                    default:
                        return false;
                }

                if( reading_items )
                {
                    EncodedInt encId = new EncodedInt();
                    encId.Read(br);
                    item_id = encId.Value;
                    size_read += encId.Length();

                    if( size_read == size_expected )
                    {
                        return true;
                    }

                    prefix = br.ReadByte();
                    size_read++;

                    if( prefix != PREFIX_ITEM_NAME )
                    {
                        return false;
                    }
                }

                EncodedInt encName = new EncodedInt();
                encName.Read(br);

                name = DecodeString(br, encName.Value);
                size_read += encName.Value + encName.Length();

                if( size_read == size_expected )
                {
                    return true;
                }

                prefix = br.ReadByte();
                size_read++;

                if( reading_items )
                {
                    if( prefix != PREFIX_ITEM_VALUE )
                    {
                        return false;
                    }
                }
                else if (prefix != PREFIX_CLIENT_VAL)
                {
                    return false;
                }

                EncodedInt encValue = new EncodedInt();
                encValue.Read(br);
                value = DecodeString(br, encValue.Value);

                size_read += encValue.Value + encValue.Length(); ;

                if( size_read != size_expected )
                {
                    return false;
                }

                return true;
            }

            public bool Write(BinaryWriter bw)
            {
                byte[] name_encoded = EncodeString(name);
                byte[] value_encoded = EncodeString(value);

                EncodedInt encName = new EncodedInt((u32)name_encoded.Length);
                EncodedInt encValue = new EncodedInt((u32)value_encoded.Length);
                
                u32 total_size = 0;

                EncodedInt encItem = null;

                if( item_id != 0 )
                {
                    encItem = new EncodedInt(item_id);
                    total_size += 1 + encItem.Length();
                }

                if( encName.Value > 0 )
                {
                    total_size += 1 + encName.Length() + encName.Value;
                }

                if (encValue.Value > 0)
                {
                    total_size += 1 + encValue.Length() + encValue.Value;
                }

                // write total size
                EncodedInt encTotal = new EncodedInt(total_size);
                bw.Write(PREFIX_SIZE);
                encTotal.Write(bw);

                if( encItem != null )
                {
                    // write item_id
                    bw.Write(PREFIX_ITEM_ID);
                    encItem.Write(bw);

                    // write name
                    if (encName.Value > 0)
                    {
                        bw.Write(PREFIX_ITEM_NAME);
                        encName.Write(bw);
                        bw.Write(name_encoded, 0, name_encoded.Length);
                    }

                    // write value
                    if (encValue.Value > 0)
                    {
                        bw.Write(PREFIX_ITEM_VALUE);
                        encValue.Write(bw);
                        bw.Write(value_encoded, 0, value_encoded.Length);
                    }
                }
                else
                {
                    // write name
                    if (encName.Value > 0)
                    {
                        bw.Write(PREFIX_CLIENT_KEY);
                        encName.Write(bw);
                        bw.Write(name_encoded, 0, name_encoded.Length);
                    }

                    // write value
                    if (encValue.Value > 0)
                    {
                        bw.Write(PREFIX_CLIENT_VAL);
                        encValue.Write(bw);
                        bw.Write(value_encoded, 0, value_encoded.Length);
                    }
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

                    u32 item_id = 0;
                    string name;
                    while ((name = src.ReadLine()) != null)
                    {
                        // horrible check that name is actually the item_id
                        if( GetNumber(name, ref item_id) )
                        {
                            name = src.ReadLine();
                        }

                        string value = src.ReadLine();

                        if (value == null)
                        {
                            valid = false;
                            break;
                        }

                        localeDb.Add(new Entry(name, value, item_id));
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
                if (valid)
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
                if (valid)
                {
                    StreamWriter dst = new StreamWriter(file_name);

                    foreach (Entry e in localeDb)
                    {
                        if (e.item_id != 0)
                        {
                            dst.WriteLine(e.item_id);
                        }
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
