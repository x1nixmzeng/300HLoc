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
        	
	        public bool Write(BinaryWriter bw)
	        {
                bool valid = true;

                // todo: byte array for this?!
                
                // todo: encode name and value
                // we can get flags and figure out things from here
        	
		        return valid;
	        }
        }

        public class Reader
        {
	        List<Entry> localeDb;
        	
	        public Reader()
	        {
		        localeDb = new List<Entry>();
	        }

            // Text file with edits into database
            public bool ReadSource(string file_name)
            {
                bool valid = true;

                valid &= (localeDb.Count == 0);
                if (valid)
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
	        public bool ReadBinary(BinaryReader br)
	        {
		        bool valid = true;

		        while( valid && ( br.BaseStream.Position < br.BaseStream.Length ) )
		        {
                    Entry e = new Entry();
			        valid = e.Read(br);
        			
			        if( valid )
			        {
				        localeDb.Add(e);
			        }
		        }
        		
		        if( !valid )
		        {
			        localeDb.Clear();
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
