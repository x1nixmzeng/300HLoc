using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace THHLoc
{
    class HeroConverter
    {
        static string version_string = "0.01";

        string srcFile;

        public HeroConverter(string[] args)
        {
            if (args.Length == 1)
            {
                srcFile = args[0];
            }
        }

        private string GetExt(string filename)
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
            //Console.WriteLine("\t300HLoc.exe source_file target_file");
        }

        public bool Run()
        {
            bool valid = true;

            Console.WriteLine("300Heroes Locale Tool v{0}", version_string);
            Console.WriteLine("Written by WRS (xentax.com)");

            valid &= srcFile != null;
            if (valid)
            {
                valid &= File.Exists(srcFile);
                if (valid)
                {
                    HeroLocale.LocManager loc = new HeroLocale.LocManager();

                    string test_source = "dump.txt";

                    // binary -> binary + source
                    loc.ReadBinary(srcFile);
                    loc.WriteBinary("dump.dat");
                    loc.WriteSource(test_source);
                    
                    // source -> binary
                    loc.ReadSource(test_source);
                    loc.WriteBinary("dump2.dat");
                }
            }

            return valid;        
        }
    }
}
