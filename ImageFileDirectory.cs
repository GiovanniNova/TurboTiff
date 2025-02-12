using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace TurboTiff
{
    public class ImageFileDirectory
    {
        private BinaryReader Reader { get; }

        public List<Field> Entries { get; }
        public uint NextIFDOffset { get; }

        public ImageFileDirectory(BinaryReader reader) {
            Reader = reader;

            var fields = Reader.ReadUInt16();

            Entries = new List<Field>();
            for (int i = 0; i < fields; i++)
            {
                Entries.Add(new Field(Reader));
            }

            NextIFDOffset = Reader.ReadUInt32();
        }

        public bool ContainsField(Tag tag, out Field? entryOut)
        {
            foreach (var entry in Entries)
            {
                if (entry.Tag == tag)
                {
                    entryOut = entry;
                    return true;
                }
            }

            entryOut = null;
            return false;
        }

        public void SaveDirectory(BinaryWriter writer)
        {
            // IFD Entry count
            writer.Write((ushort)Entries.Count);

            // Save IFD entries position
            var startPosition = writer.BaseStream.Position;

            // Skip all would-be IFD entries + Next IFD offset
            writer.BaseStream.Position = startPosition + Entries.Count * 12 + 4;

            // Write Strips
            if (ContainsField(Tag.StripOffsets, out var stripOffsetsEntry) && ContainsField(Tag.StripByteCounts, out var stripByteCounts))
            {
                for (int i = 0; i < stripOffsetsEntry.Count; i++)
                {
                    Reader.BaseStream.Seek((uint)stripOffsetsEntry.Values[i], SeekOrigin.Begin);
                    var byteCount = (uint)stripByteCounts.Values[i];

                    stripOffsetsEntry.Values[i] = (uint)writer.BaseStream.Position;
                    writer.Write(Reader.ReadBytes((int)byteCount));
                }
            }

            // Write Values of IFD entries
            foreach (var entry in Entries)
            {
                entry.WriteValue(writer);
            }

            // Seek back to entries position and write headers
            writer.BaseStream.Position = startPosition;
            foreach (var entry in Entries)
            {
                entry.WriteHeader(writer);
            }
        }

        public byte[] ExtractDirectory()
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    SaveDirectory(writer);
                }

                return stream.ToArray();
            }
        }
    }
}
