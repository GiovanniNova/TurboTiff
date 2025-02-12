using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TurboTiff
{
    public class Field
    {
        private BinaryReader Reader { get; }

        public Tag Tag { get; }
        public Type Type { get; }
        public uint Count { get; }
        private byte[] ValueOffset { get; set; }

        private List<object> values;

        public List<object> Values
        {
            get
            {
                if (values == null)
                {
                    values = new List<object>();

                    if (HasValue())
                    {
                        if (Type == Type.Undefined)
                            values.Add(ValueOffset);
                        else if (Type == Type.ASCII)
                            values.Add(Encoding.ASCII.GetString(ValueOffset));
                        else
                        {
                            for (int i = 0; i < Count; i+=TypeSize())
                            {
                                switch (Type)
                                {
                                    case Type.Byte:
                                        values.Add(ValueOffset[i]);
                                        break;
                                    case Type.SByte:
                                        values.Add((sbyte)ValueOffset[i]);
                                        break;
                                    case Type.Short:
                                        values.Add(BitConverter.ToUInt16(ValueOffset[i..(i + 2)]));
                                        break;
                                    case Type.SShort:
                                        values.Add(BitConverter.ToInt16(ValueOffset[i..(i + 2)]));
                                        break;
                                    case Type.Long:
                                        values.Add(BitConverter.ToUInt32(ValueOffset));
                                        break;
                                    case Type.SLong:
                                        values.Add(BitConverter.ToInt32(ValueOffset));
                                        break;
                                    case Type.Float:
                                        values.Add(BitConverter.ToSingle(ValueOffset));
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                    else
                    {
                        Reader.BaseStream.Seek(BitConverter.ToUInt32(ValueOffset), SeekOrigin.Begin);

                        if (Type == Type.Undefined)
                            values.Add(Reader.ReadBytes((int)Count));
                        else if (Type == Type.ASCII)
                            values.Add(Encoding.ASCII.GetString(Reader.ReadBytes((int)Count)));
                        else
                        {
                            for (int i = 0; i < Count; i++)
                            {
                                switch (Type)
                                {
                                    case Type.Byte:
                                        values.Add(Reader.ReadByte());
                                        break;
                                    case Type.Short:
                                        values.Add(Reader.ReadUInt16());
                                        break;
                                    case Type.Long:
                                        values.Add(Reader.ReadUInt32());
                                        break;
                                    case Type.Rational:
                                        values.Add(new uint[] { Reader.ReadUInt32(), Reader.ReadUInt32() });
                                        break;
                                    case Type.SByte:
                                        values.Add(Reader.ReadSByte());
                                        break;
                                    case Type.SShort:
                                        values.Add(Reader.ReadSByte());
                                        break;
                                    case Type.SLong:
                                        values.Add(Reader.ReadInt16());
                                        break;
                                    case Type.SRational:
                                        values.Add(new int[] { Reader.ReadInt32(), Reader.ReadInt32() });
                                        break;
                                    case Type.Float:
                                        values.Add(Reader.ReadSingle());
                                        break;
                                    case Type.Double:
                                        values.Add(Reader.ReadDouble());
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                }

                return values;
            }
        }

        public Field(BinaryReader reader)
        {
            Reader = reader;

            Tag = (Tag)Reader.ReadUInt16();
            Type = (Type)Reader.ReadUInt16();
            Count = Reader.ReadUInt32();
            ValueOffset = Reader.ReadBytes(4);
        }

        internal int TypeSize() {
            switch (Type)
            {
                case Type.Byte:
                case Type.SByte:
                case Type.ASCII:
                case Type.Undefined:
                    return 1;
                    break;
                case Type.Short:
                case Type.SShort:
                    return 2;
                    break;
                case Type.Long:
                case Type.SLong:
                case Type.Float:
                    return 4;
                    break;
                case Type.Rational:
                case Type.SRational:
                case Type.Double:
                    return 8;
                    break;
                default:
                    break;
            }

            return -1;
        }

        internal bool HasValue() {
            return TypeSize() * Count <= 4;
        }

        public void WriteHeader(BinaryWriter writer)
        {
            writer.Write((ushort)Tag);
            writer.Write((ushort)Type);
            writer.Write(Count);

            if (HasValue())
            {
                WriteValue_Internal(writer);

                for (int i = 0; i < 4 - TypeSize() * Count; i++)
                {
                    writer.Write((byte)0x0);
                }
            }
            else
                writer.Write(ValueOffset);
            
        }

        internal void WriteValue_Internal(BinaryWriter writer)
        {
            if (Type == Type.Undefined)
                writer.Write((byte[])values[0]);
            else if (Type == Type.ASCII)
                writer.Write(Encoding.ASCII.GetBytes((string)Values[0]));
            else
                foreach (var value in Values)
                {
                    switch (Type)
                    {
                        case Type.Byte:
                            writer.Write((byte)value);
                            break;
                        case Type.Short:
                            writer.Write((ushort)value);
                            break;
                        case Type.Long:
                            writer.Write((uint)value);
                            break;
                        case Type.Rational:
                            var rational = (uint[])value;
                            writer.Write(rational[0]);
                            writer.Write(rational[1]);
                            break;
                        case Type.SByte:
                            writer.Write((sbyte)value);
                            break;
                        case Type.SShort:
                            writer.Write((short)value);
                            break;
                        case Type.SLong:
                            writer.Write((int)value);
                            break;
                        case Type.SRational:
                            var sRational = (int[])value;
                            writer.Write(sRational[0]);
                            writer.Write(sRational[1]);
                            break;
                        case Type.Float:
                            writer.Write((float)value);
                            break;
                        case Type.Double:
                            writer.Write((double)value);
                            break;
                        default:
                            break;
                    }
                }
        }

        public void WriteValue(BinaryWriter writer)
        {
            if (HasValue())
                return;
            else
            {
                ValueOffset = BitConverter.GetBytes((uint)writer.BaseStream.Position);

                WriteValue_Internal(writer);
            }
        }
    }
}
