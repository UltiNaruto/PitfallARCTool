using Misc.Structs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PitfallARCTool.PitfallTLE.FileFormats
{
    public class ARC : BinaryStruct
    {
        public class ARCFileInfo : BinaryStruct
        {
            bool IsBigEndian = false;

            public UInt32 CRC;
            public Int32 Offset;
            public Int32 Size;
            public String Name;
            public Int64 Timestamp;

            public ARCFileInfo(bool isBigEndian) => IsBigEndian = isBigEndian;

            public override int StructSize => 12 + (Name.Length + 1) + 8;

            public override void import(Stream stream)
            {
                var reader = (BinaryReader)(IsBigEndian ? new BinaryReaderBE(stream) : new BinaryReaderLE(stream));
                CRC = reader.ReadUInt32();
                Offset = reader.ReadInt32();
                Size = reader.ReadInt32();
                Name = reader.ReadString();
                Timestamp = reader.ReadInt64();
            }

            public override void export(Stream stream)
            {
                var writer = (BinaryWriter)(IsBigEndian ? new BinaryWriterBE(stream) : new BinaryWriterLE(stream));
                writer.Write(CRC);
                writer.Write(Offset);
                writer.Write(Size);
                writer.Write(Name);
                writer.Write(Timestamp);
            }
        }

        bool IsBigEndian;

        public List<ARCFileInfo> FileInfos;
        public List<MemoryStream> Files;

        public ARC(bool isBigEndian=false)
        {
            IsBigEndian = isBigEndian;
            FileInfos = new List<ARCFileInfo>();
            Files = new List<MemoryStream>();
        }

        public override int StructSize
        {
            get
            {
                int len = 4;
                foreach (var fileInfo in FileInfos)
                    len += fileInfo.Size;
                foreach (var fileInfo in FileInfos)
                    len += 12 + (fileInfo.Name.Length + 1) + 8;
                return len;
            }
        }

        public override void import(Stream stream)
        {
            int i, filesCount;
            UInt32 fsOffset = BitConverter.ToUInt32(new byte[]
            {
                (Byte)stream.ReadByte(), (Byte)stream.ReadByte(), (Byte)stream.ReadByte(), (Byte)stream.ReadByte()
            }, 0);

            var reader = (BinaryReader)(IsBigEndian ? new BinaryReaderBE(stream) : new BinaryReaderLE(stream));
            stream.Position = fsOffset;
            filesCount = reader.ReadInt32();
            for (i = 0; i < filesCount; i++)
            {
                FileInfos.Add(new ARCFileInfo(IsBigEndian));
                FileInfos[i].import(stream);
            }

            for (i = 0; i < filesCount; i++)
            {
                stream.Position = FileInfos[i].Offset;
                Files.Add(new MemoryStream(reader.ReadBytes(FileInfos[i].Size)));
            }
            reader.Close();
        }

        public override void export(Stream stream)
        {
            int fsOffset;

            var writer = (BinaryWriter)(IsBigEndian ? new BinaryWriterBE(stream) : new BinaryWriterLE(stream));
            writer.Write(0);

            foreach (var file in Files)
                writer.Write(file.ToArray());

            fsOffset = (int)stream.Position;

            writer.Write(Files.Count);

            foreach (var fileInfo in FileInfos)
                fileInfo.export(stream);

            stream.Position = 0;
            writer.Write(BitConverter.GetBytes(fsOffset));
            writer.Close();
        }

        public bool FileExists(String fn)
        {
            foreach (var fileInfo in FileInfos)
                if (fileInfo.Name.ToLower() == fn.ToLower())
                    return true;
            return false;
        }

        public int GetFileIndexByName(String fn)
        {
            int i;
            for (i=0;i<FileInfos.Count;i++)
                if (FileInfos[i].Name.ToLower() == fn.ToLower())
                    return i;
            return -1;
        }

        public int GetFileIndexByCRC(UInt32 crc)
        {
            int i;
            for (i = 0; i < FileInfos.Count; i++)
                if (FileInfos[i].CRC == crc)
                    return i;
            return -1;
        }

        public MemoryStream GetFile(String fn)
        {
            int i = GetFileIndexByName(fn);
            if (i == -1)
                throw new FileNotFoundException($"File {fn} has not been found!");
            return new MemoryStream(Files[i].ToArray());
        }

        Int32 GetFileOffset(Int32 index) => index == 0 ? FileInfos[0].Offset : FileInfos[index].Size + GetFileOffset(index - 1);

        public void SetFile(String fn, MemoryStream datas)
        {
            int i = GetFileIndexByName(fn);
            if (i == -1)
                throw new FileNotFoundException($"File {fn} has not been found!");
            FileInfos[i].Offset = GetFileOffset(i);
            FileInfos[i].Size = (int)datas.Length;
            FileInfos[i].Timestamp = Convert.ToInt64((DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds);

            Files[i].Close();
            Files[i] = new MemoryStream(datas.ToArray());

            GC.Collect();
        }

        public void InsertFile(String fn, int index, MemoryStream datas)
        {
            ARCFileInfo fileInfo = new ARCFileInfo(IsBigEndian)
            {
                CRC = Utils.CRC32.Instance.ComputeHash(Encoding.ASCII.GetBytes(fn), true, true),
                Name = fn,
                Size = (int)datas.Length,
                Timestamp = DateTime.Now.ToFileTime()
            };

            FileInfos.Insert(index, fileInfo);
            FileInfos[index].Offset = GetFileOffset(index);

            Files.Insert(index, new MemoryStream(datas.ToArray()));
        }

        public void AddFile(String fn, MemoryStream datas)
        {
            int index = FileInfos.Count;
            ARCFileInfo fileInfo = new ARCFileInfo(IsBigEndian)
            {
                CRC = Utils.CRC32.Instance.ComputeHash(Encoding.ASCII.GetBytes(fn), true, true),
                Name = fn,
                Size = (int)datas.Length,
                Timestamp = DateTime.Now.ToFileTime()
            };

            FileInfos.Add(fileInfo);
            FileInfos[index].Offset = GetFileOffset(index);

            Files.Add(new MemoryStream(datas.ToArray()));
        }

        public void RemoveFile(String fn)
        {
            int i = GetFileIndexByName(fn);
            if (i == -1)
                throw new FileNotFoundException($"File {fn} has not been found!");

            FileInfos.RemoveAt(i);

            Files[i].Close();
            Files.RemoveAt(i);

            GC.Collect();
        }
    }
}
