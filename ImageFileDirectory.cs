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

        public int Width() {
            if (ContainsField(Tag.ImageWidth, out var field))
                return (int)field.GetIntegerValues()[0];

            return 0;
        }
        public int Height()
        {
            if (ContainsField(Tag.ImageLength, out var field))
                return (int)field.GetIntegerValues()[0];

            return 0;
        }

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

        public byte[] SaveStrip(bool singleStrip = true)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    // Write Strips
                    if (ContainsField(Tag.StripOffsets, out var stripOffsetsEntry) && ContainsField(Tag.StripByteCounts, out var stripByteCounts))
                    {
                        for (int i = 0; i < stripOffsetsEntry.Count; i++)
                        {
                            Reader.BaseStream.Seek((uint)stripOffsetsEntry.Values[i], SeekOrigin.Begin);
                            var byteCount = (uint)stripByteCounts.Values[i];

                            writer.Write(Reader.ReadBytes((int)byteCount));
                        }
                    }
                }

                return stream.ToArray();
            }
        }

        public long SaveDirectory(BinaryWriter writer)
        {
            // Write Strips
            if (ContainsField(Tag.StripOffsets, out var stripOffsetsEntry) && ContainsField(Tag.StripByteCounts, out var stripByteCounts))
            {
                for (int i = 0; i < stripOffsetsEntry.Count; i++)
                {
                    Reader.BaseStream.Seek((uint)stripOffsetsEntry.Values[i], SeekOrigin.Begin);
                    var byteCount = (uint)stripByteCounts.Values[i];

                    var offset = (long)(MathF.Ceiling(writer.BaseStream.Position / 4.0f) * 4);
                    writer.BaseStream.Position = offset;

                    stripOffsetsEntry.Values[i] = (uint)offset;
                    writer.Write(Reader.ReadBytes((int)byteCount));
                }
            }

            var offset2 = (long)(MathF.Ceiling(writer.BaseStream.Position / 4.0f) * 4);
            writer.BaseStream.Position = offset2;

            // Save IFD entries position
            var firstIFD = writer.BaseStream.Position;

            // IFD Entry count
            writer.Write((ushort)Entries.Count);

            // Skip IFD entries + next IFD offset
            writer.BaseStream.Position += 12 * Entries.Count + 4;

            // Write Values of IFD entries
            foreach (var entry in Entries)
            {
                entry.WriteValue(writer);
            }

            // Seek to IFD entries and skip 2 bytes from entry count
            writer.BaseStream.Position = firstIFD + 2;

            // Write IFD Entries
            foreach (var entry in Entries)
            {
                entry.WriteHeader(writer);
            }

            return firstIFD;
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
