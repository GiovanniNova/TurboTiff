namespace TurboTiff
{
    public class TiffReader : IDisposable
    {
        public List<ImageFileDirectory> IFDs { get; }

        private BinaryReader Reader { get; }

        public TiffReader(Stream stream) {
            Reader = new BinaryReader(stream);
            
            var format = Reader.ReadUInt16();

            if (format == 18761)
                Console.WriteLine("Little-endian");
            else if (format == 19789)
                Console.WriteLine("Big-endian");
            else
                return;

            var identifier = Reader.ReadUInt16();

            if (identifier != 42)
                return;

            var nextIFDOffset = Reader.ReadUInt32();

            IFDs = new List<ImageFileDirectory>();

            while (nextIFDOffset != 0)
            {
                Reader.BaseStream.Seek(nextIFDOffset, SeekOrigin.Begin);

                var ifd = new ImageFileDirectory(Reader);             
                IFDs.Add(ifd);

                nextIFDOffset = ifd.NextIFDOffset;
            }
            
        }

        public byte[] SavePageStrip(int page, bool singleStrip=true)
        {
            return IFDs[page].SaveStrip(singleStrip);
        }

        public byte[] SavePage(int page) {
            var ifd = IFDs[page];

            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    // Byte order indicator
                    writer.Write((byte)0x49);
                    writer.Write((byte)0x49);

                    // TIFF identificator
                    writer.Write((ushort)42);

                    // Skip to first IFD
                    writer.BaseStream.Position = 8;
                    
                    var firstIFD = ifd.SaveDirectory(writer);

                    // Write blank next IFD offset
                    writer.Write((uint)0);

                    // Seek back to first IFD offset
                    writer.BaseStream.Position = 4;

                    // Write offset
                    writer.Write((uint)firstIFD);                    
                }

                return stream.ToArray();
            }
        }

        public void Dispose() {
            Reader.Close();
            Reader.Dispose();
        }
    }
}
