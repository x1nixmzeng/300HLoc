using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace THHLoc
{
    class HeroConverter
    {
        static string version_string = "0.02";

        string file1, file2;

        public HeroConverter(string[] args)
        {
            if (args.Length == 2)
            {
                file1 = args[0];
                file2 = args[1];
            }
        }

        static string GetExt(string filename)
        {
            int last_dot = filename.LastIndexOf('.');
            int last_sla = filename.LastIndexOf('\\');
            
            if( last_dot == -1 )
                return "";

            if( ( last_dot > last_sla ) || last_sla == -1 )
            {
                return filename.Substring(last_dot + 1);
            }

            if( last_sla == -1)
            {
                return filename;
            }

            return filename.Substring(last_sla + 1);
        }

        private void ShowInfo()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("\t300HLoc.exe src_file dst_file");
            Console.WriteLine("\t src_file Binary or text file");
            Console.WriteLine("\t dst_file Binary or text file");
            Console.WriteLine("\tBinary files do not have a specific extension");
            Console.WriteLine("\tText files must have the extension \".txt\"");
        }

        public bool Run()
        {
            bool valid = true;

            Console.WriteLine("300Heroes Locale Tool v{0}", version_string);
            Console.WriteLine("Written by WRS (xentax.com)");

            
            valid &= (file1 != null);
            valid &= (file2 != null);

            if( valid )
            {
                valid &= File.Exists(file1);
            }

            if (!valid)
            {
                ShowInfo();
            }
            else
            {
                HeroLocale.LocManager loc = new HeroLocale.LocManager();

                bool src_is_text = (GetExt(file1).ToLower() == "txt");
                bool dst_is_text = (GetExt(file2).ToLower() == "txt");

                if( src_is_text )
                {
                    valid &= loc.ReadSource(file1);
                }
                else
                {
                    valid &= loc.ReadBinary(file1);
                }

                if( !valid )
                {
                    Console.WriteLine("Error: Failed to read \"{0}\"", file1);
                }
                else
                {
                    if( dst_is_text )
                    {
                        valid &= loc.WriteSource(file2);
                    }
                    else
                    {
                        valid &= loc.WriteBinary(file2);
                    }

                    if( valid )
                    {
                        Console.WriteLine("Success! Written out \"{0}\"", file2);
                    }
                    else
                    {
                        Console.WriteLine("Error: Failed to write \"{0}\"", file2);
                    }
                }
            }

            return valid;        
        }
    }
}
