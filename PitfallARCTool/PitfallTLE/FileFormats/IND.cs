using Misc.Structs;
using System;
using System.Collections.Generic;
using System.IO;

namespace PitfallARCTool.PitfallTLE.FileFormats
{
    public class IND : BinaryStruct
    {
        public class ArchiveInfo : BinaryStruct
        {
            bool IsBigEndian;
            int Offset;

            public String ShortName => Name.Substring(0, Math.Min(8, Name.Length)).ToLower();


            public String Name;
            public List<UInt32> FileCRCs = new List<UInt32>();
            public List<ArchiveFileInfo> FileInfos = new List<ArchiveFileInfo>();

            public ArchiveInfo(bool isBigEndian, int offset)
            {
                IsBigEndian = isBigEndian;
                Offset = offset;
            }

            public void SetOffset(int offset) => Offset = offset;

            public override int StructSize
            {
                get
                {
                    int filenameLen = Math.Max(8, Name.Length + 1);
                    int len = 0;
                    while ((Offset + filenameLen) % 4 != 0)
                        filenameLen++;
                    len += filenameLen + 4;
                    len += FileCRCs.Count * 4 + FileInfos.Count * 8;
                    return len;
                }
            }

            public override void import(Stream stream)
            {
                int i, filenameLen = 0, filesCount = 0;
                var reader = (BinaryReader)(IsBigEndian ? new BinaryReaderBE(stream) : new BinaryReaderLE(stream));
                Name = reader.ReadString();
                filenameLen = Math.Max(8, Name.Length + 1);
                while ((Offset + filenameLen) % 4 != 0)
                    filenameLen++;
                for (i = 0; i < filenameLen - Name.Length - 1; i++)
                    reader.ReadByte();
                filesCount = reader.ReadInt32();
                for (i = 0; i < filesCount; i++)
                    FileCRCs.Add(reader.ReadUInt32());
                for (i = 0; i < filesCount; i++)
                {
                    FileInfos.Add(new ArchiveFileInfo(IsBigEndian));
                    FileInfos[i].import(stream);
                }
            }

            public override void export(Stream stream)
            {
                int i, filenameLen;

                var writer = (BinaryWriter)(IsBigEndian ? new BinaryWriterBE(stream) : new BinaryWriterLE(stream));
                writer.Write(Name);
                filenameLen = Math.Max(8, Name.Length + 1);
                while ((Offset + filenameLen) % 4 != 0)
                    filenameLen++;
                for (i = 0; i < filenameLen - Name.Length; i++)
                    writer.Write((Byte)0);
                writer.Write(FileCRCs.Count);
                foreach (var crc in FileCRCs)
                    writer.Write(crc);
                foreach (var fileInfo in FileInfos)
                {
                    writer.Write(fileInfo.Offset);
                    writer.Write(fileInfo.Size);
                }
            }
        }

        public class ArchiveFileInfo : BinaryStruct
        {
            bool IsBigEndian;

            public Int32 Offset;
            public Int32 Size;

            public ArchiveFileInfo(bool isBigEndian) => IsBigEndian = isBigEndian;


            public override int StructSize => 8;

            public override void import(Stream stream)
            {
                var reader = (BinaryReader)(IsBigEndian ? new BinaryReaderBE(stream) : new BinaryReaderLE(stream));
                Offset = reader.ReadInt32();
                Size = reader.ReadInt32();
            }

            public override void export(Stream stream)
            {
                var writer = (BinaryWriter)(IsBigEndian ? new BinaryWriterBE(stream) : new BinaryWriterLE(stream));
                writer.Write(Offset);
                writer.Write(Size);
            }
        }

        bool IsBigEndian;
        String BasePath;
        List<ArchiveInfo> ArchiveInfos;

        public IND(String basePath, bool isBigEndian = false)
        {
            BasePath = basePath;
            IsBigEndian = isBigEndian;
            ArchiveInfos = new List<ArchiveInfo>();
        }

        public override int StructSize
        {
            get
            {
                int len = 4 + ArchiveInfos.Count * 8 + 4;
                foreach (var archive in ArchiveInfos)
                    len += archive.StructSize;
                return len;
            }
        }

        public override void import(Stream stream)
        {
            int i, archiveCount;
            int[] nameOffsets;

            var reader = (BinaryReader)(IsBigEndian ? new BinaryReaderBE(stream) : new BinaryReaderLE(stream));
            archiveCount = reader.ReadInt32() / 2;
            nameOffsets = new int[archiveCount];
            for (i=0; i < archiveCount;i++)
            {
                nameOffsets[i] = reader.ReadInt32();
                reader.ReadInt32(); // skip info offset
            }

            for (i = 0; i < archiveCount; i++)
            {
                stream.Position = nameOffsets[i];
                ArchiveInfos.Add(new ArchiveInfo(IsBigEndian, nameOffsets[i]));
                ArchiveInfos[i].import(stream);
            }
            reader.Close();
        }

        public override void export(Stream stream)
        {
            int filenameLen, nameOffset, infoOffset;
            List<Tuple<Int32, Int32>> offsets = new List<Tuple<Int32, Int32>>();
            var writer = (BinaryWriter)(IsBigEndian ? new BinaryWriterBE(stream) : new BinaryWriterLE(stream));
            writer.Write(ArchiveInfos.Count * 2);
            stream.Position += ArchiveInfos.Count * 8 + 4;
            foreach(var archiveInfo in ArchiveInfos)
            {
                filenameLen = Math.Max(8, archiveInfo.Name.Length + 1);
                nameOffset = (int)stream.Position;
                infoOffset = nameOffset + filenameLen;
                while (infoOffset % 4 != 0) infoOffset++;

                offsets.Add(new Tuple<Int32, Int32>(nameOffset, infoOffset));
                archiveInfo.SetOffset((int)stream.Position);
                archiveInfo.export(stream);
            }
            stream.Position = 4;

            foreach (var (nameOff, infoOff) in offsets)
            {
                writer.Write(nameOff);
                writer.Write(infoOff);
            }
            writer.Write(StructSize);
            writer.Close();
        }

        public void UpdateArchiveInfo()
        {
            ARC archive = default(ARC);
            int i;
            List<UInt32> updatedCRCs = default(List<UInt32>);
            List<ArchiveFileInfo> updatedInfos = default(List<ArchiveFileInfo>);
            foreach (var archiveInfo in ArchiveInfos)
            {
                archive = GetArchive(archiveInfo.Name);
                updatedCRCs = new List<UInt32>();
                updatedInfos = new List<ArchiveFileInfo>();
                for (i = 0; i < archive.FileInfos.Count; i++)
                {
                    updatedCRCs.Add(archive.FileInfos[i].CRC);
                    updatedInfos.Add(new ArchiveFileInfo(IsBigEndian)
                    {
                        Offset = archive.FileInfos[i].Offset,
                        Size = archive.FileInfos[i].Size
                    });
                }
                archiveInfo.FileCRCs.Clear();
                archiveInfo.FileCRCs.AddRange(updatedCRCs);
                archiveInfo.FileInfos.Clear();
                archiveInfo.FileInfos.AddRange(updatedInfos);
                GC.Collect();
            }
        }

        public bool HasArchive(String fn)
        {
            foreach (var archiveInfo in ArchiveInfos)
                if (archiveInfo.Name.ToLower() == fn.ToLower())
                    return true;
            return false;
        }

        public int GetArchiveIndex(String fn)
        {
            int i = 0;
            for (i = 0; i < ArchiveInfos.Count; i++)
                if (ArchiveInfos[i].Name.ToLower() == fn.ToLower())
                    return i;
            return -1;
        }

        public ARC GetArchive(String fn)
        {
            ARC archive = default(ARC);
            int i = GetArchiveIndex(fn);
            if (i == -1)
                throw new FileNotFoundException($"Archive {fn} doesn't exist!");
            archive = new ARC();
            archive.import(File.OpenRead(Path.Combine(BasePath, $"{ArchiveInfos[i].ShortName}.arc")));
            return archive;
        }

        public void SetArchive(String fn, ARC archive)
        {
            int i = GetArchiveIndex(fn);
            if (i == -1)
                throw new FileNotFoundException($"Archive {fn} doesn't exist!");
            archive.export(File.Open(Path.Combine(BasePath, $"{ArchiveInfos[i].ShortName}.arc"), FileMode.Create, FileAccess.Write));
        }
    }
}
