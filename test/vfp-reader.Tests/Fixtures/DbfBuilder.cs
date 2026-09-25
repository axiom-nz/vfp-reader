using System;
using System.Collections.Generic;
using System.Text;

namespace VfpReader.Tests.Fixtures
{
    /// <summary>
    /// Builds a DBF byte array one field and one record at a time, so tests can pin each format
    /// rule without shipping a binary fixture for every case. It writes only what the reader is
    /// supposed to understand, never using the reader itself.
    /// </summary>
    internal sealed class DbfBuilder
    {
        private readonly List<FieldSpec> _fields = new List<FieldSpec>();
        private readonly List<RecordSpec> _records = new List<RecordSpec>();

        public byte Version { get; set; } = 0x30;

        public byte Year { get; set; } = 124;

        public byte Month { get; set; } = 1;

        public byte Day { get; set; } = 2;

        public byte LanguageDriver { get; set; } = 0x03;

        public string DatabasePath { get; set; } = string.Empty;

        public bool HasMemoFlag { get; set; }

        public bool HasIndexFlag { get; set; }

        public bool IsDatabaseFlag { get; set; }

        /// <summary>Overrides the computed record length, for malformed-layout tests.</summary>
        public int? ExplicitRecordLength { get; set; }

        public DbfBuilder AddField(
            string name,
            char type,
            byte length,
            byte decimals = 0,
            bool nullable = false,
            bool system = false,
            bool binary = false,
            byte autoIncrementStep = 0,
            uint autoIncrementNext = 0)
        {
            _fields.Add(new FieldSpec(name, type, length, decimals, nullable, system, binary, autoIncrementStep, autoIncrementNext));
            return this;
        }

        public DbfBuilder AddRecord(bool deleted, byte[] payload)
        {
            _records.Add(new RecordSpec(deleted, payload));
            return this;
        }

        public byte[] Build()
        {
            bool visualFoxPro = Version == 0x30 || Version == 0x31 || Version == 0x32;
            int backlinkLength = visualFoxPro ? 263 : 0;
            int headerLength = 32 + (_fields.Count * 32) + 1 + backlinkLength;
            int recordLength = ExplicitRecordLength ?? (1 + SumLengths());
            int totalLength = headerLength + (recordLength * _records.Count);

            var bytes = new byte[totalLength];

            bytes[0] = Version;
            bytes[1] = Year;
            bytes[2] = Month;
            bytes[3] = Day;
            WriteUInt32(bytes, 4, (uint)_records.Count);
            WriteUInt16(bytes, 8, (ushort)headerLength);
            WriteUInt16(bytes, 10, (ushort)recordLength);

            byte flags = 0;
            if (HasIndexFlag)
            {
                flags |= 0x01;
            }

            if (HasMemoFlag)
            {
                flags |= 0x02;
            }

            if (IsDatabaseFlag)
            {
                flags |= 0x04;
            }

            bytes[28] = flags;
            bytes[29] = LanguageDriver;

            int position = 32;
            int fieldOffset = 1;
            foreach (FieldSpec field in _fields)
            {
                byte[] nameBytes = Encoding.ASCII.GetBytes(field.Name);
                int nameLength = Math.Min(nameBytes.Length, 11);
                Array.Copy(nameBytes, 0, bytes, position, nameLength);
                bytes[position + 11] = (byte)field.Type;
                WriteUInt32(bytes, position + 12, (uint)fieldOffset);
                bytes[position + 16] = field.Length;
                bytes[position + 17] = field.Decimals;

                byte fieldFlags = 0;
                if (field.System)
                {
                    fieldFlags |= 0x01;
                }

                if (field.Nullable)
                {
                    fieldFlags |= 0x02;
                }

                if (field.Binary)
                {
                    fieldFlags |= 0x04;
                }

                if (field.AutoIncrementStep > 0)
                {
                    fieldFlags |= 0x0C;
                }

                bytes[position + 18] = fieldFlags;
                WriteUInt32(bytes, position + 19, field.AutoIncrementNext);
                bytes[position + 23] = field.AutoIncrementStep;

                fieldOffset += field.Length;
                position += 32;
            }

            bytes[position] = 0x0D;
            position++;

            if (backlinkLength > 0 && DatabasePath.Length > 0)
            {
                byte[] pathBytes = Encoding.ASCII.GetBytes(DatabasePath);
                Array.Copy(pathBytes, 0, bytes, position, Math.Min(pathBytes.Length, backlinkLength));
            }

            int recordPosition = headerLength;
            foreach (RecordSpec record in _records)
            {
                bytes[recordPosition] = record.Deleted ? (byte)'*' : (byte)' ';
                int payloadLength = Math.Min(record.Payload.Length, Math.Max(0, recordLength - 1));
                if (payloadLength > 0)
                {
                    Array.Copy(record.Payload, 0, bytes, recordPosition + 1, payloadLength);
                }

                recordPosition += recordLength;
            }

            return bytes;
        }

        private int SumLengths()
        {
            int sum = 0;
            foreach (FieldSpec field in _fields)
            {
                sum += field.Length;
            }

            return sum;
        }

        private static void WriteUInt16(byte[] buffer, int offset, ushort value)
        {
            buffer[offset] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        }

        private static void WriteUInt32(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
        }

        private sealed class FieldSpec
        {
            internal FieldSpec(
                string name,
                char type,
                byte length,
                byte decimals,
                bool nullable,
                bool system,
                bool binary,
                byte autoIncrementStep,
                uint autoIncrementNext)
            {
                Name = name;
                Type = type;
                Length = length;
                Decimals = decimals;
                Nullable = nullable;
                System = system;
                Binary = binary;
                AutoIncrementStep = autoIncrementStep;
                AutoIncrementNext = autoIncrementNext;
            }

            internal string Name { get; }

            internal char Type { get; }

            internal byte Length { get; }

            internal byte Decimals { get; }

            internal bool Nullable { get; }

            internal bool System { get; }

            internal bool Binary { get; }

            internal byte AutoIncrementStep { get; }

            internal uint AutoIncrementNext { get; }
        }

        private sealed class RecordSpec
        {
            internal RecordSpec(bool deleted, byte[] payload)
            {
                Deleted = deleted;
                Payload = payload;
            }

            internal bool Deleted { get; }

            internal byte[] Payload { get; }
        }
    }
}
