using PitfallARCTool.PitfallTLE.FileFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PitfallARCTool
{
    class Program
    {
        struct Argument
        {
            public String cmd;
            public String[] args;
        }

        static void Usage()
        {
            Console.WriteLine("Pitfall ARC Tool");
            Console.WriteLine("-----------------------");
            Console.WriteLine();
            Console.WriteLine("Extract : PitfallARCTool -x <input path> -e <be|le> [-o <output path>]");
            Console.WriteLine("Create : PitfallARCTool -c <input path> -e <be|le> [-o <output path>]");
            Console.WriteLine("Update index.ind : PitfallARCTool -u <input path> -e <be|le>");
        }

        static bool ParseArguments(String[] args, out List<Argument> outArgs)
        {
            int i;
            outArgs = new List<Argument>();

            for (i = 0; i < args.Length; i++)
            {
                if (i < args.Length - 1)
                {
                    if (args[i] == "-c")
                    {
                        if (outArgs.Any(a => a.cmd == "-x"))
                        {
                            Console.WriteLine("Cannot compress when extracting a file already!");
                            return false;
                        }

                        if (outArgs.Any(a => a.cmd == "-u"))
                        {
                            Console.WriteLine("Cannot compress when updating index.ind!");
                            return false;
                        }

                        if (args[i + 1].StartsWith("-"))
                            return false;

                        outArgs.Add(new Argument()
                        {
                            cmd = args[i],
                            args = new String[] { args[i + 1] }
                        });
                        i++;
                        continue;
                    }

                    if (args[i] == "-x")
                    {
                        if (outArgs.Any(a => a.cmd == "-c"))
                        {
                            Console.WriteLine("Cannot extract when compressing a file already!");
                            return false;
                        }

                        if (outArgs.Any(a => a.cmd == "-u"))
                        {
                            Console.WriteLine("Cannot extract when updating index.ind!");
                            return false;
                        }

                        if (args[i + 1].StartsWith("-"))
                            return false;

                        outArgs.Add(new Argument()
                        {
                            cmd = args[i],
                            args = new String[] { args[i + 1] }
                        });
                        i++;
                        continue;
                    }

                    if (args[i] == "-u")
                    {
                        if (outArgs.Any(a => a.cmd == "-c"))
                        {
                            Console.WriteLine("Cannot update index.ind when compressing a file already!");
                            return false;
                        }

                        if (outArgs.Any(a => a.cmd == "-x"))
                        {
                            Console.WriteLine("Cannot update index.ind when extracting a file already!");
                            return false;
                        }

                        if (args[i + 1].StartsWith("-"))
                            return false;

                        outArgs.Add(new Argument()
                        {
                            cmd = args[i],
                            args = new String[] { args[i + 1] }
                        });
                        i++;
                        continue;
                    }

                    if (args[i] == "-o")
                    {
                        if (!outArgs.Any(a => a.cmd == "-c") && !outArgs.Any(a => a.cmd == "-x"))
                        {
                            Console.WriteLine("Are you compressing or extracting?");
                            return false;
                        }

                        if (args[i + 1].StartsWith("-"))
                            return false;

                        outArgs.Add(new Argument()
                        {
                            cmd = args[i],
                            args = new String[] { args[i + 1] }
                        });
                        i++;
                        continue;
                    }

                    if (args[i] == "-e")
                    {
                        if (!outArgs.Any(a => a.cmd == "-c") && !outArgs.Any(a => a.cmd == "-x") && !outArgs.Any(a => a.cmd == "-u"))
                        {
                            Console.WriteLine("Are you compressing or extracting or updating index.ind?");
                            return false;
                        }

                        if (args[i + 1].StartsWith("-"))
                            return false;

                        if (args[i + 1].ToLower() != "be" && args[i + 1].ToLower() != "le")
                        {
                            Console.WriteLine($"Invalid endianness supplied! ({args[i + 1]} in argument {outArgs.Count})");
                            return false;
                        }

                        outArgs.Add(new Argument()
                        {
                            cmd = args[i],
                            args = new String[] { args[i + 1].ToUpper() }
                        });
                        i++;
                        continue;
                    }
                }
                else
                {
                    if (args[i] == "-c" ||
                        args[i] == "-x" ||
                        args[i] == "-u" ||
                        args[i] == "-o")
                    {
                        return false;
                    }
                }
            }

            if (!outArgs.Any(a => a.cmd == "-c") && !outArgs.Any(a => a.cmd == "-x") && !outArgs.Any(a => a.cmd == "-u"))
                return false;
            return true;
        }

        static void Main(string[] args)
        {
            if (!ParseArguments(args, out List<Argument> arguments))
            {
                Usage();
                return;
            }

            Utils.CRC32.Instance = new Utils.CRC32();
            int i;
            bool isCompressing = arguments.Any(a => a.cmd == "-c");
            bool isExtracting = arguments.Any(a => a.cmd == "-x");
            bool isUpdatingIndexFile = arguments.Any(a => a.cmd == "-u");
            bool specifyOutputFolder = arguments.Any(a => a.cmd == "-o");
            String inPath = String.Empty;
            String outPath = String.Empty;
            String fileName = String.Empty;

            try
            {
                if (isCompressing)
                    inPath = arguments.Find(a => a.cmd == "-c").args[0];
                else if (isExtracting)
                    inPath = arguments.Find(a => a.cmd == "-x").args[0];
                else if (isUpdatingIndexFile)
                    inPath = arguments.Find(a => a.cmd == "-u").args[0];

                if (specifyOutputFolder)
                    outPath = arguments.Find(a => a.cmd == "-o").args[0];
                else
                    outPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileNameWithoutExtension(inPath));

                String endianness = arguments.Find(a => a.cmd == "-e").args[0];

                ARC arc = default(ARC);
                IND ind = default(IND);

                if (isExtracting)
                {
                    arc = new ARC(endianness == "BE");

                    if (!File.Exists(inPath))
                        throw new FileNotFoundException($"Couldn't find the file {inPath}");

                    arc.import(File.OpenRead(inPath));

                    if (!Directory.Exists(outPath))
                        Directory.CreateDirectory(outPath);

                    for (i = 0; i < arc.FileInfos.Count; i++)
                    {
                        Console.WriteLine($"Extracting {arc.FileInfos[i].Name}...");
                        File.WriteAllBytes(
                            Path.Combine(outPath, arc.FileInfos[i].Name),
                            arc.Files[i].ToArray()
                        );
                    }

                    using (var list = new StreamWriter(Path.Combine(outPath, "files.list")))
                    {
                        foreach (var fileInfo in arc.FileInfos)
                        {
                            list.WriteLine(fileInfo.Name);
                        }
                    }
                }

                if (isCompressing)
                {
                    arc = new ARC(endianness == "BE");

                    if (!Directory.Exists(inPath))
                        throw new FileNotFoundException($"Couldn't find the folder {inPath}");

                    if (Path.GetExtension(outPath) == String.Empty)
                        outPath += ".arc";
                    else if (Path.GetExtension(outPath).ToLower() != ".arc")
                        outPath = Path.ChangeExtension(outPath, ".arc");

                    if (!File.Exists(Path.Combine(inPath, "files.list")))
                        throw new FileNotFoundException($"Couldn't find the file files.list");

                    using (var list = new StreamReader(Path.Combine(inPath, "files.list")))
                    {
                        while (!list.EndOfStream)
                        {
                            fileName = list.ReadLine().TrimEnd('\r', '\n');
                            Console.WriteLine($"Adding {fileName}...");
                            arc.AddFile(
                                fileName,
                                new MemoryStream(File.ReadAllBytes(
                                    Path.Combine(inPath, fileName)
                                ))
                            );
                        }
                    }

                    arc.export(File.Open(outPath, FileMode.Create, FileAccess.Write));
                }

                if (isUpdatingIndexFile)
                {
                    if (!Directory.Exists(inPath))
                        throw new FileNotFoundException($"Couldn't find the folder {inPath}");

                    ind = new IND(inPath, endianness == "BE");

                    String index_ind_path = Path.Combine(inPath, "index.ind");

                    Console.Write("Updating index.ind... ");

                    ind.import(File.OpenRead(index_ind_path));
                    ind.export(File.Open(index_ind_path, FileMode.Create, FileAccess.Write));

                    Console.WriteLine("Done!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured!");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine();
                Console.WriteLine();
            }
        }
    }
}
